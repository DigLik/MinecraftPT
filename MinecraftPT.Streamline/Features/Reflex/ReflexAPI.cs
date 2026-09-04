using System.Runtime.CompilerServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Управляемый программный интерфейс (API) для работы с технологией снижения задержек NVIDIA Reflex.
/// Предоставляет методы задержки потока симуляции CPU (<c>slReflexSleep</c>), настройки режимов и получения телеметрии задержки.
/// Основан на функциях <c>slReflex*</c> из заголовочного файла <c>sl_reflex.h</c>.
/// </summary>
public static unsafe class ReflexAPI
{
    /// <summary>
    /// Нативный указатель на функцию задержки CPU <c>slReflexSleep</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<FrameToken*, int> SleepPtr { get; internal set; }

    /// <summary>
    /// Нативный указатель на функцию установки параметров <c>slReflexSetOptions</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<ReflexOptions*, int> SetOptionsPtr { get; internal set; }

    /// <summary>
    /// Нативный указатель на функцию получения текущего состояния <c>slReflexGetState</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<ReflexState*, int> GetStatePtr { get; internal set; }

    /// <summary>
    /// Нативный указатель на функцию передачи данных камеры <c>slReflexSetCameraData</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<ViewportHandle*, FrameToken*, ReflexCameraData*, int> SetCameraDataPtr { get; internal set; }

    /// <summary>
    /// Нативный указатель на функцию получения предсказанных данных камеры <c>slReflexGetPredictedCameraData</c>.
    /// </summary>
    public static delegate* unmanaged[Cdecl]<ViewportHandle*, FrameToken*, ReflexPredictedCameraData*, int> GetPredictedCameraDataPtr { get; internal set; }

    /// <summary>
    /// Показывает, загружены ли нативные функции модуля Reflex и готов ли API к использованию.
    /// </summary>
    public static bool IsLoaded => SetOptionsPtr != null;

    /// <summary>
    /// Вызывает оптимизированную функцию ожидания Reflex Sleep на основном потоке игры.
    /// Синхронизирует начало обработки следующего кадра на CPU точно ко времени, когда GPU готов принять команды,
    /// исключая нахождение кадра в промежуточной очереди и минимизируя задержку ввода (Input Lag).
    /// Потокобезопасный метод. Соответствует вызову <c>slReflexSleep</c> из <c>sl_reflex.h</c>.
    /// </summary>
    /// <param name="frame">Указатель на маркёр текущего кадра (<see cref="FrameToken"/>).</param>
    /// <returns>Результат выполнения операции (<see cref="Result.eOk"/> при успехе).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result Sleep(FrameToken* frame)
    {
        if (SleepPtr == null || frame == null)
            return Result.eErrorNotInitialized;

        return (Result)SleepPtr(frame);
    }

    /// <summary>
    /// Вызывает оптимизированную функцию ожидания Reflex Sleep (по ссылке на маркёр кадра).
    /// Потокобезопасный метод. Соответствует вызову <c>slReflexSleep</c> из <c>sl_reflex.h</c>.
    /// </summary>
    /// <param name="frame">Маркёр текущего кадра (<see cref="FrameToken"/>).</param>
    /// <returns>Результат выполнения операции (<see cref="Result.eOk"/> при успехе).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result Sleep(in FrameToken frame)
    {
        if (SleepPtr == null)
            return Result.eErrorNotInitialized;

        fixed (FrameToken* pFrame = &frame)
        {
            return (Result)SleepPtr(pFrame);
        }
    }

    /// <summary>
    /// Применяет параметры и режимы работы NVIDIA Reflex (включение Low Latency, Boost, ограничение частоты кадров).
    /// Соответствует вызову <c>slReflexSetOptions</c> из <c>sl_reflex.h</c>.
    /// </summary>
    /// <param name="options">Структура настроек Reflex (<see cref="ReflexOptions"/>).</param>
    /// <returns>Результат выполнения операции (<see cref="Result.eOk"/> при успехе).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result SetOptions(in ReflexOptions options)
    {
        if (SetOptionsPtr == null)
            return Result.eErrorNotInitialized;

        fixed (ReflexOptions* pOpt = &options)
        {
            return (Result)SetOptionsPtr(pOpt);
        }
    }

    /// <summary>
    /// Запрашивает текущее состояние Reflex, доступность технологии и отчеты о задержках последних кадров.
    /// Соответствует вызову <c>slReflexGetState</c> из <c>sl_reflex.h</c>.
    /// </summary>
    /// <param name="state">Возвращаемая структура состояния (<see cref="ReflexState"/>).</param>
    /// <returns>Результат выполнения операции (<see cref="Result.eOk"/> при успехе).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result GetState(out ReflexState state)
    {
        state = ReflexState.Create();
        if (GetStatePtr == null)
            return Result.eErrorNotInitialized;

        fixed (ReflexState* pState = &state)
        {
            return (Result)GetStatePtr(pState);
        }
    }

    /// <summary>
    /// Передает данные матриц камеры текущего и предыдущего кадров в алгоритм Reflex.
    /// Потокобезопасный метод. Соответствует вызову <c>slReflexSetCameraData</c> из <c>sl_reflex.h</c>.
    /// </summary>
    /// <param name="viewport">Дескриптор целевого видового экрана (<see cref="ViewportHandle"/>).</param>
    /// <param name="frame">Указатель на маркёр текущего кадра (<see cref="FrameToken"/>).</param>
    /// <param name="cameraData">Матричные параметры камеры (<see cref="ReflexCameraData"/>).</param>
    /// <returns>Результат выполнения операции (<see cref="Result.eOk"/> при успехе).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result SetCameraData(in ViewportHandle viewport, FrameToken* frame, in ReflexCameraData cameraData)
    {
        if (SetCameraDataPtr == null || frame == null)
            return Result.eErrorNotInitialized;

        fixed (ViewportHandle* pVp = &viewport)
        fixed (ReflexCameraData* pCam = &cameraData)
        {
            return (Result)SetCameraDataPtr(pVp, frame, pCam);
        }
    }

    /// <summary>
    /// Передает данные матриц камеры текущего и предыдущего кадров в алгоритм Reflex (по ссылке на токен кадра).
    /// Потокобезопасный метод.
    /// </summary>
    /// <param name="viewport">Дескриптор целевого видового экрана (<see cref="ViewportHandle"/>).</param>
    /// <param name="frame">Маркёр текущего кадра (<see cref="FrameToken"/>).</param>
    /// <param name="cameraData">Матричные параметры камеры (<see cref="ReflexCameraData"/>).</param>
    /// <returns>Результат выполнения операции (<see cref="Result.eOk"/> при успехе).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result SetCameraData(in ViewportHandle viewport, in FrameToken frame, in ReflexCameraData cameraData)
    {
        if (SetCameraDataPtr == null)
            return Result.eErrorNotInitialized;

        fixed (ViewportHandle* pVp = &viewport)
        fixed (FrameToken* pFrame = &frame)
        fixed (ReflexCameraData* pCam = &cameraData)
        {
            return (Result)SetCameraDataPtr(pVp, pFrame, pCam);
        }
    }

    /// <summary>
    /// Запрашивает у алгоритма Reflex предсказанное (экстраполированное) положение камеры на момент будущего вывода кадра.
    /// Потокобезопасный метод. Соответствует вызову <c>slReflexGetPredictedCameraData</c> из <c>sl_reflex.h</c>.
    /// </summary>
    /// <param name="viewport">Дескриптор целевого видового экрана (<see cref="ViewportHandle"/>).</param>
    /// <param name="frame">Указатель на маркёр текущего кадра (<see cref="FrameToken"/>).</param>
    /// <param name="cameraData">Возвращаемые предсказанные матрицы камеры (<see cref="ReflexPredictedCameraData"/>).</param>
    /// <returns>Результат выполнения операции (<see cref="Result.eOk"/> при успехе).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result GetPredictedCameraData(in ViewportHandle viewport, FrameToken* frame, out ReflexPredictedCameraData cameraData)
    {
        cameraData = ReflexPredictedCameraData.Create();
        if (GetPredictedCameraDataPtr == null || frame == null)
            return Result.eErrorNotInitialized;

        fixed (ViewportHandle* pVp = &viewport)
        fixed (ReflexPredictedCameraData* pCam = &cameraData)
        {
            return (Result)GetPredictedCameraDataPtr(pVp, frame, pCam);
        }
    }

    /// <summary>
    /// Запрашивает у алгоритма Reflex предсказанное положение камеры (по ссылке на токен кадра).
    /// Потокобезопасный метод.
    /// </summary>
    /// <param name="viewport">Дескриптор целевого видового экрана (<see cref="ViewportHandle"/>).</param>
    /// <param name="frame">Маркёр текущего кадра (<see cref="FrameToken"/>).</param>
    /// <param name="cameraData">Возвращаемые предсказанные матрицы камеры (<see cref="ReflexPredictedCameraData"/>).</param>
    /// <returns>Результат выполнения операции (<see cref="Result.eOk"/> при успехе).</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Result GetPredictedCameraData(in ViewportHandle viewport, in FrameToken frame, out ReflexPredictedCameraData cameraData)
    {
        cameraData = ReflexPredictedCameraData.Create();
        if (GetPredictedCameraDataPtr == null)
            return Result.eErrorNotInitialized;

        fixed (ViewportHandle* pVp = &viewport)
        fixed (FrameToken* pFrame = &frame)
        fixed (ReflexPredictedCameraData* pCam = &cameraData)
        {
            return (Result)GetPredictedCameraDataPtr(pVp, pFrame, pCam);
        }
    }

    /// <summary>
    /// Загружает функциональные указатели библиотеки Reflex через метод <see cref="StreamlineNative.slGetFeatureFunction"/>.
    /// Должен вызываться после инициализации графического устройства.
    /// </summary>
    public static void LoadFunctions()
    {
        void* func = null;
        fixed (byte* name = "slReflexSleep\0"u8)
            StreamlineNative.slGetFeatureFunction((uint)Feature.kFeatureReflex, name, &func);
        SleepPtr = (delegate* unmanaged[Cdecl]<FrameToken*, int>)func;

        func = null;
        fixed (byte* name = "slReflexSetOptions\0"u8)
            StreamlineNative.slGetFeatureFunction((uint)Feature.kFeatureReflex, name, &func);
        SetOptionsPtr = (delegate* unmanaged[Cdecl]<ReflexOptions*, int>)func;

        func = null;
        fixed (byte* name = "slReflexGetState\0"u8)
            StreamlineNative.slGetFeatureFunction((uint)Feature.kFeatureReflex, name, &func);
        GetStatePtr = (delegate* unmanaged[Cdecl]<ReflexState*, int>)func;

        func = null;
        fixed (byte* name = "slReflexSetCameraData\0"u8)
            StreamlineNative.slGetFeatureFunction((uint)Feature.kFeatureReflex, name, &func);
        SetCameraDataPtr = (delegate* unmanaged[Cdecl]<ViewportHandle*, FrameToken*, ReflexCameraData*, int>)func;

        func = null;
        fixed (byte* name = "slReflexGetPredictedCameraData\0"u8)
            StreamlineNative.slGetFeatureFunction((uint)Feature.kFeatureReflex, name, &func);
        GetPredictedCameraDataPtr = (delegate* unmanaged[Cdecl]<ViewportHandle*, FrameToken*, ReflexPredictedCameraData*, int>)func;
    }
}
