using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Статистический отчет таймингов и задержек конвейера кадра NVIDIA Reflex.
/// Содержит микросекундные временные метки всех этапов обработки от выборки ввода до рендеринга на GPU.
/// Соответствует структуре <c>sl::ReflexReport</c> (GUID: <c>{0D569B37-A1C8-4453-BE4D-40F4DE57952B}</c>, версия 1) из заголовочного файла <c>sl_reflex.h</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct ReflexReport
{
    private static readonly StructType ReflexReportTypeId = new(
        0x0d569b37, 0xa1c8, 0x4453,
        0xbe, 0x4d, 0x40, 0xf4, 0xde, 0x57, 0x95, 0x2b);

    /// <summary>
    /// Базовый заголовок структуры Streamline (версия 1).
    /// </summary>
    public BaseStructure Base;

    /// <summary>
    /// Порядковый номер кадра (Frame ID).
    /// </summary>
    public ulong FrameID;

    /// <summary>
    /// Временная метка считывания ввода пользователя (Input Sample Time) в микросекундах.
    /// </summary>
    public ulong InputSampleTime;

    /// <summary>
    /// Временная метка начала симуляции кадра игровым движком (Simulation Start Time).
    /// </summary>
    public ulong SimStartTime;

    /// <summary>
    /// Временная метка окончания симуляции кадра (Simulation End Time).
    /// </summary>
    public ulong SimEndTime;

    /// <summary>
    /// Временная метка начала формирования и отправки команд рендеринга на CPU (Render Submit Start).
    /// </summary>
    public ulong RenderSubmitStartTime;

    /// <summary>
    /// Временная метка завершения отправки команд рендеринга на CPU (Render Submit End).
    /// </summary>
    public ulong RenderSubmitEndTime;

    /// <summary>
    /// Временная метка начала вызова отображения кадра Present (Present Start Time).
    /// </summary>
    public ulong PresentStartTime;

    /// <summary>
    /// Временная метка завершения вызова Present (Present End Time).
    /// </summary>
    public ulong PresentEndTime;

    /// <summary>
    /// Временная метка начала обработки кадра графическим драйвером (Driver Start Time).
    /// </summary>
    public ulong DriverStartTime;

    /// <summary>
    /// Временная метка завершения обработки кадра графическим драйвером (Driver End Time).
    /// </summary>
    public ulong DriverEndTime;

    /// <summary>
    /// Временная метка постановки кадра в системную очередь рендеринга ОС (OS Render Queue Start).
    /// </summary>
    public ulong OsRenderQueueStartTime;

    /// <summary>
    /// Временная метка выхода кадра из системной очереди рендеринга ОС (OS Render Queue End).
    /// </summary>
    public ulong OsRenderQueueEndTime;

    /// <summary>
    /// Временная метка фактического начала исполнения рендеринга на GPU (GPU Render Start).
    /// </summary>
    public ulong GpuRenderStartTime;

    /// <summary>
    /// Временная метка завершения рендеринга кадра на GPU (GPU Render End).
    /// </summary>
    public ulong GpuRenderEndTime;

    /// <summary>
    /// Активное чистое время выполнения команд рендеринга на GPU в микросекундах.
    /// </summary>
    public uint GpuActiveRenderTimeUs;

    /// <summary>
    /// Полное время выполнения кадра на GPU в микросекундах (GPU Frame Time).
    /// </summary>
    public uint GpuFrameTimeUs;

    /// <summary>
    /// Создает инициализированную структуру <see cref="ReflexReport"/> версии 1.
    /// </summary>
    /// <returns>Новая структура <see cref="ReflexReport"/>.</returns>
    public static ReflexReport Create()
    {
        var r = new ReflexReport();
        r.Base = new BaseStructure(ReflexReportTypeId, 1);
        return r;
    }
}
