using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Текущее внутреннее состояние плагина NVIDIA DLSS Super Resolution для указанного видового экрана.
/// Возвращается методом <see cref="DlssAPI.GetState"/>.
/// Соответствует структуре <c>sl::DLSSState</c> (GUID: <c>{9366B056-8C01-463C-BB91-E68782636CE9}</c>, версия 1) из заголовочного файла <c>sl_dlss.h</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct DLSSState
{
    private static readonly StructType DLSSStateTypeId = new(0x9366b056, 0x8c01, 0x463c, 0xbb, 0x91, 0xe6, 0x87, 0x82, 0x63, 0x6c, 0xe9);

    /// <summary>
    /// Базовый заголовок структуры Streamline (версия 1).
    /// </summary>
    public BaseStructure Base;

    /// <summary>
    /// Оценочный объем видеопамяти (в байтах), выделенной и используемой нейросетевыми моделями DLSS.
    /// </summary>
    public ulong EstimatedVRAMUsageInBytes;

    /// <summary>
    /// Создает инициализированную структуру <see cref="DLSSState"/> версии 1.
    /// </summary>
    /// <returns>Экземпляр <see cref="DLSSState"/> с базовым заголовком.</returns>
    public static DLSSState Create()
    {
        var state = new DLSSState();
        state.Base = new BaseStructure(DLSSStateTypeId, 1);
        return state;
    }
}
