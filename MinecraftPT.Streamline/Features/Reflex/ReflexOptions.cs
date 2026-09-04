using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Параметры конфигурации технологии минимизации задержек ввода NVIDIA Reflex.
/// Передаются в метод <see cref="ReflexAPI.SetOptions"/>.
/// Соответствует структуре <c>sl::ReflexOptions</c> (GUID: <c>{F03AF81A-6D0B-4902-A651-C4965E215434}</c>, версия 1) из заголовочного файла <c>sl_reflex.h</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct ReflexOptions
{
    private static readonly StructType ReflexOptionsTypeId = new(0xf03af81a, 0x6d0b, 0x4902, 0xa6, 0x51, 0xc4, 0x96, 0x5e, 0x21, 0x54, 0x34);

    /// <summary>
    /// Базовый заголовок структуры Streamline (версия 1).
    /// </summary>
    public BaseStructure Base;

    /// <summary>
    /// Режим работы Reflex (<see cref="ReflexMode"/>), например <see cref="ReflexMode.eLowLatencyWithBoost"/>.
    /// </summary>
    public uint Mode;

    /// <summary>
    /// Лимит времени кадра (FPS Cap) в микросекундах (0 — отключить ограничение, > 0 — целевое время кадра в мкс).
    /// Преимущество встроенного лимитера Reflex перед другими решениями заключается в том,
    /// что графический драйвер синхронизирует частоту кадров с максимальной плавностью без дополнительного лага очередей.
    /// </summary>
    public uint FrameLimitUs;

    /// <summary>
    /// Использовать ли маркёры для расширенной оптимизации планирования кадров (по умолчанию 0 / <see langword="false"/>).
    /// </summary>
    public byte UseMarkersToOptimize;

    /// <summary>
    /// Байт выравнивания для 2-байтовой границы.
    /// </summary>
    private byte pad0;

    /// <summary>
    /// Виртуальный код клавиши (горячая клавиша Reflex / PCL marker), например <see cref="PCLHotKey.eVK_F13"/>.
    /// </summary>
    public ushort VirtualKey;

    /// <summary>
    /// Идентификатор системного потока для передачи сообщений статистики Reflex.
    /// </summary>
    public uint IdThread;

    /// <summary>
    /// Создает инициализированную структуру <see cref="ReflexOptions"/> версии 1 со значениями по умолчанию.
    /// </summary>
    /// <returns>Новая структура <see cref="ReflexOptions"/>.</returns>
    public static ReflexOptions Create()
    {
        var opt = new ReflexOptions();
        opt.Base = new BaseStructure(ReflexOptionsTypeId, 1);
        opt.Mode = 0; // eOff
        opt.FrameLimitUs = 0;
        opt.UseMarkersToOptimize = 0;
        opt.VirtualKey = 0;
        opt.IdThread = 0;
        return opt;
    }
}
