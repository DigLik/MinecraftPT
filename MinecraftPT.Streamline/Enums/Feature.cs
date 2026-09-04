namespace MinecraftPT.Streamline;

/// <summary>
/// Уникальные идентификаторы технологий и плагинов, поддерживаемых фреймворком NVIDIA Streamline SDK.
/// Соответствует константам <c>sl::Feature</c> (<c>kFeature*</c>) из заголовочного файла <c>sl_core_types.h</c>.
/// </summary>
public enum Feature : uint
{
    /// <summary>
    /// Технология масштабирования изображения Deep Learning Super Sampling (DLSS Super Resolution).
    /// </summary>
    kFeatureDLSS = 0,

    /// <summary>
    /// Пространственное масштабирование и повышение резкости NVIDIA Image Scaling (NIS).
    /// </summary>
    kFeatureNIS = 2,

    /// <summary>
    /// Технология минимизации системных задержек ввода NVIDIA Reflex (Low Latency).
    /// </summary>
    kFeatureReflex = 3,

    /// <summary>
    /// Инструментарий мониторинга и профилирования задержек ПК (PC Latency / PCL).
    /// </summary>
    kFeaturePCL = 4,

    /// <summary>
    /// Нейросетевое улучшение динамической четкости и контраста Deep Dynamic Vibrance Control (DeepDVC).
    /// </summary>
    kFeatureDeepDVC = 5,

    /// <summary>
    /// Технология репроекции последних данных ввода перед выводом на экран (Latewarp).
    /// </summary>
    kFeatureLatewarp = 6,

    /// <summary>
    /// Генерация промежуточных кадров DLSS Frame Generation (DLSS-G).
    /// </summary>
    kFeatureDLSS_G = 1000,

    /// <summary>
    /// Нейросетевая реконструкция лучей для трассировки пути/лучей DLSS Ray Reconstruction (DLSS-RR / DLSS 3.5+).
    /// </summary>
    kFeatureDLSS_RR = 1001,

    /// <summary>
    /// Профилировщик производительности NVIDIA Performance Tools (NvPerf).
    /// </summary>
    kFeatureNvPerf = 1002,

    /// <summary>
    /// Интеграция Microsoft DirectSR через единый интерфейс Streamline.
    /// </summary>
    kFeatureDirectSR = 1003,

    /// <summary>
    /// Интеграция оверлея отладки ImGUI для инспекции состояния Streamline в реальном времени.
    /// </summary>
    kFeatureImGUI = 9999,

    /// <summary>
    /// Общая служебная функциональность Streamline (не предназначена для прямого использования приложениями).
    /// </summary>
    kFeatureCommon = uint.MaxValue
}
