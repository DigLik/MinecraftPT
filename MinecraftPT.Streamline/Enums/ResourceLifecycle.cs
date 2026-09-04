namespace MinecraftPT.Streamline;

/// <summary>
/// Жизненный цикл тегированного ресурса в Streamline SDK.
/// Соответствует перечислению <c>sl::ResourceLifecycle</c> из заголовочного файла <c>sl_core_types.h</c>.
/// </summary>
/// <remarks>
/// <b>Важно:</b> Используйте <see cref="eOnlyValidNow"/> и <see cref="eValidUntilEvaluate"/> только при реальной необходимости,
/// так как это может привести к дополнительному расходу видеопамяти (VRAM) из-за создания внутренних копий Streamline.
/// Для большинства интеграций (включая DLSS Super Resolution, Ray Reconstruction и Frame Generation)
/// рекомендуется помечать все ресурсы как <see cref="eValidUntilPresent"/>.
/// </remarks>
public enum ResourceLifecycle : int
{
    /// <summary>
    /// Ресурс действителен только в момент вызова тегирования. Содержимое может быть изменено, освобождено или повторно использовано
    /// сразу после передачи в Streamline (требует явного командного буфера для создания внутренней копии).
    /// </summary>
    eOnlyValidNow = 0,

    /// <summary>
    /// Ресурс гарантированно неизменен, не уничтожается и не перезаписывается с момента предоставления до завершения вызова Present кадра на экране.
    /// Рекомендуемый режим для оптимальной производительности и минимизации копий в VRAM.
    /// </summary>
    eValidUntilPresent,

    /// <summary>
    /// Ресурс гарантированно неизменен до момента возврата управления из метода вычисления плагина (<c>slEvaluateFeature</c>).
    /// </summary>
    eValidUntilEvaluate
}
