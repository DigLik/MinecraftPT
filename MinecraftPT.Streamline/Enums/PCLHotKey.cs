namespace MinecraftPT.Streamline;

/// <summary>
/// Назначенная горячая клавиша (виртуальный код) для аппаратного триггера маркёра задержки Reflex / PC Latency (PCL).
/// Соответствует перечислению <c>sl::PCLHotKey</c> из заголовочного файла <c>sl_pcl.h</c> NVIDIA Streamline SDK.
/// </summary>
public enum PCLHotKey : short
{
    /// <summary>
    /// Использовать оконное системное сообщение пинга вместо эмуляции нажатия виртуальной клавиши.
    /// </summary>
    eUsePingMessage = 0,

    /// <summary>
    /// Виртуальная клавиша Windows VK_F13 (0x7C).
    /// </summary>
    eVK_F13 = 0x7C,

    /// <summary>
    /// Виртуальная клавиша Windows VK_F14 (0x7D).
    /// </summary>
    eVK_F14 = 0x7D,

    /// <summary>
    /// Виртуальная клавиша Windows VK_F15 (0x7E).
    /// </summary>
    eVK_F15 = 0x7E,
}
