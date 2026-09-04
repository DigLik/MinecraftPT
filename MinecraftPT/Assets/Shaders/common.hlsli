#ifndef COMMON_HLSLI
#define COMMON_HLSLI

#include "structs.hlsli"
#include "sampling.hlsli"

// Фабричная функция инициализации Payload всеми валидными значениями по умолчанию
Payload InitPayload() {
    Payload p;
    p.hitDistance = -1.0;
    p.normal = float3(0.0, 0.0, 0.0);
    p.roughness = 1.0;
    p.albedo = float3(0.0, 0.0, 0.0);
    p.metallic = 0.0;
    p.emission = float3(0.0, 0.0, 0.0);
    p.opacity = 1.0;
    p.ior = 1.0;
    p.absorption = 0.0;
    p.frontFacing = 1.0;
    return p;
}

#endif // COMMON_HLSLI
