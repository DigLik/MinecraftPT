namespace MinecraftPT.Streamline;

/// <summary>
/// Пресеты нейросетевых моделей для технологии NVIDIA DLSS-D (Ray Reconstruction / Реконструкция лучей).
/// Соответствует перечислению <c>sl::DLSSDPreset</c> из заголовочного файла <c>sl_dlss_d.h</c> NVIDIA Streamline SDK.
/// </summary>
/// <remarks>
/// <b>Важно:</b> Не путать с <see cref="DLSSPreset"/> для DLSS Super Resolution (SR).
/// В технологии Ray Reconstruction существуют только две фиксированные архитектуры:
/// <list type="bullet">
///   <item><description><see cref="ePresetD"/> — 1-е поколение модели Transformer (дебют в DLSS 3.5).</description></item>
///   <item><description><see cref="ePresetE"/> — 2-е (новейшее) поколение модели Transformer с улучшенной стабильностью тонких деталей и поддержкой направляющих прозрачности/DoF.</description></item>
/// </list>
/// Все остальные пресеты (<see cref="ePresetF"/>–<see cref="ePresetO"/>, включая <see cref="ePresetM"/>) в DLSS-D являются зарезервированными
/// слотами, которые откатываются к дефолтной модели (<c>Reverts to default. Not recommended to use</c>). Популярный в комьюнити
/// «Preset M» относится исключительно к апскейлеру DLSS SR (<see cref="DLSSPreset.ePresetM"/>) для режима Performance.
/// </remarks>
public enum DLSSDPreset : uint
{
    /// <summary>
    /// Дефолтное поведение модели (может обновляться через драйвер или профиль OTA).
    /// </summary>
    eDefault = 0,

    /// <summary>
    /// Первое поколение Ray Reconstruction (Gen 1). Базовая модель Transformer, дебютировавшая в DLSS 3.5.
    /// </summary>
    ePresetD = 4,

    /// <summary>
    /// Второе (новейшее) поколение Ray Reconstruction (Gen 2). Актуальная модель Transformer с повышенной
    /// временной стабильностью, сниженным гостингом на отражениях и поддержкой направляющих прозрачности и DoF.
    /// Рекомендуется для достижения наивысшего качества шумоподавления и реконструкции трассировки пути.
    /// </summary>
    ePresetE = 5,

    /// <summary>
    /// Зарезервировано. Откатывается к дефолтной модели (<see cref="eDefault"/>). Не рекомендуется к использованию.
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
    /// Зарезервировано. Откатывается к дефолтной модели (<see cref="eDefault"/>). Не рекомендуется к использованию.
    /// </summary>
    ePresetJ = 10,

    /// <summary>
    /// Зарезервировано. Откатывается к дефолтной модели (<see cref="eDefault"/>). Не рекомендуется к использованию.
    /// </summary>
    ePresetK = 11,

    /// <summary>
    /// Зарезервировано. Откатывается к дефолтной модели (<see cref="eDefault"/>). Не рекомендуется к использованию.
    /// </summary>
    ePresetL = 12,

    /// <summary>
    /// Зарезервировано. Откатывается к дефолтной модели (<see cref="eDefault"/>). Не рекомендуется к использованию.
    /// В отличие от DLSS SR, в Ray Reconstruction пресет M не является моделью второго поколения.
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
