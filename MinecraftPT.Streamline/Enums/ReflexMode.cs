namespace MinecraftPT.Streamline;

/// <summary>
/// Режимы работы технологии минимизации системной задержки NVIDIA Reflex.
/// Соответствует перечислению <c>sl::ReflexMode</c> из заголовочного файла <c>sl_reflex.h</c> NVIDIA Streamline SDK.
/// </summary>
public enum ReflexMode : uint
{
    /// <summary>
    /// NVIDIA Reflex отключен. Очередь команд GPU работает в обычном режиме драйвера.
    /// </summary>
    eOff = 0,

    /// <summary>
    /// Режим низкой задержки (Low Latency). Ограничивает очередь кадров GPU до 1 кадра и синхронизирует
    /// работу CPU и GPU, устраняя задержку очередей без снижения средней частоты кадров.
    /// </summary>
    eLowLatency,

    /// <summary>
    /// Режим низкой задержки с форсированием частот (Low Latency with Boost).
    /// В дополнение к алгоритмам низкой задержки удерживает тактовые частоты GPU на максимальном уровне
    /// даже в сценариях с упором в процессор (CPU-bound), устраняя задержки перехода состояний питания GPU.
    /// </summary>
    eLowLatencyWithBoost,

    /// <summary>
    /// Служебное значение количества элементов перечисления.
    /// </summary>
    eCount
}
