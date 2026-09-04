using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Текущее состояние подсистемы мониторинга задержек NVIDIA PC Latency (PCL).
/// Возвращается методом <see cref="PclAPI.GetState"/>.
/// Соответствует структуре <c>sl::PCLState</c> (GUID: <c>{CFA32F9B-023C-420E-9056-6832B74F89B5}</c>, версия 1) из заголовочного файла <c>sl_pcl.h</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct PCLState
{
    private static readonly StructType PCLStateTypeId = new(
        0xcfa32f9b, 0x023c, 0x420e,
        0x90, 0x56, 0x68, 0x32, 0xb7, 0x4f, 0x89, 0xb5);

    /// <summary>
    /// Базовый заголовок структуры Streamline (версия 1).
    /// </summary>
    public BaseStructure Base;

    /// <summary>
    /// Идентификатор зарегистрированного системного сообщения Windows для сбора статистики PCL (если <see cref="PCLOptions.VirtualKey"/> равно 0).
    /// </summary>
    public uint StatsWindowMessage;

    /// <summary>
    /// Байт выравнивания.
    /// </summary>
    private uint _pad0;

    /// <summary>
    /// Создает инициализированную структуру <see cref="PCLState"/> версии 1.
    /// </summary>
    /// <returns>Новая структура <see cref="PCLState"/>.</returns>
    public static PCLState Create()
    {
        var s = new PCLState();
        s.Base = new BaseStructure(PCLStateTypeId, 1);
        s.StatsWindowMessage = 0;
        s._pad0 = 0;
        return s;
    }
}
