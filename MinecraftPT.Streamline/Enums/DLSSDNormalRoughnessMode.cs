namespace MinecraftPT.Streamline;

/// <summary>
/// Режим передачи буферов нормалей и шероховатости для технологии NVIDIA DLSS Ray Reconstruction (DLSS-D).
/// Соответствует перечислению <c>sl::DLSSDNormalRoughnessMode</c> из заголовочного файла <c>sl_dlss_d.h</c> NVIDIA Streamline SDK.
/// </summary>
public enum DLSSDNormalRoughnessMode : uint
{
    /// <summary>
    /// Раздельный (распакованный) режим.
    /// Приложение предоставляет буфер нормалей (<see cref="BufferType.kBufferTypeNormals"/>)
    /// и буфер шероховатости (<see cref="BufferType.kBufferTypeRoughness"/>) в виде двух отдельных текстур.
    /// </summary>
    eUnpacked = 0,

    /// <summary>
    /// Упакованный режим (рекомендуемый для оптимизации пропускной способности памяти).
    /// Приложение записывает нормали в каналы RGB, а скаляр шероховатости — в канал A (W)
    /// единой текстуры (<see cref="BufferType.kBufferTypeNormalRoughness"/>).
    /// </summary>
    ePacked,

    /// <summary>
    /// Служебное значение количества элементов перечисления.
    /// </summary>
    eCount
}
