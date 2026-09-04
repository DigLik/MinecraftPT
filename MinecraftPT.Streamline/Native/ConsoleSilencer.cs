using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Утилита перенаправления и временного подавления вывода в системную консоль Windows и C Runtime (CRT).
/// Используется для предотвращения нежелательного спама нативных библиотек Streamline в релизных сборках.
/// </summary>
public static partial class ConsoleSilencer
{
    /// <summary>
    /// Устанавливает путь поиска динамических библиотек для текущего процесса.
    /// </summary>
    /// <param name="lpPathName">Путь к каталогу с библиотеками или <see langword="null"/> для сброса.</param>
    /// <returns><see langword="true"/> при успешной установке пути.</returns>
    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, EntryPoint = "SetDllDirectoryW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetDllDirectory(string? lpPathName);

    [LibraryImport("kernel32.dll")]
    private static partial IntPtr GetStdHandle(int nStdHandle);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetStdHandle(int nStdHandle, IntPtr hHandle);

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, EntryPoint = "CreateFileW")]
    private static partial IntPtr CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CloseHandle(IntPtr hObject);

    [LibraryImport("ucrtbase.dll", EntryPoint = "__acrt_iob_func")]
    private static partial IntPtr __acrt_iob_func(uint index);

    [LibraryImport("ucrtbase.dll", StringMarshalling = StringMarshalling.Utf8, EntryPoint = "freopen")]
    private static partial IntPtr freopen(string filename, string mode, IntPtr stream);

    private const int STD_OUTPUT_HANDLE = -11;
    private const int STD_ERROR_HANDLE = -12;
    private const uint GENERIC_WRITE = 0x40000000;
    private const uint FILE_SHARE_WRITE = 2;
    private const uint OPEN_EXISTING = 3;

    [System.Diagnostics.Conditional("DEBUG")]
    private static void CheckDebug(ref bool isDebug) => isDebug = true;

    /// <summary>
    /// Проверяет, скомпилирована ли текущая сборка в отладочной конфигурации (DEBUG).
    /// </summary>
    /// <returns><see langword="true"/>, если сборка собрана с флагом DEBUG.</returns>
    public static bool IsDebugBuild()
    {
        bool isDebug = false;
        CheckDebug(ref isDebug);
        return isDebug;
    }

    /// <summary>
    /// Контекст сохранения оригинальных файловых дескрипторов консоли для последующего восстановления.
    /// </summary>
    public struct SilenceContext
    {
        /// <summary>Исходный дескриптор stdout Windows.</summary>
        public IntPtr OriginalStdoutHandle;
        /// <summary>Исходный дескриптор stderr Windows.</summary>
        public IntPtr OriginalStderrHandle;
        /// <summary>Дескриптор устройства NUL.</summary>
        public IntPtr NulHandle;
        /// <summary>Было ли выполнено перенаправление на уровне Win32 API.</summary>
        public bool IsRedirected;
        /// <summary>Было ли выполнено перенаправление на уровне потоков CRT.</summary>
        public bool IsCrtRedirected;
    }

    /// <summary>
    /// Начинает подавление вывода в консоль, перенаправляя дескрипторы stdout и stderr в устройство NUL.
    /// В отладочных сборках подавление отключается для сохранения логов разработчика.
    /// </summary>
    /// <returns>Контекст для последующего восстановления через <see cref="EndSilence"/>.</returns>
    public static SilenceContext BeginSilence()
    {
        var ctx = new SilenceContext
        {
            OriginalStdoutHandle = IntPtr.Zero,
            OriginalStderrHandle = IntPtr.Zero,
            NulHandle = IntPtr.Zero,
            IsRedirected = false,
            IsCrtRedirected = false
        };

        if (IsDebugBuild())
        {
            return ctx;
        }

        try
        {
            // Win32 API level redirection
            ctx.OriginalStdoutHandle = GetStdHandle(STD_OUTPUT_HANDLE);
            ctx.OriginalStderrHandle = GetStdHandle(STD_ERROR_HANDLE);

            ctx.NulHandle = CreateFile(
                "NUL",
                GENERIC_WRITE,
                FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                0,
                IntPtr.Zero);

            if (ctx.NulHandle != (IntPtr)(-1))
            {
                SetStdHandle(STD_OUTPUT_HANDLE, ctx.NulHandle);
                SetStdHandle(STD_ERROR_HANDLE, ctx.NulHandle);
                ctx.IsRedirected = true;
            }

            // CRT level redirection
            IntPtr stdoutStream = __acrt_iob_func(1);
            IntPtr stderrStream = __acrt_iob_func(2);
            if (stdoutStream != IntPtr.Zero && stderrStream != IntPtr.Zero)
            {
                freopen("NUL", "w", stdoutStream);
                freopen("NUL", "w", stderrStream);
                ctx.IsCrtRedirected = true;
            }
        }
        catch
        {
            // Игнорируем ошибки перенаправления
        }

        return ctx;
    }

    /// <summary>
    /// Завершает подавление вывода в консоль и восстанавливает оригинальные дескрипторы вывода.
    /// </summary>
    /// <param name="ctx">Контекст, полученный из <see cref="BeginSilence"/>.</param>
    public static void EndSilence(in SilenceContext ctx)
    {
        if (IsDebugBuild())
        {
            return;
        }

        // Restore Win32 level redirection
        if (ctx.IsRedirected)
        {
            try
            {
                if (ctx.OriginalStdoutHandle != IntPtr.Zero)
                    SetStdHandle(STD_OUTPUT_HANDLE, ctx.OriginalStdoutHandle);
                if (ctx.OriginalStderrHandle != IntPtr.Zero)
                    SetStdHandle(STD_ERROR_HANDLE, ctx.OriginalStderrHandle);

                if (ctx.NulHandle != IntPtr.Zero && ctx.NulHandle != (IntPtr)(-1))
                    CloseHandle(ctx.NulHandle);
            }
            catch
            {
                // Игнорируем ошибки восстановления
            }
        }
    }

    /// <summary>
    /// Выполняет указанное действие с временным подавлением консольного вывода.
    /// </summary>
    /// <param name="action">Делегат действия.</param>
    public static void RunSilenced(Action action)
    {
        var ctx = BeginSilence();
        try
        {
            action();
        }
        finally
        {
            EndSilence(ctx);
        }
    }

    /// <summary>
    /// Выполняет указанную функцию с временным подавлением консольного вывода и возвращает результат.
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения.</typeparam>
    /// <param name="func">Функция для выполнения.</param>
    /// <returns>Результат выполнения функции.</returns>
    public static T RunSilenced<T>(Func<T> func)
    {
        var ctx = BeginSilence();
        try
        {
            return func();
        }
        finally
        {
            EndSilence(ctx);
        }
    }
}
