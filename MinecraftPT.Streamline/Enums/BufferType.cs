namespace MinecraftPT.Streamline;

/// <summary>
/// Идентификаторы типов буферов и ресурсов рендеринга для тегирования во фреймворке NVIDIA Streamline SDK.
/// Соответствует константам <c>sl::BufferType</c> (<c>kBufferType*</c>) из заголовочного файла <c>sl_core_types.h</c>.
/// </summary>
public enum BufferType : uint
{
    /// <summary>
    /// Буфер глубины сцены (Depth buffer).
    /// <para><b>Важно:</b> Должен быть согласован с матрицей преобразования <c>clipToPrevClip</c> (см. <see cref="Constants"/>).</para>
    /// </summary>
    kBufferTypeDepth = 0,

    /// <summary>
    /// Векторы движения объектов сцены и опционально камеры (Motion Vectors).
    /// </summary>
    kBufferTypeMotionVectors = 1,

    /// <summary>
    /// Буфер цвета со всеми примененными эффектами постобработки, но без элементов пользовательского интерфейса (HUD-less Color).
    /// </summary>
    kBufferTypeHUDLessColor = 2,

    /// <summary>
    /// Входной цветной буфер с примененным субпиксельным сдвигом (jitter) для прохода масштабирования (DLSS SR / DLSS-RR input).
    /// </summary>
    kBufferTypeScalingInputColor = 3,

    /// <summary>
    /// Выходной цветной буфер с результатом масштабирования/реконструкции (DLSS SR / DLSS-RR output).
    /// </summary>
    kBufferTypeScalingOutputColor = 4,

    /// <summary>
    /// Буфер нормалей геометрии в пространстве вида/мира (Normals).
    /// </summary>
    kBufferTypeNormals = 5,

    /// <summary>
    /// Буфер шероховатости поверхностей (Roughness).
    /// </summary>
    kBufferTypeRoughness = 6,

    /// <summary>
    /// Буфер базового диффузного цвета/альбедо поверхностей (Albedo).
    /// </summary>
    kBufferTypeAlbedo = 7,

    /// <summary>
    /// Буфер зеркального альбедо поверхностей (Specular Albedo / F0).
    /// </summary>
    kBufferTypeSpecularAlbedo = 8,

    /// <summary>
    /// Буфер непрямого альбедо (Indirect Albedo).
    /// </summary>
    kBufferTypeIndirectAlbedo = 9,

    /// <summary>
    /// Векторы движения зеркальных отражений (Specular Motion Vectors).
    /// </summary>
    kBufferTypeSpecularMotionVectors = 10,

    /// <summary>
    /// Маска перекрытия/раскрытия геометрии (Disocclusion Mask).
    /// </summary>
    kBufferTypeDisocclusionMask = 11,

    /// <summary>
    /// Буфер излучающих/светящихся поверхностей (Emissive).
    /// </summary>
    kBufferTypeEmissive = 12,

    /// <summary>
    /// Текстура 1x1 или значение экспозиции сцены (Exposure).
    /// </summary>
    kBufferTypeExposure = 13,

    /// <summary>
    /// Упакованный буфер с нормалями в каналах RGB и шероховатостью в альфа-канале A (Normal + Roughness).
    /// Используется для эффективного DLSS Ray Reconstruction при <see cref="DLSSDNormalRoughnessMode.ePacked"/>.
    /// </summary>
    kBufferTypeNormalRoughness = 14,

    /// <summary>
    /// Зашумленный сигнал диффузного освещения и длина луча первичного пересечения (Diffuse Hit Noisy).
    /// </summary>
    kBufferTypeDiffuseHitNoisy = 15,

    /// <summary>
    /// Очищенный от шума сигнал диффузного освещения (Diffuse Hit Denoised).
    /// </summary>
    kBufferTypeDiffuseHitDenoised = 16,

    /// <summary>
    /// Зашумленный сигнал зеркального освещения и длина отраженного луча (Specular Hit Noisy).
    /// </summary>
    kBufferTypeSpecularHitNoisy = 17,

    /// <summary>
    /// Очищенный от шума сигнал зеркального освещения (Specular Hit Denoised).
    /// </summary>
    kBufferTypeSpecularHitDenoised = 18,

    /// <summary>
    /// Зашумленный буфер теней (Shadow Noisy).
    /// </summary>
    kBufferTypeShadowNoisy = 19,

    /// <summary>
    /// Очищенный от шума буфер теней (Shadow Denoised).
    /// </summary>
    kBufferTypeShadowDenoised = 20,

    /// <summary>
    /// Зашумленный буфер фонового затенения (Ambient Occlusion Noisy).
    /// </summary>
    kBufferTypeAmbientOcclusionNoisy = 21,

    /// <summary>
    /// Очищенный от шума буфер фонового затенения (Ambient Occlusion Denoised).
    /// </summary>
    kBufferTypeAmbientOcclusionDenoised = 22,

    /// <summary>
    /// Опционально: Цвет и альфа элементов пользовательского интерфейса (UI/HUD Color and Alpha).
    /// <para><b>Важно:</b> Альфа-канал должен иметь достаточную точность (не используйте форматы вроде R10G10B10A2).</para>
    /// </summary>
    kBufferTypeUIColorAndAlpha = 23,

    /// <summary>
    /// Опциональная маска теней (1 если пиксель в тени, 0 в противном случае).
    /// </summary>
    kBufferTypeShadowHint = 24,

    /// <summary>
    /// Опциональная маска зеркальных отражений (1 если пиксель является отражением, 0 в противном случае).
    /// </summary>
    kBufferTypeReflectionHint = 25,

    /// <summary>
    /// Опциональная маска частиц (1 если пиксель представляет частицу, 0 в противном случае).
    /// </summary>
    kBufferTypeParticleHint = 26,

    /// <summary>
    /// Опциональная маска полупрозрачных поверхностей (1 если пиксель полупрозрачен, 0 в противном случае).
    /// </summary>
    kBufferTypeTransparencyHint = 27,

    /// <summary>
    /// Опциональная маска анимированных текстур (1 если пиксель принадлежит анимированной текстуре, 0 в противном случае).
    /// </summary>
    kBufferTypeAnimatedTextureHint = 28,

    /// <summary>
    /// Опциональный коэффициент смещения цвета текущего кадра относительно истории: lerp(history, current, bias) (1 для полного сброса истории).
    /// </summary>
    kBufferTypeBiasCurrentColorHint = 29,

    /// <summary>
    /// Опциональный буфер дистанции лучей трассировки (длина луча камеры).
    /// </summary>
    kBufferTypeRaytracingDistance = 30,

    /// <summary>
    /// Опциональные векторы движения для отражений (Reflection Motion Vectors).
    /// </summary>
    kBufferTypeReflectionMotionVectors = 31,

    /// <summary>
    /// Опциональный буфер мировых позиций фрагментов (Position), в той же системе координат, что и нормали.
    /// </summary>
    kBufferTypePosition = 32,

    /// <summary>
    /// Опциональная маска недействительной глубины/движения для пикселей с наложениями (например, PiP / Picture-in-Picture).
    /// </summary>
    kBufferTypeInvalidDepthMotionHint = 33,

    /// <summary>
    /// Буфер альфа-канала (Alpha).
    /// </summary>
    kBufferTypeAlpha = 34,

    /// <summary>
    /// Цветной буфер, содержащий только непрозрачную геометрию (Opaque Color).
    /// </summary>
    kBufferTypeOpaqueColor = 35,

    /// <summary>
    /// Опциональная маска реактивности (Reactive Mask): снижает доверие к истории, отдавая предпочтение текущему кадру (0 — по умолчанию, 1 — полностью реактивный).
    /// </summary>
    kBufferTypeReactiveMaskHint = 36,

    /// <summary>
    /// Опциональная маска прозрачности и композитинга (Transparency and Composition Mask): регулирует блокировку пикселей (pixel lock).
    /// </summary>
    kBufferTypeTransparencyAndCompositionMaskHint = 37,

    /// <summary>
    /// Опциональное альбедо точки попадания луча отражения (Reflected Albedo). Для многократных отскоков — первое незеркальное рассеяние.
    /// </summary>
    kBufferTypeReflectedAlbedo = 38,

    /// <summary>
    /// Опциональный цветной буфер сцены до отрисовки частиц (Color Before Particles).
    /// </summary>
    kBufferTypeColorBeforeParticles = 39,

    /// <summary>
    /// Опциональный цветной буфер сцены до отрисовки прозрачных объектов (Color Before Transparency).
    /// </summary>
    kBufferTypeColorBeforeTransparency = 40,

    /// <summary>
    /// Опциональный цветной буфер сцены до отрисовки тумана (Color Before Fog).
    /// </summary>
    kBufferTypeColorBeforeFog = 41,

    /// <summary>
    /// Опциональный буфер дистанции пересечения зеркальных лучей (Specular Hit Distance).
    /// </summary>
    kBufferTypeSpecularHitDistance = 42,

    /// <summary>
    /// Опциональный буфер с направлением (3 канала) и дистанцией (1 канал) зеркального луча.
    /// </summary>
    kBufferTypeSpecularRayDirectionHitDistance = 43,

    /// <summary>
    /// Опциональный буфер с нормализованным направлением зеркального луча (Specular Ray Direction).
    /// </summary>
    kBufferTypeSpecularRayDirection = 44,

    /// <summary>
    /// Опциональный буфер дистанции пересечения диффузных лучей (Diffuse Hit Distance).
    /// </summary>
    kBufferTypeDiffuseHitDistance = 45,

    /// <summary>
    /// Опциональный буфер с направлением (3 канала) и дистанцией (1 канал) диффузного луча.
    /// </summary>
    kBufferTypeDiffuseRayDirectionHitDistance = 46,

    /// <summary>
    /// Опциональный буфер с нормализованным направлением диффузного луча (Diffuse Ray Direction).
    /// </summary>
    kBufferTypeDiffuseRayDirection = 47,

    /// <summary>
    /// Опциональный буфер глубины в экранном разрешении (Hi-Res Depth).
    /// </summary>
    kBufferTypeHiResDepth = 48,

    /// <summary>
    /// Линейная глубина сцены (Linear Depth). Необходима при отсутствии <see cref="kBufferTypeDepth"/>.
    /// </summary>
    kBufferTypeLinearDepth = 49,

    /// <summary>
    /// Двунаправленное поле искажений (Bidirectional Distortion Field). 4 канала: RG — смещение от искаженного к исходному, BA — от исходного к искаженному.
    /// </summary>
    kBufferTypeBidirectionalDistortionField = 50,

    /// <summary>
    /// Опциональный слой прозрачности (Transparency Layer) для частиц и эффектов, рендеримых отдельно от основного цвета.
    /// </summary>
    kBufferTypeTransparencyLayer = 51,

    /// <summary>
    /// Трехканальная непрозрачность слоя прозрачности (Transparency Layer Opacity, RaGaBa).
    /// </summary>
    kBufferTypeTransparencyLayerOpacity = 52,

    /// <summary>
    /// Буфер цепочки показа (Swapchain backbuffer), отправляемый на экран.
    /// </summary>
    kBufferTypeBackbuffer = 53,

    /// <summary>
    /// Опциональная маска пикселей, исключаемых из интерполяции/деформации кадров (No Warp Mask).
    /// </summary>
    kBufferTypeNoWarpMask = 54,

    /// <summary>
    /// Цветной буфер после отрисовки частиц (для исследовательских целей).
    /// </summary>
    kBufferTypeColorAfterParticles = 55,

    /// <summary>
    /// Цветной буфер после отрисовки прозрачных объектов (для исследовательских целей).
    /// </summary>
    kBufferTypeColorAfterTransparency = 56,

    /// <summary>
    /// Цветной буфер после отрисовки тумана (для исследовательских целей).
    /// </summary>
    kBufferTypeColorAfterFog = 57,

    /// <summary>
    /// Направляющий буфер подповерхностного рассеяния в пространстве экрана (SSSSS Guide).
    /// </summary>
    kBufferTypeScreenSpaceSubsurfaceScatteringGuide = 58,

    /// <summary>
    /// Цветной буфер до подповерхностного рассеяния (для исследовательских целей).
    /// </summary>
    kBufferTypeColorBeforeScreenSpaceSubsurfaceScattering = 59,

    /// <summary>
    /// Цветной буфер после подповерхностного рассеяния (для исследовательских целей).
    /// </summary>
    kBufferTypeColorAfterScreenSpaceSubsurfaceScattering = 60,

    /// <summary>
    /// Направляющий буфер экранного преломления (SS Refraction Guide).
    /// </summary>
    kBufferTypeScreenSpaceRefractionGuide = 61,

    /// <summary>
    /// Цветной буфер до экранного преломления (для исследовательских целей).
    /// </summary>
    kBufferTypeColorBeforeScreenSpaceRefraction = 62,

    /// <summary>
    /// Цветной буфер после экранного преломления (для исследовательских целей).
    /// </summary>
    kBufferTypeColorAfterScreenSpaceRefraction = 63,

    /// <summary>
    /// Направляющий буфер глубины резкости (Depth of Field Guide) для нейросетевой реконструкции лучей.
    /// </summary>
    kBufferTypeDepthOfFieldGuide = 64,

    /// <summary>
    /// Цветной буфер до применения глубины резкости (для исследовательских целей).
    /// </summary>
    kBufferTypeColorBeforeDepthOfField = 65,

    /// <summary>
    /// Цветной буфер после применения глубины резкости (для исследовательских целей).
    /// </summary>
    kBufferTypeColorAfterDepthOfField = 66,

    /// <summary>
    /// Опциональный цветной буфер, переопределяющий альфа-канал масштабированного цвета (<see cref="kBufferTypeScalingOutputColor"/>).
    /// </summary>
    kBufferTypeScalingOutputAlpha = 67,

    /// <summary>
    /// Опциональная маска отклика/чувствительности (Responsivity Mask).
    /// </summary>
    kBufferTypeResponsivityMask = 68,

    /// <summary>
    /// Одноканальный ресурс альфы интерфейса (UI Alpha) со значениями [0.0..1.0] для оптимизированной производительности.
    /// </summary>
    kBufferTypeUIAlpha = 69
}
