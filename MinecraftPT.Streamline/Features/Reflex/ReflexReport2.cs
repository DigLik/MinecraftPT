using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Дополнительный статистический отчет таймингов кадра NVIDIA Reflex (расширение отчета версии 2).
/// Содержит метрики времени финализации камеры и копирования между адаптерами.
/// Соответствует структуре <c>sl::ReflexReport2</c> (GUID: <c>{68BB0632-5E1C-402B-899D-B49F633C56C2}</c>, версия 1) из заголовочного файла <c>sl_reflex.h</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct ReflexReport2
{
    private static readonly StructType ReflexReport2TypeId = new(
        0x68bb0632, 0x5e1c, 0x402b,
        0x89, 0x9d, 0xb4, 0x9f, 0x63, 0x3c, 0x56, 0xc2);

    /// <summary>
    /// Базовый заголовок структуры Streamline (версия 1).
    /// </summary>
    public BaseStructure Base;

    /// <summary>
    /// Временная метка завершения построения матрицы камеры (Camera Constructed Time) в микросекундах.
    /// </summary>
    public ulong CameraConstructedTime;

    /// <summary>
    /// Время копирования кадра между графическими адаптерами (Cross-Adapter Copy Time) в микросекундах в многопроцессорных системах.
    /// </summary>
    public uint CrossAdapterCopyTimeUs;

    /// <summary>
    /// Байт выравнивания структуры.
    /// </summary>
    private uint _pad0;

    /// <summary>
    /// Создает инициализированную структуру <see cref="ReflexReport2"/> версии 1.
    /// </summary>
    /// <returns>Новая структура <see cref="ReflexReport2"/>.</returns>
    public static ReflexReport2 Create()
    {
        var r = new ReflexReport2();
        r.Base = new BaseStructure(ReflexReport2TypeId, 1);
        return r;
    }
}
