using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Параметры конфигурации плагина профилирования задержек NVIDIA PC Latency (PCL).
/// Передаются в метод <see cref="PclAPI.SetOptions"/>.
/// Соответствует структуре <c>sl::PCLOptions</c> (GUID: <c>{CFA32F9B-023C-420E-9056-6832B74F89B4}</c>, версия 1) из заголовочного файла <c>sl_pcl.h</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct PCLOptions
{
    private static readonly StructType PCLOptionsTypeId = new(
        0xcfa32f9b, 0x023c, 0x420e,
        0x90, 0x56, 0x68, 0x32, 0xb7, 0x4f, 0x89, 0xb4);

    /// <summary>
    /// Базовый заголовок структуры Streamline (версия 1).
    /// </summary>
    public BaseStructure Base;

    /// <summary>
    /// Виртуальная клавиша (горячая клавиша), используемая вместо пользовательского оконного сообщения для маркёра задержки (<see cref="PCLHotKey"/>).
    /// </summary>
    public PCLHotKey VirtualKey;

    /// <summary>
    /// Байт выравнивания для 4-байтовой границы.
    /// </summary>
    private short _pad0;

    /// <summary>
    /// Идентификатор системного потока (Thread ID) для обработки сообщений PCL.
    /// </summary>
    public uint IdThread;

    /// <summary>
    /// Создает инициализированную структуру <see cref="PCLOptions"/> версии 1.
    /// </summary>
    /// <returns>Новая структура <see cref="PCLOptions"/>.</returns>
    public static PCLOptions Create()
    {
        var opt = new PCLOptions();
        opt.Base = new BaseStructure(PCLOptionsTypeId, 1);
        opt.VirtualKey = PCLHotKey.eUsePingMessage;
        opt._pad0 = 0;
        opt.IdThread = 0;
        return opt;
    }
}
