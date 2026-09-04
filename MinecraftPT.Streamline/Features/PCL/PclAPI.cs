using System.Runtime.CompilerServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Управляемый программный интерфейс (API) для работы с подсистемой мониторинга задержек NVIDIA PC Latency (PCL).
/// Позволяет расставлять временные маркёры кадров в конвейере для анализа задержек ввода-вывода Reflex Analyzer.
/// Основан на функциях <c>slPCL*</c> из заголовочного файла <c>sl_pcl.h</c>.
/// </summary>
public static unsafe class PclAPI
{
    /// <summary>
    /// Нативный указатель на функцию установки маркёра задержки <c>slPCLSetMarker</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<PCLMarker, FrameToken*, int> SetMarkerPtr { get; internal set; }

    /// <summary>
    /// Нативный указатель на функцию установки параметров <c>slPCLSetOptions</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<PCLOptions*, int> SetOptionsPtr { get; internal set; }

    /// <summary>
    /// Нативный указатель на функцию получения текущего состояния <c>slPCLGetState</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<PCLState*, int> GetStatePtr { get; internal set; }

    /// <summary>
    /// Показывает, загружены ли нативные функции модуля PCL и готов ли API к использованию.
    /// </summary>
    public static bool IsLoaded => SetMarkerPtr != null;

    /// <summary>
    /// Регистрирует маркёр фазы конвейера рендеринга для текущего кадра.
    /// Потокобезопасный метод. Соответствует <c>slPCLSetMarker</c> из <c>sl_pcl.h</c>.
    /// </summary>
    /// <param name="marker">Тип регистрируемого маркёра (<see cref="PCLMarker"/>).</param>
    /// <param name="token">Указатель на маркёр текущего кадра (<see cref="FrameToken"/>).</param>
    /// <returns>Результат выполнения операции (<see cref="Result.eOk"/> при успехе).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result SetMarker(PCLMarker marker, FrameToken* token)
    {
        if (SetMarkerPtr == null || token == null)
            return Result.eErrorNotInitialized;

        return (Result)SetMarkerPtr(marker, token);
    }

    /// <summary>
    /// Регистрирует маркёр фазы конвейера рендеринга для текущего кадра (по ссылке на маркёр).
    /// Потокобезопасный метод. Соответствует <c>slPCLSetMarker</c> из <c>sl_pcl.h</c>.
    /// </summary>
    /// <param name="marker">Тип регистрируемого маркёра (<see cref="PCLMarker"/>).</param>
    /// <param name="token">Маркёр текущего кадра (<see cref="FrameToken"/>).</param>
    /// <returns>Результат выполнения операции (<see cref="Result.eOk"/> при успехе).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result SetMarker(PCLMarker marker, in FrameToken token)
    {
        if (SetMarkerPtr == null)
            return Result.eErrorNotInitialized;

        fixed (FrameToken* pToken = &token)
        {
            return (Result)SetMarkerPtr(marker, pToken);
        }
    }

    /// <summary>
    /// Устанавливает параметры конфигурации модуля PCL.
    /// Соответствует вызову <c>slPCLSetOptions</c> из <c>sl_pcl.h</c>.
    /// </summary>
    /// <param name="options">Структура настроек PCL (<see cref="PCLOptions"/>).</param>
    /// <returns>Результат выполнения операции (<see cref="Result.eOk"/> при успехе).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result SetOptions(in PCLOptions options)
    {
        if (SetOptionsPtr == null)
            return Result.eErrorNotInitialized;

        fixed (PCLOptions* pOpt = &options)
        {
            return (Result)SetOptionsPtr(pOpt);
        }
    }

    /// <summary>
    /// Возвращает текущее состояние подсистемы мониторинга задержек PCL.
    /// Соответствует вызову <c>slPCLGetState</c> из <c>sl_pcl.h</c>.
    /// </summary>
    /// <param name="state">Возвращаемая структура состояния (<see cref="PCLState"/>).</param>
    /// <returns>Результат выполнения операции (<see cref="Result.eOk"/> при успехе).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result GetState(out PCLState state)
    {
        state = PCLState.Create();
        if (GetStatePtr == null)
            return Result.eErrorNotInitialized;

        fixed (PCLState* pState = &state)
        {
            return (Result)GetStatePtr(pState);
        }
    }

    /// <summary>
    /// Загружает функциональные указатели библиотеки PCL через метод <see cref="StreamlineNative.slGetFeatureFunction"/>.
    /// Должен вызываться после инициализации графического устройства.
    /// </summary>
    public static void LoadFunctions()
    {
        void* func = null;
        fixed (byte* name = "slPCLSetMarker\0"u8)
            StreamlineNative.slGetFeatureFunction((uint)Feature.kFeaturePCL, name, &func);
        SetMarkerPtr = (delegate* unmanaged[Cdecl]<PCLMarker, FrameToken*, int>)func;

        func = null;
        fixed (byte* name = "slPCLSetOptions\0"u8)
            StreamlineNative.slGetFeatureFunction((uint)Feature.kFeaturePCL, name, &func);
        SetOptionsPtr = (delegate* unmanaged[Cdecl]<PCLOptions*, int>)func;

        func = null;
        fixed (byte* name = "slPCLGetState\0"u8)
            StreamlineNative.slGetFeatureFunction((uint)Feature.kFeaturePCL, name, &func);
        GetStatePtr = (delegate* unmanaged[Cdecl]<PCLState*, int>)func;
    }
}
