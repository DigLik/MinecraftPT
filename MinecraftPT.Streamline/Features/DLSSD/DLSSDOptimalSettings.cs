using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Оптимальные параметры и размеры области рендеринга для технологии NVIDIA DLSS Ray Reconstruction (DLSS-D).
/// Возвращаются методом <see cref="DlssdAPI.GetOptimalSettings"/> на основе переданных опций <see cref="DLSSDOptions"/>.
/// Соответствует структуре <c>sl::DLSSDOptimalSettings</c> (GUID: <c>{FBD0C637-A28F-41F2-BC91-B421FAEE8E1E}</c>, версия 1) из заголовочного файла <c>sl_dlss_d.h</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct DLSSDOptimalSettings
{
    private static readonly StructType DLSSDOptimalSettingsTypeId = new(0xfbd0c637, 0xa28f, 0x41f2, 0xbc, 0x91, 0xb4, 0x21, 0xfa, 0xee, 0x8e, 0x1e);

    /// <summary>
    /// Базовый заголовок структуры Streamline (версия 1).
    /// </summary>
    public BaseStructure Base;

    /// <summary>
    /// Оптимальная рекомендуемая ширина внутреннего рендеринга в пикселях.
    /// </summary>
    public uint OptimalRenderWidth;

    /// <summary>
    /// Оптимальная рекомендуемая высота внутреннего рендеринга в пикселях.
    /// </summary>
    public uint OptimalRenderHeight;

    /// <summary>
    /// Оптимальное рекомендуемое значение резкости фильтра.
    /// </summary>
    public float OptimalSharpness;

    /// <summary>
    /// Минимально допустимая ширина внутреннего рендеринга при динамическом масштабировании (DRS).
    /// </summary>
    public uint RenderWidthMin;

    /// <summary>
    /// Минимально допустимая высота внутреннего рендеринга при динамическом масштабировании (DRS).
    /// </summary>
    public uint RenderHeightMin;

    /// <summary>
    /// Максимально допустимая ширина внутреннего рендеринга при DRS.
    /// </summary>
    public uint RenderWidthMax;

    /// <summary>
    /// Максимально допустимая высота внутреннего рендеринга при DRS.
    /// </summary>
    public uint RenderHeightMax;

    /// <summary>
    /// Создает инициализированную структуру <see cref="DLSSDOptimalSettings"/> версии 1.
    /// </summary>
    /// <returns>Экземпляр <see cref="DLSSDOptimalSettings"/> с базовым заголовком.</returns>
    public static DLSSDOptimalSettings Create()
    {
        var set = new DLSSDOptimalSettings();
        set.Base = new BaseStructure(DLSSDOptimalSettingsTypeId, 1);
        return set;
    }
}
