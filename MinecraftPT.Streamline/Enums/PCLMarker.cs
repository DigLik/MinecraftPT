namespace MinecraftPT.Streamline;

/// <summary>
/// Маркёры стадий конвейера рендеринга и игрового цикла для сбора метрик задержки NVIDIA Reflex и PC Latency (PCL).
/// Соответствует перечислению <c>sl::PCLMarker</c> из заголовочного файла <c>sl_pcl.h</c> NVIDIA Streamline SDK v2.12.0.
/// </summary>
public enum PCLMarker : uint
{
    /// <summary>
    /// Начало этапа симуляции кадра (обработка игровой логики, физики и анимаций).
    /// </summary>
    eSimulationStart = 0,

    /// <summary>
    /// Окончание этапа симуляции кадра.
    /// </summary>
    eSimulationEnd = 1,

    /// <summary>
    /// Начало записи и отправки команд рендеринга текущего кадра на исполнение в командную очередь GPU.
    /// </summary>
    eRenderSubmitStart = 2,

    /// <summary>
    /// Окончание записи и отправки команд рендеринга кадра (вызов очереди завершён).
    /// </summary>
    eRenderSubmitEnd = 3,

    /// <summary>
    /// Начало вызова вывода кадра на экран (Present).
    /// </summary>
    ePresentStart = 4,

    /// <summary>
    /// Окончание вызова вывода кадра на экран (Present завершён).
    /// </summary>
    ePresentEnd = 5,

    // eInputSample = 6 является устаревшим и удален в SDK v2.12.0

    /// <summary>
    /// Триггер аппаратного индикатора вспышки (Reflex Analyzer Flash Indicator) на экране для замера задержки датчиком мыши/монитора.
    /// </summary>
    eTriggerFlash = 7,

    /// <summary>
    /// Пинг задержки ПК (PCLatency Ping).
    /// </summary>
    ePCLatencyPing = 8,

    /// <summary>
    /// Начало асинхронной (out-of-band) отправки команд рендеринга вне основного конвейера кадра.
    /// </summary>
    eOutOfBandRenderSubmitStart = 9,

    /// <summary>
    /// Окончание асинхронной отправки команд рендеринга вне основного конвейера кадра.
    /// </summary>
    eOutOfBandRenderSubmitEnd = 10,

    /// <summary>
    /// Начало асинхронного вывода кадра (out-of-band present).
    /// </summary>
    eOutOfBandPresentStart = 11,

    /// <summary>
    /// Окончание асинхронного вывода кадра (out-of-band present).
    /// </summary>
    eOutOfBandPresentEnd = 12,

    /// <summary>
    /// Момент выборки состояния устройств ввода (мышь, клавиатура, контроллер) для текущего кадра.
    /// </summary>
    eControllerInputSample = 13,

    /// <summary>
    /// Момент вычисления дельты времени кадра (Delta Time Calculation).
    /// </summary>
    eDeltaTCalculation = 14,

    /// <summary>
    /// Начало презентации кадра в технологии Latewarp.
    /// </summary>
    eLateWarpPresentStart = 15,

    /// <summary>
    /// Окончание презентации кадра в технологии Latewarp.
    /// </summary>
    eLateWarpPresentEnd = 16,

    /// <summary>
    /// Момент завершения формирования матрицы камеры (View/Projection) для кадра.
    /// </summary>
    eCameraConstructed = 17,

    /// <summary>
    /// Начало отправки команд рендеринга Latewarp.
    /// </summary>
    eLateWarpRenderSubmitStart = 18,

    /// <summary>
    /// Окончание отправки команд рендеринга Latewarp.
    /// </summary>
    eLateWarpRenderSubmitEnd = 19,

    /// <summary>
    /// Начало внутренней асинхронной презентации драйвера поставщика GPU.
    /// </summary>
    eVendorInternalAsyncPresentStart = 20,

    /// <summary>
    /// Окончание внутренней асинхронной презентации драйвера поставщика GPU.
    /// </summary>
    eVendorInternalAsyncPresentEnd = 21,

    /// <summary>
    /// Количество кадровых показов в текущем пакете (Num Presents in Batch).
    /// </summary>
    eNumPresentsInBatch = 22,

    /// <summary>
    /// Верхняя граница значений маркёров (всего 23 элемента).
    /// </summary>
    eMaximum = 23
}
