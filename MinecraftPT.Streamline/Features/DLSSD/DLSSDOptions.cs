using System.Numerics;
using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Параметры конфигурации технологии нейросетевой реконструкции лучей NVIDIA DLSS Ray Reconstruction (DLSS-D / RR).
/// Передаются в метод <see cref="DlssdAPI.SetOptions"/> для применения настроек к указанному видовому экрану.
/// Соответствует структуре <c>sl::DLSSDOptions</c> (GUID: <c>{0AD87504-774E-4BF3-9633-A44D1F7F9CB8}</c>, версия 3) из заголовочного файла <c>sl_dlss_d.h</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct DLSSDOptions
{
    private static readonly StructType DLSSDOptionsTypeId = new(0x0ad87504, 0x774e, 0x4bf3, 0x96, 0x33, 0xa4, 0x4d, 0x1f, 0x7f, 0x9c, 0xb8);

    /// <summary>
    /// Базовый заголовок структуры Streamline (версия 3).
    /// </summary>
    public BaseStructure Base;

    /// <summary>
    /// Режим работы масштабирования/реконструкции лучей (<see cref="DLSSMode"/>), например <see cref="DLSSMode.eDLAA"/>, <see cref="DLSSMode.eMaxPerformance"/>, <see cref="DLSSMode.eBalanced"/> или <see cref="DLSSMode.eOff"/>.
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
    /// Уровень резкости в диапазоне [0.0, 1.0].
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
    /// Режим предоставления нормалей и шероховатости (<see cref="DLSSDNormalRoughnessMode"/>):
    /// <see cref="DLSSDNormalRoughnessMode.ePacked"/> (упакованный RGB+A) или <see cref="DLSSDNormalRoughnessMode.eUnpacked"/> (раздельные буферы).
    /// </summary>
    public DLSSDNormalRoughnessMode NormalRoughnessMode;

    /// <summary>
    /// Матрица преобразования из мирового пространства в пространство вида камеры (World to View). Row-major.
    /// </summary>
    public Matrix4x4 WorldToCameraView;

    /// <summary>
    /// Обратная матрица преобразования из пространства вида камеры в мировое пространство (View to World). Row-major.
    /// Вычисляется как обратная к <see cref="WorldToCameraView"/>.
    /// </summary>
    public Matrix4x4 CameraViewToWorld;

    /// <summary>
    /// Включено ли масштабирование альфа-канала вместе с цветом RGB.
    /// </summary>
    public Boolean AlphaUpscalingEnabled;

    /// <summary>
    /// Байты выравнивания для 4-байтовой границы полей пресетов.
    /// </summary>
    private byte pad1, pad2, pad3;

    /// <summary>
    /// Пресет модели нейросети Ray Reconstruction для режима DLAA (<see cref="DLSSDPreset"/>, например <see cref="DLSSDPreset.ePresetE"/>).
    /// </summary>
    public DLSSDPreset DlaaPreset;

    /// <summary>
    /// Пресет модели нейросети Ray Reconstruction для режима Quality (<see cref="DLSSDPreset"/>, например <see cref="DLSSDPreset.ePresetE"/>).
    /// </summary>
    public DLSSDPreset QualityPreset;

    /// <summary>
    /// Пресет модели нейросети Ray Reconstruction для режима Balanced (<see cref="DLSSDPreset"/>, например <see cref="DLSSDPreset.ePresetE"/>).
    /// </summary>
    public DLSSDPreset BalancedPreset;

    /// <summary>
    /// Пресет модели нейросети Ray Reconstruction для режима Performance (<see cref="DLSSDPreset"/>, например <see cref="DLSSDPreset.ePresetE"/>).
    /// </summary>
    public DLSSDPreset PerformancePreset;

    /// <summary>
    /// Пресет модели нейросети Ray Reconstruction для режима UltraPerformance (<see cref="DLSSDPreset"/>, например <see cref="DLSSDPreset.ePresetE"/>).
    /// </summary>
    public DLSSDPreset UltraPerformancePreset;

    /// <summary>
    /// Пресет модели нейросети Ray Reconstruction для режима UltraQuality (<see cref="DLSSDPreset"/>, например <see cref="DLSSDPreset.ePresetE"/>).
    /// </summary>
    public DLSSDPreset UltraQualityPreset;

    /// <summary>
    /// Создает инициализированную структуру <see cref="DLSSDOptions"/> версии 3 со значениями по умолчанию.
    /// </summary>
    /// <returns>Новая структура <see cref="DLSSDOptions"/>.</returns>
    public static DLSSDOptions Create()
    {
        var opt = new DLSSDOptions();
        opt.Base = new BaseStructure(DLSSDOptionsTypeId, 3);
        opt.Mode = DLSSMode.eOff;
        opt.OutputWidth = uint.MaxValue;
        opt.OutputHeight = uint.MaxValue;
        opt.Sharpness = 0.0f;
        opt.PreExposure = 1.0f;
        opt.ExposureScale = 1.0f;
        opt.ColorBuffersHDR = Boolean.eTrue;
        opt.IndicatorInvertAxisX = Boolean.eFalse;
        opt.IndicatorInvertAxisY = Boolean.eFalse;
        opt.NormalRoughnessMode = DLSSDNormalRoughnessMode.eUnpacked;
        opt.WorldToCameraView = Matrix4x4.Identity;
        opt.CameraViewToWorld = Matrix4x4.Identity;
        opt.AlphaUpscalingEnabled = Boolean.eFalse;
        opt.DlaaPreset = DLSSDPreset.eDefault;
        opt.QualityPreset = DLSSDPreset.eDefault;
        opt.BalancedPreset = DLSSDPreset.eDefault;
        opt.PerformancePreset = DLSSDPreset.eDefault;
        opt.UltraPerformancePreset = DLSSDPreset.eDefault;
        opt.UltraQualityPreset = DLSSDPreset.eDefault;
        return opt;
    }
}
