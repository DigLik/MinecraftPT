using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Параметры конфигурации плагина масштабирования NVIDIA DLSS Super Resolution (SR).
/// Передаются в метод <see cref="DlssAPI.SetOptions"/> для применения настроек к указанному видовому экрану.
/// Соответствует структуре <c>sl::DLSSOptions</c> (GUID: <c>{6AC826E4-4C61-4101-A92D-638D421057B8}</c>, версия 3) из заголовочного файла <c>sl_dlss.h</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct DLSSOptions
{
    private static readonly StructType DLSSOptionsTypeId = new(0x6ac826e4, 0x4c61, 0x4101, 0xa9, 0x2d, 0x63, 0x8d, 0x42, 0x10, 0x57, 0xb8);

    /// <summary>
    /// Базовый заголовок структуры Streamline (версия 3).
    /// </summary>
    public BaseStructure Base;

    /// <summary>
    /// Режим работы масштабирования DLSS (<see cref="DLSSMode"/>), например <see cref="DLSSMode.eMaxPerformance"/>, <see cref="DLSSMode.eBalanced"/>, <see cref="DLSSMode.eDLAA"/> или <see cref="DLSSMode.eOff"/>.
    /// </summary>
    public DLSSMode Mode;

    /// <summary>
    /// Целевая ширина итогового отображаемого изображения в пикселях (<c>OutputWidth</c>).
    /// </summary>
    public uint OutputWidth;

    /// <summary>
    /// Целевая высота итогового отображаемого изображения в пикселях (<c>OutputHeight</c>).
    /// </summary>
    public uint OutputHeight;

    /// <summary>
    /// Степень резкости в диапазоне [0.0, 1.0]. Устаревшее поле (резкость управляется отдельными алгоритмами).
    /// </summary>
    public float Sharpness;

    /// <summary>
    /// Значение предварительной экспозиции (Pre-Exposure), по умолчанию 1.0f.
    /// </summary>
    public float PreExposure;

    /// <summary>
    /// Масштабный коэффициент экспозиции (Exposure Scale), по умолчанию 1.0f.
    /// </summary>
    public float ExposureScale;

    /// <summary>
    /// Находятся ли тегированные цветные буферы в HDR диапазоне (<see cref="Boolean.eTrue"/>) или LDR/SDR (<see cref="Boolean.eFalse"/>).
    /// </summary>
    public Boolean ColorBuffersHDR;

    /// <summary>
    /// Инвертировать ли ось X экранного индикатора DLSS.
    /// </summary>
    public Boolean IndicatorInvertAxisX;

    /// <summary>
    /// Инвертировать ли ось Y экранного индикатора DLSS.
    /// </summary>
    public Boolean IndicatorInvertAxisY;

    /// <summary>
    /// Байт выравнивания для 4-байтовой границы.
    /// </summary>
    private byte pad0;

    /// <summary>
    /// Пресет нейросетевой модели для режима DLAA (<see cref="DLSSPreset"/>).
    /// </summary>
    public DLSSPreset DlaaPreset;

    /// <summary>
    /// Пресет нейросетевой модели для режима Quality (<see cref="DLSSPreset"/>).
    /// </summary>
    public DLSSPreset QualityPreset;

    /// <summary>
    /// Пресет нейросетевой модели для режима Balanced (<see cref="DLSSPreset"/>).
    /// </summary>
    public DLSSPreset BalancedPreset;

    /// <summary>
    /// Пресет нейросетевой модели для режима Performance (<see cref="DLSSPreset"/>, например <see cref="DLSSPreset.ePresetM"/>).
    /// </summary>
    public DLSSPreset PerformancePreset;

    /// <summary>
    /// Пресет нейросетевой модели для режима UltraPerformance (<see cref="DLSSPreset"/>).
    /// </summary>
    public DLSSPreset UltraPerformancePreset;

    /// <summary>
    /// Пресет нейросетевой модели для режима UltraQuality (<see cref="DLSSPreset"/>).
    /// </summary>
    public DLSSPreset UltraQualityPreset;

    /// <summary>
    /// Использовать ли внутренний автоматический расчет экспозиции (AutoExposure) DLSS.
    /// </summary>
    public Boolean UseAutoExposure;

    /// <summary>
    /// Включено ли масштабирование альфа-канала вместе с цветом RGB.
    /// Включение может оказывать незначительное влияние на производительность.
    /// </summary>
    public Boolean AlphaUpscalingEnabled;

    /// <summary>
    /// Байты выравнивания структуры.
    /// </summary>
    private byte pad1, pad2;

    /// <summary>
    /// Создает инициализированную структуру <see cref="DLSSOptions"/> версии 3 со значениями по умолчанию.
    /// </summary>
    /// <returns>Новая структура <see cref="DLSSOptions"/>.</returns>
    public static DLSSOptions Create()
    {
        var opt = new DLSSOptions();
        opt.Base = new BaseStructure(DLSSOptionsTypeId, 3);
        opt.Mode = DLSSMode.eOff;
        opt.OutputWidth = uint.MaxValue;
        opt.OutputHeight = uint.MaxValue;
        opt.Sharpness = 0.0f;
        opt.PreExposure = 1.0f;
        opt.ExposureScale = 1.0f;
        opt.ColorBuffersHDR = Boolean.eTrue;
        opt.IndicatorInvertAxisX = Boolean.eFalse;
        opt.IndicatorInvertAxisY = Boolean.eFalse;
        opt.DlaaPreset = DLSSPreset.eDefault;
        opt.QualityPreset = DLSSPreset.eDefault;
        opt.BalancedPreset = DLSSPreset.eDefault;
        opt.PerformancePreset = DLSSPreset.eDefault;
        opt.UltraPerformancePreset = DLSSPreset.eDefault;
        opt.UltraQualityPreset = DLSSPreset.eDefault;
        opt.UseAutoExposure = Boolean.eFalse;
        opt.AlphaUpscalingEnabled = Boolean.eFalse;
        return opt;
    }
}
