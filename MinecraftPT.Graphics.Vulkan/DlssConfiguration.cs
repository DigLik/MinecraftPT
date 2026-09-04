using MinecraftPT.Streamline;

namespace MinecraftPT.Graphics.Vulkan;

/// <summary>
/// Единая точка конфигурации для технологий NVIDIA DLSS Super Resolution (SR) и DLSS Ray Reconstruction (RR).
/// Изменение параметров в этом классе централизованно управляет поведением DLSS на всех этапах графического пайплайна
/// (инициализация Streamline, пересоздание swapchain при изменении размера окна, покадровый рендеринг).
/// </summary>
public static class DlssConfiguration
{
    /// <summary>
    /// Режим работы масштабирования DLSS Super Resolution (SR).
    /// <para>
    /// По умолчанию: <see cref="DLSSMode.eDLAA"/> — нативное разрешение с антиалиасингом на базе глубокого обучения
    /// без снижения разрешения рендеринга (наивысшее качество картинки).
    /// </para>
    /// Для максимального FPS можно переключить на <see cref="DLSSMode.eMaxPerformance"/> или <see cref="DLSSMode.eBalanced"/>.
    /// </summary>
    public static DLSSMode SrMode { get; set; } = DLSSMode.eDLAA;

    /// <summary>
    /// Пресет нейросетевой модели для DLSS Super Resolution (SR).
    /// <para>
    /// По умолчанию: <see cref="DLSSPreset.ePresetK"/> — специализированная нейросеть на базе архитектуры Transformer,
    /// оптимизированная NVIDIA специально для режимов DLAA и Quality (обеспечивает максимальную стабильность тонких деталей).
    /// </para>
    /// При использовании режима <see cref="DLSSMode.eMaxPerformance"/> рекомендуется устанавливать <see cref="DLSSPreset.ePresetM"/>.
    /// </summary>
    public static DLSSPreset SrPreset { get; set; } = DLSSPreset.ePresetK;

    /// <summary>
    /// Режим работы реконструктора лучей DLSS Ray Reconstruction (RR).
    /// <para>
    /// По умолчанию: <see cref="DLSSMode.eDLAA"/> — реконструкция трассированных лучей и денойзинг в нативном разрешении
    /// для максимального качества глобального освещения и зеркальных отражений без апскейлинга.
    /// </para>
    /// </summary>
    public static DLSSMode RrMode { get; set; } = DLSSMode.eDLAA;

    /// <summary>
    /// Пресет нейросетевой модели для DLSS Ray Reconstruction (RR).
    /// <para>
    /// По умолчанию: <see cref="DLSSDPreset.ePresetE"/> — новейшая модель Transformer 2-го поколения для Ray Reconstruction,
    /// устраняющая размытие и артефакты нестабильности денойзера первого поколения (<see cref="DLSSDPreset.ePresetD"/>).
    /// </para>
    /// </summary>
    public static DLSSDPreset RrPreset { get; set; } = DLSSDPreset.ePresetE;

    /// <summary>
    /// Режим упаковки буферов нормалей и шероховатости для DLSS Ray Reconstruction.
    /// <para>
    /// По умолчанию: <see cref="DLSSDNormalRoughnessMode.ePacked"/> (упакованный формат normal.xyz + roughness в alpha).
    /// </para>
    /// </summary>
    public static DLSSDNormalRoughnessMode RrNormalRoughnessMode { get; set; } = DLSSDNormalRoughnessMode.ePacked;
}
