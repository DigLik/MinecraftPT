using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Текущее внутреннее состояние плагина NVIDIA DLSS Ray Reconstruction (DLSS-D) для указанного видового экрана.
/// Возвращается методом <see cref="DlssdAPI.GetState"/>.
/// Соответствует структуре <c>sl::DLSSDState</c> (GUID: <c>{71873C14-F8CA-4767-9EAF-3B4393EA98FA}</c>, версия 1) из заголовочного файла <c>sl_dlss_d.h</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct DLSSDState
{
    private static readonly StructType DLSSDStateTypeId = new(0x71873c14, 0xf8ca, 0x4767, 0x9e, 0xaf, 0x3b, 0x43, 0x93, 0xea, 0x98, 0xfa);

    /// <summary>
    /// Базовый заголовок структуры Streamline (версия 1).
    /// </summary>
    public BaseStructure Base;

    /// <summary>
    /// Оценочный объем видеопамяти (в байтах), выделенной и используемой нейросетевыми моделями Ray Reconstruction.
    /// </summary>
    public ulong EstimatedVRAMUsageInBytes;

    /// <summary>
    /// Создает инициализированную структуру <see cref="DLSSDState"/> версии 1.
    /// </summary>
    /// <returns>Экземпляр <see cref="DLSSDState"/> с базовым заголовком.</returns>
    public static DLSSDState Create()
    {
        var state = new DLSSDState();
        state.Base = new BaseStructure(DLSSDStateTypeId, 1);
        return state;
    }
}
