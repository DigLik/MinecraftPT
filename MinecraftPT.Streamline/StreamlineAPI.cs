using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Главный фасад прикладного интерфейса (API) NVIDIA Streamline SDK.
/// Предоставляет методы ранней инициализации фреймворка, регистрации контекста графического API (Vulkan),
/// покадрового тегирования ресурсов, внедрения вычислений и взаимодействия с плагинами DLSS SR, DLSS Ray Reconstruction, Reflex и PCL.
/// Соответствует интерфейсам ядра Streamline из заголовочных файлов <c>sl.h</c>, <c>sl_core_api.h</c> и документации SDK.
/// </summary>
public static unsafe class StreamlineAPI
{
    [System.Diagnostics.Conditional("DEBUG")]
    private static void LogInfo(string message) => Console.WriteLine(message);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void LogCallback(uint type, byte* msg)
    {
        try
        {
            uint minLogLevel = 1u; // Warning and above
            if (type < minLogLevel) return;

            string message = Marshal.PtrToStringAnsi((IntPtr)msg) ?? "";
            Console.WriteLine($"[Streamline Native Log] Type: {type}, Msg: {message}");
        }
        catch
        {
            // Игнорируем исключения внутри нативного колбэка
        }
    }

    /// <summary>
    /// Выполняет раннюю инициализацию библиотеки Streamline SDK до создания контекста графического API.
    /// Настраивает пути поиска нативных библиотек <c>binaries/x64/</c>, инициализирует параметры хоста (<see cref="Preferences"/>)
    /// и вызывает <c>slInit</c> с запросом базовых технологий (<see cref="Feature.kFeatureDLSS"/>, <see cref="Feature.kFeatureDLSS_RR"/>,
    /// <see cref="Feature.kFeatureReflex"/>, <see cref="Feature.kFeaturePCL"/>).
    /// </summary>
    /// <returns>Целочисленный код результата <see cref="Result"/> (0 = <see cref="Result.eOk"/> при успехе, отрицательное значение при ошибке загрузки библиотеки).</returns>
    public static int EarlyInitStreamline()
    {
        try
        {
            string binariesPath = Path.Combine(AppContext.BaseDirectory, "binaries", "x64");
            ConsoleSilencer.SetDllDirectory(binariesPath);
        }
        catch
        {
            // Игнорируем ошибки настройки пути
        }

        int initRes = -1;
        Exception? initException = null;

        var ctx = ConsoleSilencer.BeginSilence();
        try
        {
            var pref = Preferences.Create();
            pref.ShowConsole = 0;
            pref.LogLevel = ConsoleSilencer.IsDebugBuild() ? (uint)StreamlineLogLevel.eDefault : (uint)StreamlineLogLevel.eOff;
            pref.LogMessageCallback = (delegate* unmanaged[Cdecl]<uint, byte*, void>)&LogCallback;

            uint* features = stackalloc uint[4];
            features[0] = (uint)Feature.kFeatureDLSS;
            features[1] = (uint)Feature.kFeatureDLSS_RR;
            features[2] = (uint)Feature.kFeatureReflex;
            features[3] = (uint)Feature.kFeaturePCL;
            pref.FeaturesToLoad = features;
            pref.NumFeaturesToLoad = 4;
            pref.RenderAPI = 2; // eVulkan
            pref.ApplicationId = 0x10DE; // Идентификатор для подавления предупреждения продакшн-сборки

            // Streamline 2.12.0 (Major = 2, Minor = 12, Build = 0, Magic = 0xfedc)
            ulong sdkVersion = (2UL << 48) | (12UL << 32) | (0UL << 16) | 0xfedcUL;
            initRes = StreamlineNative.slInit(&pref, sdkVersion);
        }
        catch (Exception ex)
        {
            initException = ex;
        }
        finally
        {
            ConsoleSilencer.EndSilence(ctx);
        }

        if (initException != null)
        {
            Console.WriteLine($"[Streamline] Failed to load or initialize sl.interposer.dll during early init: {initException.Message}");
            return -1;
        }

        if (initRes != (int)Result.eOk)
        {
            Console.WriteLine($"[Streamline] slInit failed: {(Result)initRes}");
        }
        else
        {
            LogInfo("[Streamline] slInit early initialization succeeded.");
        }

        return initRes;
    }

    /// <summary>
    /// Завершает работу Streamline SDK и выгружает все загруженные модули.
    /// Метод <b>не является</b> потокобезопасным.
    /// Соответствует <c>slShutdown</c> из <c>sl_core_api.h</c>.
    /// </summary>
    /// <returns>Целочисленный код результата <see cref="Result"/>.</returns>
    public static int slShutdown() => StreamlineNative.slShutdown();

    /// <summary>
    /// Передает параметры созданного графического контекста Vulkan в Streamline SDK.
    /// Должен вызываться немедленно после создания <c>VkDevice</c>.
    /// Метод <b>не является</b> потокобезопасным.
    /// Соответствует <c>slSetVulkanInfo</c> из <c>sl_helpers_vk.h</c>.
    /// </summary>
    /// <param name="info">Указатель на заполненную структуру <see cref="VulkanInfo"/>.</param>
    /// <returns>Целочисленный код результата <see cref="Result"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int slSetVulkanInfo(VulkanInfo* info) => StreamlineNative.slSetVulkanInfo(info);

    /// <summary>
    /// Проверяет аппаратную и программную поддержку указанной технологии Streamline на графическом адаптере.
    /// Соответствует <c>slIsFeatureSupported</c> из <c>sl_core_api.h</c>.
    /// </summary>
    /// <param name="feature">Идентификатор технологии (<see cref="Feature"/>).</param>
    /// <param name="adapterInfo">Указатель на структуру сведений об адаптере (<see cref="AdapterInfo"/>).</param>
    /// <returns>Целочисленный код результата <see cref="Result"/> (<see cref="Result.eOk"/> если технология поддерживается).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int slIsFeatureSupported(uint feature, AdapterInfo* adapterInfo) =>
        StreamlineNative.slIsFeatureSupported(feature, adapterInfo);

    /// <summary>
    /// Выполняет покадровое связывание семантических тегов с физическими буферами GPU для указанного вьюпорта.
    /// Потокобезопасный метод. Соответствует <c>slSetTagForFrame</c> из <c>sl_core_api.h</c>.
    /// </summary>
    /// <param name="frame">Указатель на маркёр текущего кадра (<see cref="FrameToken"/>).</param>
    /// <param name="viewport">Указатель на дескриптор видового экрана (<see cref="ViewportHandle"/>).</param>
    /// <param name="tags">Указатель на массив тегов ресурсов (<see cref="ResourceTag"/>).</param>
    /// <param name="numTags">Количество тегов в массиве.</param>
    /// <param name="cmdBuffer">Нативный указатель на командный буфер Vulkan (<c>VkCommandBuffer</c>).</param>
    /// <returns>Целочисленный код результата <see cref="Result"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int slSetTagForFrame(FrameToken* frame, ViewportHandle* viewport, ResourceTag* tags, uint numTags, void* cmdBuffer) =>
        StreamlineNative.slSetTagForFrame(frame, viewport, tags, numTags, cmdBuffer);

    /// <summary>
    /// Устанавливает матричные параметры камеры и общие константы кадра для указанного вьюпорта.
    /// Потокобезопасный метод. Соответствует <c>slSetConstants</c> из <c>sl_core_api.h</c>.
    /// </summary>
    /// <param name="values">Указатель на структуру общих констант (<see cref="Constants"/>).</param>
    /// <param name="frame">Указатель на маркёр текущего кадра (<see cref="FrameToken"/>).</param>
    /// <param name="viewport">Указатель на дескриптор видового экрана (<see cref="ViewportHandle"/>).</param>
    /// <returns>Целочисленный код результата <see cref="Result"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int slSetConstants(Constants* values, FrameToken* frame, ViewportHandle* viewport) =>
        StreamlineNative.slSetConstants(values, frame, viewport);

    /// <summary>
    /// Записывает команды инференса и обработки заданной технологии в командный буфер GPU.
    /// Соответствует <c>slEvaluateFeature</c> из <c>sl_core_api.h</c>.
    /// </summary>
    /// <param name="feature">Идентификатор исполняемой технологии (<see cref="Feature"/>).</param>
    /// <param name="frame">Указатель на маркёр текущего кадра (<see cref="FrameToken"/>).</param>
    /// <param name="inputs">Массив указателей на входные структуры параметров.</param>
    /// <param name="numInputs">Количество входных структур.</param>
    /// <param name="cmdBuffer">Командный буфер GPU для записи команд рендеринга.</param>
    /// <returns>Целочисленный код результата <see cref="Result"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int slEvaluateFeature(uint feature, FrameToken* frame, void** inputs, uint numInputs, void* cmdBuffer) =>
        StreamlineNative.slEvaluateFeature(feature, frame, inputs, numInputs, cmdBuffer);

    /// <summary>
    /// Явно выделяет внутренние буферы и ресурсы GPU для технологии на указанном вьюпорте.
    /// Соответствует <c>slAllocateResources</c> из <c>sl_core_api.h</c>.
    /// </summary>
    /// <param name="cmdBuffer">Командный буфер GPU.</param>
    /// <param name="feature">Идентификатор технологии (<see cref="Feature"/>).</param>
    /// <param name="viewport">Указатель на дескриптор видового экрана (<see cref="ViewportHandle"/>).</param>
    /// <returns>Целочисленный код результата <see cref="Result"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int slAllocateResources(void* cmdBuffer, uint feature, ViewportHandle* viewport) =>
        StreamlineNative.slAllocateResources(cmdBuffer, feature, viewport);

    /// <summary>
    /// Явно освобождает ранее выделенные ресурсы GPU технологии для указанного вьюпорта.
    /// Соответствует <c>slFreeResources</c> из <c>sl_core_api.h</c>.
    /// </summary>
    /// <param name="feature">Идентификатор технологии (<see cref="Feature"/>).</param>
    /// <param name="viewport">Указатель на дескриптор видового экрана (<see cref="ViewportHandle"/>).</param>
    /// <returns>Целочисленный код результата <see cref="Result"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int slFreeResources(uint feature, ViewportHandle* viewport) =>
        StreamlineNative.slFreeResources(feature, viewport);

    /// <summary>
    /// Запрашивает новый уникальный токен кадра для идентификации ресурсов и вызовов текущей итерации рендеринга.
    /// Потокобезопасный метод. Соответствует <c>slGetNewFrameToken</c> из <c>sl_core_api.h</c>.
    /// </summary>
    /// <param name="token">Возвращаемый двойной указатель на токен кадра (<see cref="FrameToken"/>).</param>
    /// <param name="frameIndex">Опциональный указатель на номер кадра хоста.</param>
    /// <returns>Целочисленный код результата <see cref="Result"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int slGetNewFrameToken(FrameToken** token, uint* frameIndex) =>
        StreamlineNative.slGetNewFrameToken(token, frameIndex);

    /// <summary>
    /// Возвращает адрес нативной функции указанного плагина по её строковому имени.
    /// Соответствует <c>slGetFeatureFunction</c> из <c>sl_core_api.h</c>.
    /// </summary>
    /// <param name="feature">Идентификатор технологии (<see cref="Feature"/>).</param>
    /// <param name="functionName">Имя функции в формате байтовой строки UTF-8 с нулевым окончанием.</param>
    /// <param name="function">Возвращаемый указатель на функцию.</param>
    /// <returns>Целочисленный код результата <see cref="Result"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int slGetFeatureFunction(uint feature, byte* functionName, void** function) =>
        StreamlineNative.slGetFeatureFunction(feature, functionName, function);

    /// <summary>
    /// Перехватываемый вызов вывода кадра на экран очереди Vulkan <c>vkQueuePresentKHR</c>.
    /// </summary>
    /// <param name="queue">Очередь вывода кадра (<c>VkQueue</c>).</param>
    /// <param name="presentInfo">Указатель на структуру описания презентации <c>VkPresentInfoKHR</c>.</param>
    /// <returns>Код результата Vulkan (<c>VkResult</c>).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int vkQueuePresentKHR(void* queue, void* presentInfo) =>
        StreamlineNative.vkQueuePresentKHR(queue, presentInfo);

    /// <summary>
    /// Делегат функционального указателя нативного метода <c>slDLSSGetOptimalSettings</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<DLSSOptions*, DLSSOptimalSettings*, int> slDLSSGetOptimalSettings => DlssAPI.GetOptimalSettingsPtr;

    /// <summary>
    /// Делегат функционального указателя нативного метода <c>slDLSSGetState</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<ViewportHandle*, DLSSState*, int> slDLSSGetState => DlssAPI.GetStatePtr;

    /// <summary>
    /// Делегат функционального указателя нативного метода <c>slDLSSSetOptions</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<ViewportHandle*, DLSSOptions*, int> slDLSSSetOptions => DlssAPI.SetOptionsPtr;

    /// <summary>
    /// Делегат функционального указателя нативного метода <c>slDLSSDGetOptimalSettings</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<DLSSDOptions*, DLSSDOptimalSettings*, int> slDLSSDGetOptimalSettings => DlssdAPI.GetOptimalSettingsPtr;

    /// <summary>
    /// Делегат функционального указателя нативного метода <c>slDLSSDGetState</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<ViewportHandle*, DLSSDState*, int> slDLSSDGetState => DlssdAPI.GetStatePtr;

    /// <summary>
    /// Делегат функционального указателя нативного метода <c>slDLSSDSetOptions</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<ViewportHandle*, DLSSDOptions*, int> slDLSSDSetOptions => DlssdAPI.SetOptionsPtr;

    /// <summary>
    /// Делегат функционального указателя нативного метода <c>slReflexSleep</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<FrameToken*, int> slReflexSleep => ReflexAPI.SleepPtr;

    /// <summary>
    /// Делегат функционального указателя нативного метода <c>slReflexSetOptions</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<ReflexOptions*, int> slReflexSetOptions => ReflexAPI.SetOptionsPtr;

    /// <summary>
    /// Делегат функционального указателя нативного метода <c>slReflexGetState</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<ReflexState*, int> slReflexGetState => ReflexAPI.GetStatePtr;

    /// <summary>
    /// Делегат функционального указателя нативного метода <c>slReflexSetCameraData</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<ViewportHandle*, FrameToken*, ReflexCameraData*, int> slReflexSetCameraData => ReflexAPI.SetCameraDataPtr;

    /// <summary>
    /// Делегат функционального указателя нативного метода <c>slReflexGetPredictedCameraData</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<ViewportHandle*, FrameToken*, ReflexPredictedCameraData*, int> slReflexGetPredictedCameraData => ReflexAPI.GetPredictedCameraDataPtr;

    /// <summary>
    /// Делегат функционального указателя нативного метода <c>slPCLSetMarker</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<PCLMarker, FrameToken*, int> slPCLSetMarker => PclAPI.SetMarkerPtr;

    /// <summary>
    /// Делегат функционального указателя нативного метода <c>slPCLSetOptions</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<PCLOptions*, int> slPCLSetOptions => PclAPI.SetOptionsPtr;

    /// <summary>
    /// Делегат функционального указателя нативного метода <c>slPCLGetState</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<PCLState*, int> slPCLGetState => PclAPI.GetStatePtr;

    /// <summary>
    /// Загружает функциональные указатели для технологии DLSS Super Resolution.
    /// </summary>
    public static void LoadDLSSFunctions() => DlssAPI.LoadFunctions();

    /// <summary>
    /// Загружает функциональные указатели для технологии DLSS Ray Reconstruction.
    /// </summary>
    public static void LoadDLSSDFunctions() => DlssdAPI.LoadFunctions();

    /// <summary>
    /// Загружает функциональные указатели для технологии NVIDIA Reflex.
    /// </summary>
    public static void LoadReflexFunctions() => ReflexAPI.LoadFunctions();

    /// <summary>
    /// Загружает функциональные указатели для подсистемы профилирования задержек PCL.
    /// </summary>
    public static void LoadPCLFunctions() => PclAPI.LoadFunctions();

    /// <summary>
    /// Выполняет действие с временным перенаправлением системных потоков консоли.
    /// </summary>
    /// <param name="action">Делегат действия.</param>
    public static void RunSilenced(Action action) => ConsoleSilencer.RunSilenced(action);

    /// <summary>
    /// Выполняет функцию с временным перенаправлением системных потоков консоли и возвращает её результат.
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения.</typeparam>
    /// <param name="func">Функция для выполнения.</param>
    /// <returns>Результат выполнения функции.</returns>
    public static T RunSilenced<T>(Func<T> func) => ConsoleSilencer.RunSilenced(func);
}
