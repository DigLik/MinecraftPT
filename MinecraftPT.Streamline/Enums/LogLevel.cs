namespace MinecraftPT.Streamline;

/// <summary>
/// Уровни детализации системного логирования Streamline SDK.
/// Соответствует перечислению <c>sl::LogLevel</c> из заголовочного файла <c>sl_core_types.h</c> NVIDIA Streamline SDK.
/// </summary>
public enum StreamlineLogLevel : uint
{
    /// <summary>
    /// Логирование отключено полностью.
    /// </summary>
    eOff = 0,

    /// <summary>
    /// Стандартный уровень логирования (ошибки, критические предупреждения и ключевые события).
    /// </summary>
    eDefault,

    /// <summary>
    /// Подробный отладочный уровень логирования (трассировка каждого кадра, тегирования ресурсов и вызовов API).
    /// </summary>
    eVerbose,

    /// <summary>
    /// Служебное значение количества уровней логирования.
    /// </summary>
    eCount
}
