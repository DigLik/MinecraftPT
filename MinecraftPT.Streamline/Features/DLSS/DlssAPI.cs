using System.Runtime.CompilerServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Управляемый программный интерфейс (API) для работы с технологией NVIDIA DLSS Super Resolution (SR).
/// Предоставляет методы настройки качества, вычисления оптимального разрешения и запуска вычислений нейросети в командном буфере GPU.
/// Основан на функциях <c>slDLSS*</c> из заголовочного файла <c>sl_dlss.h</c>.
/// </summary>
public static unsafe class DlssAPI
{
    /// <summary>
    /// Нативный указатель на функцию получения оптимальных настроек <c>slDLSSGetOptimalSettings</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<DLSSOptions*, DLSSOptimalSettings*, int> GetOptimalSettingsPtr { get; internal set; }

    /// <summary>
    /// Нативный указатель на функцию получения текущего состояния <c>slDLSSGetState</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<ViewportHandle*, DLSSState*, int> GetStatePtr { get; internal set; }

    /// <summary>
    /// Нативный указатель на функцию установки параметров <c>slDLSSSetOptions</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<ViewportHandle*, DLSSOptions*, int> SetOptionsPtr { get; internal set; }

    /// <summary>
    /// Показывает, загружены ли нативные функции плагина DLSS и готов ли API к использованию.
    /// </summary>
    public static bool IsLoaded => SetOptionsPtr != null;

    /// <summary>
    /// Запрашивает оптимальные размеры области рендеринга и параметры для заданных настроек DLSS.
    /// Соответствует вызову <c>slDLSSGetOptimalSettings</c> из <c>sl_dlss.h</c>.
    /// </summary>
    /// <param name="options">Параметры масштабирования DLSS (<see cref="DLSSOptions"/>).</param>
    /// <param name="settings">Возвращаемая структура с оптимальными размерами рендеринга (<see cref="DLSSOptimalSettings"/>).</param>
    /// <returns>Результат выполнения операции (<see cref="Result.eOk"/> при успехе).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result GetOptimalSettings(in DLSSOptions options, out DLSSOptimalSettings settings)
    {
        settings = DLSSOptimalSettings.Create();
        if (GetOptimalSettingsPtr == null)
            return Result.eErrorNotInitialized;

        fixed (DLSSOptions* pOpt = &options)
        fixed (DLSSOptimalSettings* pSet = &settings)
        {
            return (Result)GetOptimalSettingsPtr(pOpt, pSet);
        }
    }

    /// <summary>
    /// Применяет параметры масштабирования DLSS Super Resolution к указанному видовому экрану.
    /// Соответствует вызову <c>slDLSSSetOptions</c> из <c>sl_dlss.h</c>.
    /// </summary>
    /// <param name="viewport">Дескриптор целевого видового экрана (<see cref="ViewportHandle"/>).</param>
    /// <param name="options">Структура настроек DLSS (<see cref="DLSSOptions"/>).</param>
    /// <returns>Результат выполнения операции (<see cref="Result.eOk"/> при успехе).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result SetOptions(in ViewportHandle viewport, in DLSSOptions options)
    {
        if (SetOptionsPtr == null)
            return Result.eErrorNotInitialized;

        fixed (ViewportHandle* pVp = &viewport)
        fixed (DLSSOptions* pOpt = &options)
        {
            return (Result)SetOptionsPtr(pVp, pOpt);
        }
    }

    /// <summary>
    /// Записывает команды инференса масштабирования нейросетью DLSS в указанный командный буфер GPU.
    /// Вызывает <c>slEvaluateFeature</c> для технологии <see cref="Feature.kFeatureDLSS"/>.
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
            return (Result)StreamlineNative.slEvaluateFeature((uint)Feature.kFeatureDLSS, frame, pInputs, 1, cmdBuffer);
        }
    }

    /// <summary>
    /// Записывает команды инференса масштабирования нейросетью DLSS в указанный командный буфер GPU (по ссылке на токен кадра).
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
            return (Result)StreamlineNative.slEvaluateFeature((uint)Feature.kFeatureDLSS, pFrame, pInputs, 1, cmdBuffer);
        }
    }

    /// <summary>
    /// Возвращает текущее состояние и объем занятой видеопамяти плагина DLSS для указанного видового экрана.
    /// Соответствует вызову <c>slDLSSGetState</c> из <c>sl_dlss.h</c>.
    /// </summary>
    /// <param name="viewport">Дескриптор видового экрана (<see cref="ViewportHandle"/>).</param>
    /// <param name="state">Возвращаемая структура состояния (<see cref="DLSSState"/>).</param>
    /// <returns>Результат выполнения операции (<see cref="Result.eOk"/> при успехе).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result GetState(in ViewportHandle viewport, out DLSSState state)
    {
        state = DLSSState.Create();
        if (GetStatePtr == null)
            return Result.eErrorNotInitialized;

        fixed (ViewportHandle* pVp = &viewport)
        fixed (DLSSState* pState = &state)
        {
            return (Result)GetStatePtr(pVp, pState);
        }
    }

    /// <summary>
    /// Загружает функциональные указатели библиотеки DLSS через метод <see cref="StreamlineNative.slGetFeatureFunction"/>.
    /// Должен вызываться после инициализации графического устройства (<c>slSetVulkanInfo</c> / <c>slSetD3DDevice</c>).
    /// </summary>
    public static void LoadFunctions()
    {
        void* func = null;
        fixed (byte* name = "slDLSSGetOptimalSettings\0"u8)
            StreamlineNative.slGetFeatureFunction((uint)Feature.kFeatureDLSS, name, &func);
        GetOptimalSettingsPtr = (delegate* unmanaged[Cdecl]<DLSSOptions*, DLSSOptimalSettings*, int>)func;

        func = null;
        fixed (byte* name = "slDLSSGetState\0"u8)
            StreamlineNative.slGetFeatureFunction((uint)Feature.kFeatureDLSS, name, &func);
        GetStatePtr = (delegate* unmanaged[Cdecl]<ViewportHandle*, DLSSState*, int>)func;

        func = null;
        fixed (byte* name = "slDLSSSetOptions\0"u8)
            StreamlineNative.slGetFeatureFunction((uint)Feature.kFeatureDLSS, name, &func);
        SetOptionsPtr = (delegate* unmanaged[Cdecl]<ViewportHandle*, DLSSOptions*, int>)func;
    }
}
