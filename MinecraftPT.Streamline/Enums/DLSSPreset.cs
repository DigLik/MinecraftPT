namespace MinecraftPT.Streamline;

/// <summary>
/// Пресеты нейросетевых моделей масштабирования для технологии NVIDIA DLSS Super Resolution (SR).
/// Соответствует перечислению <c>sl::DLSSPreset</c> из заголовочного файла <c>sl_dlss.h</c> NVIDIA Streamline SDK.
/// </summary>
/// <remarks>
/// В технологии DLSS Super Resolution пресеты определяют веса и архитектуру модели для конкретных режимов масштабирования:
/// <list type="bullet">
///   <item><description><see cref="ePresetK"/> — модель Transformer наивысшего качества для режимов DLAA, Quality и Balanced («Best image quality preset»).</description></item>
///   <item><description><see cref="ePresetL"/> — модель Transformer для режима UltraPerformance со сниженным гостингом.</description></item>
///   <item><description><see cref="ePresetM"/> — модель Transformer для режима Performance (сопоставимое с Preset L качество при повышенной скорости).</description></item>
/// </list>
/// </remarks>
public enum DLSSPreset : uint
{
    /// <summary>
    /// Дефолтное поведение модели (выбирается автоматически драйвером или профилем OTA).
    /// </summary>
    eDefault = 0,

    /// <summary>
    /// Устаревшая модель (Deprecated).
    /// </summary>
    ePresetE = 5,

    /// <summary>
    /// Устаревшая модель (Deprecated).
    /// </summary>
    ePresetF = 6,

    /// <summary>
    /// Зарезервировано. Откатывается к дефолтной модели (<see cref="eDefault"/>). Не рекомендуется к использованию.
    /// </summary>
    ePresetG = 7,

    /// <summary>
    /// Зарезервировано. Откатывается к дефолтной модели (<see cref="eDefault"/>). Не рекомендуется к использованию.
    /// </summary>
    ePresetH = 8,

    /// <summary>
    /// Зарезервировано. Откатывается к дефолтной модели (<see cref="eDefault"/>). Не рекомендуется к использованию.
    /// </summary>
    ePresetI = 9,

    /// <summary>
    /// Модель на базе Transformer/CNN со сниженным гостингом ценой возможного небольшого мерцания.
    /// Пресет <see cref="ePresetK"/> обычно предпочтительнее.
    /// </summary>
    ePresetJ = 10,

    /// <summary>
    /// Дефолтный пресет для режимов DLAA, Quality и Balanced на базе архитектуры Transformer.
    /// Обеспечивает наивысшее качество изображения и тонкую детализацию при повышенных требованиях к производительности GPU.
    /// Рекомендуется для режима DLAA и максимального визуального качества.
    /// </summary>
    ePresetK = 11,

    /// <summary>
    /// Дефолтный пресет для режима UltraPerformance на базе архитектуры Transformer.
    /// Формирует более резкое и стабильное изображение с меньшим гостингом, чем пресеты J и K.
    /// </summary>
    ePresetL = 12,

    /// <summary>
    /// Дефолтный пресет для режима Performance на базе архитектуры Transformer.
    /// Предоставляет качество, близкое к Preset L, но со скоростью работы, приближенной к пресетам J и K.
    /// </summary>
    ePresetM = 13,

    /// <summary>
    /// Зарезервировано. Откатывается к дефолтной модели (<see cref="eDefault"/>). Не рекомендуется к использованию.
    /// </summary>
    ePresetN = 14,

    /// <summary>
    /// Зарезервировано. Откатывается к дефолтной модели (<see cref="eDefault"/>). Не рекомендуется к использованию.
    /// </summary>
    ePresetO = 15,

    /// <summary>
    /// Служебное значение количества элементов перечисления.
    /// </summary>
    eCount
}
