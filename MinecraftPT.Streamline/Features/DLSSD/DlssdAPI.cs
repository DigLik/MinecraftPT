using System.Runtime.CompilerServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Управляемый программный интерфейс (API) для работы с технологией NVIDIA DLSS Ray Reconstruction (DLSS-D / RR).
/// Обеспечивает нейросетевое шумоподавление и реконструкцию отражений и диффузного освещения в реальном времени.
/// Основан на функциях <c>slDLSSD*</c> из заголовочного файла <c>sl_dlss_d.h</c>.
/// </summary>
public static unsafe class DlssdAPI
{
    /// <summary>
    /// Нативный указатель на функцию получения оптимальных настроек <c>slDLSSDGetOptimalSettings</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<DLSSDOptions*, DLSSDOptimalSettings*, int> GetOptimalSettingsPtr { get; internal set; }

    /// <summary>
    /// Нативный указатель на функцию получения текущего состояния <c>slDLSSDGetState</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<ViewportHandle*, DLSSDState*, int> GetStatePtr { get; internal set; }

    /// <summary>
    /// Нативный указатель на функцию установки параметров <c>slDLSSDSetOptions</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<ViewportHandle*, DLSSDOptions*, int> SetOptionsPtr { get; internal set; }

    /// <summary>
    /// Показывает, загружены ли нативные функции плагина DLSS-D и готов ли API к использованию.
    /// </summary>
    public static bool IsLoaded => SetOptionsPtr != null;

    /// <summary>
    /// Применяет параметры DLSS Ray Reconstruction к указанному видовому экрану.
    /// Соответствует вызову <c>slDLSSDSetOptions</c> из <c>sl_dlss_d.h</c>.
    /// </summary>
    /// <param name="viewport">Дескриптор целевого видового экрана (<see cref="ViewportHandle"/>).</param>
    /// <param name="options">Структура настроек DLSS-D (<see cref="DLSSDOptions"/>).</param>
    /// <returns>Результат выполнения операции (<see cref="Result.eOk"/> при успехе).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result SetOptions(in ViewportHandle viewport, in DLSSDOptions options)
    {
        if (SetOptionsPtr == null)
            return Result.eErrorNotInitialized;

        fixed (ViewportHandle* pVp = &viewport)
        fixed (DLSSDOptions* pOpt = &options)
        {
            return (Result)SetOptionsPtr(pVp, pOpt);
        }
    }

    /// <summary>
    /// Записывает команды инференса реконструкции лучей нейросетью DLSS-D в командный буфер GPU.
    /// Вызывает <c>slEvaluateFeature</c> для технологии <see cref="Feature.kFeatureDLSS_RR"/>.
    /// </summary>
    /// <param name="frame">Указатель на маркёр текущего кадра (<see cref="FrameToken"/>).</param>
    /// <param name="viewport">Дескриптор видового экрана (<see cref="ViewportHandle"/>).</param>
    /// <param name="cmdBuffer">Нативный командный буфер GPU (<c>VkCommandBuffer</c> или <c>ID3D12GraphicsCommandList*</c>).</param>
    /// <returns>Результат выполнения операции (<see cref="Result.eOk"/> при успехе).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result Evaluate(FrameToken* frame, in ViewportHandle viewport, void* cmdBuffer)
    {
        if (frame == null)
            return Result.eErrorInvalidParameter;

        fixed (ViewportHandle* pVp = &viewport)
        {
            void* inputViewport = pVp;
            void** pInputs = &inputViewport;
            return (Result)StreamlineNative.slEvaluateFeature((uint)Feature.kFeatureDLSS_RR, frame, pInputs, 1, cmdBuffer);
        }
    }

    /// <summary>
    /// Записывает команды инференса реконструкции лучей нейросетью DLSS-D в командный буфер GPU (по ссылке на токен кадра).
    /// </summary>
    /// <param name="frame">Маркёр текущего кадра (<see cref="FrameToken"/>).</param>
    /// <param name="viewport">Дескриптор видового экрана (<see cref="ViewportHandle"/>).</param>
    /// <param name="cmdBuffer">Нативный командный буфер GPU (<c>VkCommandBuffer</c> или <c>ID3D12GraphicsCommandList*</c>).</param>
    /// <returns>Результат выполнения операции (<see cref="Result.eOk"/> при успехе).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result Evaluate(in FrameToken frame, in ViewportHandle viewport, void* cmdBuffer)
    {
        fixed (FrameToken* pFrame = &frame)
        fixed (ViewportHandle* pVp = &viewport)
        {
            void* inputViewport = pVp;
            void** pInputs = &inputViewport;
            return (Result)StreamlineNative.slEvaluateFeature((uint)Feature.kFeatureDLSS_RR, pFrame, pInputs, 1, cmdBuffer);
        }
    }

    /// <summary>
    /// Запрашивает оптимальные размеры области рендеринга и параметры для заданных настроек DLSS-D.
    /// Соответствует вызову <c>slDLSSDGetOptimalSettings</c> из <c>sl_dlss_d.h</c>.
    /// </summary>
    /// <param name="options">Параметры DLSS-D (<see cref="DLSSDOptions"/>).</param>
    /// <param name="settings">Возвращаемая структура с оптимальными размерами рендеринга (<see cref="DLSSDOptimalSettings"/>).</param>
    /// <returns>Результат выполнения операции (<see cref="Result.eOk"/> при успехе).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result GetOptimalSettings(in DLSSDOptions options, out DLSSDOptimalSettings settings)
    {
        settings = DLSSDOptimalSettings.Create();
        if (GetOptimalSettingsPtr == null)
            return Result.eErrorNotInitialized;

        fixed (DLSSDOptions* pOpt = &options)
        fixed (DLSSDOptimalSettings* pSet = &settings)
        {
            return (Result)GetOptimalSettingsPtr(pOpt, pSet);
        }
    }

    /// <summary>
    /// Возвращает текущее состояние и объем занятой видеопамяти плагина DLSS-D для указанного видового экрана.
    /// Соответствует вызову <c>slDLSSDGetState</c> из <c>sl_dlss_d.h</c>.
    /// </summary>
    /// <param name="viewport">Дескриптор видового экрана (<see cref="ViewportHandle"/>).</param>
    /// <param name="state">Возвращаемая структура состояния (<see cref="DLSSDState"/>).</param>
    /// <returns>Результат выполнения операции (<see cref="Result.eOk"/> при успехе).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result GetState(in ViewportHandle viewport, out DLSSDState state)
    {
        state = DLSSDState.Create();
        if (GetStatePtr == null)
            return Result.eErrorNotInitialized;

        fixed (ViewportHandle* pVp = &viewport)
        fixed (DLSSDState* pState = &state)
        {
            return (Result)GetStatePtr(pVp, pState);
        }
    }

    /// <summary>
    /// Загружает функциональные указатели библиотеки DLSS-D через метод <see cref="StreamlineNative.slGetFeatureFunction"/>.
    /// Должен вызываться после инициализации графического устройства (<c>slSetVulkanInfo</c> / <c>slSetD3DDevice</c>).
    /// </summary>
    public static void LoadFunctions()
    {
        void* func = null;
        fixed (byte* name = "slDLSSDGetOptimalSettings\0"u8)
            StreamlineNative.slGetFeatureFunction((uint)Feature.kFeatureDLSS_RR, name, &func);
        GetOptimalSettingsPtr = (delegate* unmanaged[Cdecl]<DLSSDOptions*, DLSSDOptimalSettings*, int>)func;

        func = null;
        fixed (byte* name = "slDLSSDGetState\0"u8)
            StreamlineNative.slGetFeatureFunction((uint)Feature.kFeatureDLSS_RR, name, &func);
        GetStatePtr = (delegate* unmanaged[Cdecl]<ViewportHandle*, DLSSDState*, int>)func;

        func = null;
        fixed (byte* name = "slDLSSDSetOptions\0"u8)
            StreamlineNative.slGetFeatureFunction((uint)Feature.kFeatureDLSS_RR, name, &func);
        SetOptionsPtr = (delegate* unmanaged[Cdecl]<ViewportHandle*, DLSSDOptions*, int>)func;
    }
}
