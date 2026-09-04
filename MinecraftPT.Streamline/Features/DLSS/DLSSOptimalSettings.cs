using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Оптимальные параметры и размеры области рендеринга для технологии NVIDIA DLSS Super Resolution.
/// Возвращаются методом <see cref="DlssAPI.GetOptimalSettings"/> на основе переданных опций <see cref="DLSSOptions"/>.
/// Соответствует структуре <c>sl::DLSSOptimalSettings</c> (GUID: <c>{EF1D0957-FD58-4DF7-B504-8B69D8AA6B76}</c>, версия 1) из заголовочного файла <c>sl_dlss.h</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct DLSSOptimalSettings
{
    private static readonly StructType DLSSOptimalSettingsTypeId = new(0xef1d0957, 0xfd58, 0x4df7, 0xb5, 0x04, 0x8b, 0x69, 0xd8, 0xaa, 0x6b, 0x76);

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
    /// Минимально допустимая ширина внутреннего рендеринга при динамическом масштабировании (Dynamic Resolution Scaling / DRS).
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
    /// Создает инициализированную структуру <see cref="DLSSOptimalSettings"/> версии 1.
    /// </summary>
    /// <returns>Экземпляр <see cref="DLSSOptimalSettings"/> с базовым заголовком.</returns>
    public static DLSSOptimalSettings Create()
    {
        var set = new DLSSOptimalSettings();
        set.Base = new BaseStructure(DLSSOptimalSettingsTypeId, 1);
        return set;
    }
}
