using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Текущее системное состояние технологии NVIDIA Reflex, включая историю отчетов по задержкам последних 64 кадров.
/// Возвращается методом <see cref="ReflexAPI.GetState"/>.
/// Соответствует структуре <c>sl::ReflexState</c> (GUID: <c>{F0BB5985-DAF9-4728-B2FD-AE80A2BD7989}</c>, версия 2) из заголовочного файла <c>sl_reflex.h</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct ReflexState
{
    private static readonly StructType ReflexStateTypeId = new(
        0xf0bb5985, 0xdaf9, 0x4728,
        0xb2, 0xfd, 0xae, 0x80, 0xa2, 0xbd, 0x79, 0x89);

    /// <summary>
    /// Максимальное количество отчетов кадровой статистики, сохраняемых в циклическом буфере Reflex (64 кадра).
    /// </summary>
    public const int ReflexFrameReportCount = 64;

    /// <summary>
    /// Базовый заголовок структуры Streamline (версия 2).
    /// </summary>
    public BaseStructure Base;

    /// <summary>
    /// Доступен ли режим низкой задержки (Low Latency) на текущем GPU и драйвере (1 — да, 0 — нет).
    /// </summary>
    public byte LowLatencyAvailable;

    /// <summary>
    /// Доступны ли достоверные данные в отчетах по задержкам <see cref="FrameReports"/> (1 — да, 0 — нет).
    /// </summary>
    public byte LatencyReportAvailable;

    /// <summary>
    /// Байт выравнивания.
    /// </summary>
    private byte _pad0;

    /// <summary>
    /// Байт выравнивания.
    /// </summary>
    private byte _pad1;

    /// <summary>
    /// Идентификатор системного сообщения Windows для получения статистики задержки.
    /// </summary>
    public uint StatsWindowMessage;

    /// <summary>
    /// Необработанный буфер байтов для массива отчетов <see cref="ReflexReport"/> (64 отчета по 152 байта).
    /// </summary>
    public fixed byte FrameReportRaw[ReflexFrameReportCount * 152];

    /// <summary>
    /// Управляется ли индикатор вспышки драйвером (1 — драйвером, 0 — приложением).
    /// </summary>
    public byte FlashIndicatorDriverControlled;

    /// <summary>
    /// Байты выравнивания.
    /// </summary>
    private fixed byte _pad2[7];

    /// <summary>
    /// Необработанный буфер байтов для массива отчетов версии 2 <see cref="ReflexReport2"/> (64 отчета по 48 байт).
    /// </summary>
    public fixed byte FrameReport2Raw[ReflexFrameReportCount * 48];

    /// <summary>
    /// Представление среза (ReadOnlySpan) истории отчетов <see cref="ReflexReport"/> для 64 кадров.
    /// </summary>
    public ReadOnlySpan<ReflexReport> FrameReports
    {
        get
        {
            fixed (byte* p = FrameReportRaw)
            {
                return new ReadOnlySpan<ReflexReport>(p, ReflexFrameReportCount);
            }
        }
    }

    /// <summary>
    /// Представление среза (ReadOnlySpan) дополнительных отчетов <see cref="ReflexReport2"/> для 64 кадров.
    /// </summary>
    public ReadOnlySpan<ReflexReport2> FrameReports2
    {
        get
        {
            fixed (byte* p = FrameReport2Raw)
            {
                return new ReadOnlySpan<ReflexReport2>(p, ReflexFrameReportCount);
            }
        }
    }

    /// <summary>
    /// Создает инициализированную структуру <see cref="ReflexState"/> версии 2.
    /// </summary>
    /// <returns>Новая структура <see cref="ReflexState"/>.</returns>
    public static ReflexState Create()
    {
        var s = new ReflexState();
        s.Base = new BaseStructure(ReflexStateTypeId, 2);
        return s;
    }
}
