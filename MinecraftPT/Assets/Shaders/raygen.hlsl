#include "common.hlsli"

RaytracingAccelerationStructure Scene : register(t0, space0);
ConstantBuffer<Camera> cam : register(b2, space0);

[[vk::image_format("rgba16f")]] RWTexture2D<float4> NoisyColorTarget : register(u6, space0);
[[vk::image_format("rgba16f")]] RWTexture2D<float4> NormalTarget : register(u7, space0);
[[vk::image_format("rgba8")]]   RWTexture2D<float4> AlbedoTarget : register(u9, space0);
[[vk::image_format("rgba8")]]   RWTexture2D<float4> SpecularAlbedoTarget : register(u10, space0);
[[vk::image_format("rg16f")]]   RWTexture2D<float2> MotionVectorsTarget : register(u11, space0);
[[vk::image_format("r32f")]]    RWTexture2D<float>  DepthTarget : register(u12, space0);
[[vk::image_format("rg16f")]]   RWTexture2D<float2> SpecularMotionVectorsTarget : register(u13, space0);
[[vk::image_format("r16f")]]    RWTexture2D<float>  SpecularHitDistanceTarget : register(u14, space0);
[[vk::image_format("r32f")]]    RWTexture2D<float>  LinearDepthTarget : register(u15, space0);
[[vk::image_format("rgba16f")]] RWTexture2D<float4> ColorBeforeTransparencyTarget : register(u16, space0);
[[vk::image_format("rgba16f")]] RWTexture2D<float4> DiffuseHitNoisyTarget : register(u17, space0);
[[vk::image_format("rgba16f")]] RWTexture2D<float4> SpecularHitNoisyTarget : register(u18, space0);
Texture2DArray<float4> PMJ02BNTexture : register(t19, space0);

// PMJ02BN низкодисперсионная выборка со стратифицированным синим шумом
float2 GetPmj02bn2D(uint2 pixel, uint sampleIndex, uint dimension, int bounce) {
    return SamplePmj02bn2D(PMJ02BNTexture, pixel, sampleIndex, dimension, bounce);
}

static const int MAX_TOTAL_BOUNCES = 8;
static const int MAX_DIFFUSE_BOUNCES = 2;

[shader("raygeneration")]
void main() {
    uint2 launchIndex = DispatchRaysIndex().xy;
    uint2 launchDim = DispatchRaysDimensions().xy;

    float2 pixelCenter = float2(launchIndex) + 0.5;
    float2 inUV = pixelCenter / float2(launchDim);
    float2 crd = inUV * 2.0 - 1.0;

    if (cam.JitterX != 0.0 || cam.JitterY != 0.0) {
        crd += float2(cam.JitterX, cam.JitterY) * (2.0 / float2(launchDim));
    }

    float4 target = mul(cam.InverseViewProj, float4(crd.x, crd.y, 1.0, 1.0));
    float3 baseRayDir = normalize(target.xyz / target.w - cam.LocalPosition);

    uint rayFlags = RAY_FLAG_CULL_BACK_FACING_TRIANGLES;
    uint shadowFlags = RAY_FLAG_ACCEPT_FIRST_HIT_AND_END_SEARCH | RAY_FLAG_SKIP_CLOSEST_HIT_SHADER | RAY_FLAG_CULL_BACK_FACING_TRIANGLES;
    uint cullMask = 0xFF;
    float tmin = 0.0;
    float tmax = 10000.0;

    float3 totalDiffuseRadiance = float3(0.0, 0.0, 0.0);
    float3 totalSpecularRadiance = float3(0.0, 0.0, 0.0);
    float3 totalColorBeforeTransparency = float3(0.0, 0.0, 0.0);
    float3 totalPrimaryEmission = float3(0.0, 0.0, 0.0);

    float3 primaryNormal = float3(0.0, 0.0, 0.0);
    float primaryRoughness = 1.0;
    float3 primaryDiffuseAlbedo = float3(0.0, 0.0, 0.0);
    float3 primarySpecularAlbedo = float3(0.0, 0.0, 0.0);
    float primaryDepth = 0.0;
    float primaryLinearDepth = 10000.0;
    float2 primaryMotionVector = float2(0.0, 0.0);
    float2 specularMotionVector = float2(0.0, 0.0);
    float specularHitDistance = 0.0;

    for (uint s = 0; s < cam.SamplesPerPixel; s++) {
        uint sampleIdx = cam.FrameCount * cam.SamplesPerPixel + s;
        float3 diffuseRadiance = float3(0.0, 0.0, 0.0);
        float3 specularRadiance = float3(0.0, 0.0, 0.0);
        float3 primaryEmission = float3(0.0, 0.0, 0.0);
        float3 glassReflection = float3(0.0, 0.0, 0.0);
        float3 throughput = float3(1.0, 1.0, 1.0);
        float3 rayOrigin = cam.LocalPosition;
        float3 rayDir = baseRayDir;

        float currentIor = 1.0;
        bool inMedium = false;
        float entryAlpha = 0.0;
        float3 entryAlbedo = float3(1.0, 1.0, 1.0);

        bool primaryDataWritten = false;
        bool primarySpecularWritten = false;
        bool firstOpaqueHitProcessed = false;
        bool pathIsSpecular = false;
        int diffuseBounces = 0;
        int surfaceBounces = 0;
        float3 primaryHitPos = float3(0.0, 0.0, 0.0);
        float2 primaryScreenNdc = float2(0.0, 0.0);

        for (int bounce = 0; bounce < MAX_TOTAL_BOUNCES; bounce++) {
            Payload payload = InitPayload();

            RayDesc ray;
            ray.Origin = rayOrigin;
            ray.TMin = tmin;
            ray.Direction = rayDir;
            ray.TMax = tmax;

            uint currentRayFlags = inMedium ? (rayFlags & ~RAY_FLAG_CULL_BACK_FACING_TRIANGLES) : rayFlags;
            TraceRay(Scene, currentRayFlags, cullMask, 0, 0, 0, ray, payload);

            float3 currentHitPos = (payload.hitDistance >= 0.0) ? (rayOrigin + rayDir * payload.hitDistance) : (rayOrigin + 10000.0 * rayDir);

            if (s == 0 && !primaryDataWritten) {
                bool isRoughTranslucent = (payload.ior > 1.0 || payload.opacity < 1.0) && (payload.roughness >= 0.05) && !inMedium;
                if (payload.opacity == 1.0 || payload.hitDistance < 0.0 || isRoughTranslucent) {
                    if (payload.hitDistance >= 0.0) {
                        primaryNormal = payload.normal;
                        primaryRoughness = payload.roughness;

                        float NdotV = saturate(dot(payload.normal, -baseRayDir));
                        float3 f0 = lerp(float3(0.04, 0.04, 0.04), payload.albedo, payload.metallic);

                        primaryDiffuseAlbedo = (1.0 - payload.metallic) * payload.albedo * (isRoughTranslucent ? payload.opacity : 1.0);
                        if (!primarySpecularWritten) {
                            primarySpecularAlbedo = EnvBRDFApprox(f0, payload.roughness, NdotV);
                            primarySpecularWritten = true;
                        }
                        primaryLinearDepth = dot(currentHitPos - cam.LocalPosition, cam.CameraFwd);

                        float4 clipPos = mul(cam.ViewProj, float4(currentHitPos, 1.0));
                        float3 ndcPos = clipPos.xyz / clipPos.w;
                        primaryDepth = ndcPos.z;

                        float4 prevClipPos = mul(cam.PrevViewProj, float4(currentHitPos, 1.0));
                        float3 prevNdcPos = prevClipPos.xyz / prevClipPos.w;

                        // Correct motion vector: (x_prev - x_curr) * 0.5 to convert NDC [-2, 2] to normalized screen UV [-1, 1]
                        primaryMotionVector = (prevNdcPos.xy - ndcPos.xy) * 0.5;
                        primaryHitPos = currentHitPos;
                        primaryScreenNdc = ndcPos.xy;
                    } else {
                        primaryNormal = float3(0.0, 0.0, 0.0);
                        primaryRoughness = 1.0;
                        primaryDiffuseAlbedo = float3(0.0, 0.0, 0.0);
                        if (!primarySpecularWritten) {
                            primarySpecularAlbedo = float3(0.0, 0.0, 0.0);
                            primarySpecularWritten = true;
                        }
                        primaryDepth = 0.0;
                        primaryLinearDepth = 10000.0;
                        primaryMotionVector = float2(0.0, 0.0);
                    }
                    primaryDataWritten = true;
                    if (isRoughTranslucent) {
                        firstOpaqueHitProcessed = true;
                    }
                }
            }

            // Track first specular bounce for opaque surfaces
            if (s == 0 && pathIsSpecular && specularHitDistance == 0.0 && bounce > 0) {
                float sDist = (payload.hitDistance >= 0.0) ? max(payload.hitDistance, 0.001) : 10000.0;
                specularHitDistance = sDist;

                // Виртуальная точка отражения в оптике: P_virtual = primaryHitPos + baseRayDir * sDist
                float3 pVirtual = primaryHitPos + baseRayDir * sDist;
                float4 sPrevClipPos = mul(cam.PrevViewProj, float4(pVirtual, 1.0));
                float2 sPrevNdc = sPrevClipPos.xy / sPrevClipPos.w;

                specularMotionVector = (sPrevNdc - primaryScreenNdc) * 0.5;
            }

            if (payload.hitDistance < 0.0) {
                float3 skyEmission = throughput * payload.emission;
                if (pathIsSpecular) {
                    specularRadiance += skyEmission;
                } else {
                    diffuseRadiance += skyEmission;
                }
                break;
            }

            // Surface emission
            float3 surfaceEmission = throughput * payload.emission;
            if (surfaceBounces >= 2) {
                // Подавление светляков на вторичных/многократных зеркальных отскоках (Firefly Clamping)
                surfaceEmission = min(surfaceEmission, float3(2.5, 2.5, 2.5));
            }
            if (!firstOpaqueHitProcessed) {
                // Прямой взгляд на излучающую поверхность: сохраняем в primaryEmission
                primaryEmission += surfaceEmission;
            } else if (pathIsSpecular) {
                specularRadiance += surfaceEmission;
            } else {
                diffuseRadiance += surfaceEmission;
            }

            float3 surfaceNormal = payload.normal;
            float3 surfaceAlbedo = payload.albedo;
            float surfaceRoughness = payload.roughness;
            float surfaceMetallic = payload.metallic;
            float3 surfaceHitPos = currentHitPos;

            // Translucent media (glass, water, etc.) with refraction and spectral Beer-Lambert absorption
            bool isGlass = (payload.ior > 1.0);
            if (isGlass) {
                // Всегда ориентируем рабочую нормаль N навстречу направлению луча (dot(rayDir, N) <= 0.0)
                float3 N = (dot(rayDir, surfaceNormal) < 0.0) ? surfaceNormal : -surfaceNormal;

                if (!inMedium) {
                    // --- ВХОД В СРЕДУ (FRONT FACE) ---
                    // 1. Френелевское отражение на границе «воздух -> стекло»
                    float cosTheta = saturate(dot(-rayDir, N));
                    float r0 = pow((1.0 - payload.ior) / (1.0 + payload.ior), 2.0); // ~0.04 для стекла
                    float F = r0 + (1.0 - r0) * pow(1.0 - cosTheta, 5.0);

                    // Зеркальное отражение сцены на передней грани стекла (Mirror Reflection)
                    if (bounce == 0) {
                        float3 specDir = reflect(rayDir, N);
                        if (payload.roughness > 0.0) {
                            float3 b1, b2;
                            BuildOrthonormalBasis(specDir, b1, b2);
                            float2 uSpec = GetPmj02bn2D(launchIndex, sampleIdx, 1, bounce * 4 + 1);
                            float2 disk = SampleUnitDisk(uSpec) * (payload.roughness * payload.roughness);
                            specDir = normalize(specDir + b1 * disk.x + b2 * disk.y);
                        }

                        float3 specOrigin = surfaceHitPos + N * 0.001;
                        Payload specPayload = InitPayload();
                        RayDesc specRay;
                        specRay.Origin = specOrigin;
                        specRay.TMin = tmin;
                        specRay.Direction = specDir;
                        specRay.TMax = tmax;

                        TraceRay(Scene, rayFlags, cullMask, 0, 0, 0, specRay, specPayload);

                        float sDist = (specPayload.hitDistance >= 0.0) ? max(specPayload.hitDistance, 0.001) : 10000.0;
                        float3 specHitPos = specOrigin + specDir * sDist;

                        if (s == 0) {
                            specularHitDistance = sDist;
                            float3 pVirtual = surfaceHitPos + baseRayDir * sDist;
                            float4 sPrevClipPos = mul(cam.PrevViewProj, float4(pVirtual, 1.0));
                            float2 sPrevNdc = sPrevClipPos.xy / sPrevClipPos.w;

                            float4 clipPos = mul(cam.ViewProj, float4(surfaceHitPos, 1.0));
                            float2 currNdc = clipPos.xy / clipPos.w;

                            specularMotionVector = (sPrevNdc - currNdc) * 0.5;
                            primarySpecularAlbedo = EnvBRDFApprox(float3(0.04, 0.04, 0.04), payload.roughness, cosTheta);
                            primarySpecularWritten = true;
                        }

                        float3 reflectedColor = specPayload.emission;
                        if (specPayload.hitDistance >= 0.0) {
                            float3 sSunDir = normalize(cam.SunDirection.xyz);
                            float sNdotL = max(dot(specPayload.normal, sSunDir), 0.0);
                            
                            Payload shadowPayload;
                            shadowPayload.hitDistance = 1.0;
                            RayDesc shadowRay;
                            shadowRay.Origin = specHitPos + specPayload.normal * 0.001;
                            shadowRay.TMin = 0.0;
                            shadowRay.Direction = sSunDir;
                            shadowRay.TMax = 10000.0;
                            TraceRay(Scene, shadowFlags, cullMask, 0, 0, 0, shadowRay, shadowPayload);
                            float3 sunLightColor = pow(float3(1.0, 0.95, 0.8), 2.2) * 5.0;
                            float3 skyAmbColor = pow(float3(0.4, 0.6, 0.9), 2.2) * 1.5;
                            float skyFactor = saturate(specPayload.normal.y * 0.5 + 0.5);
                            float3 directSun = (shadowPayload.hitDistance < 0.0) ? (sunLightColor * sNdotL) : float3(0.0, 0.0, 0.0);
                            reflectedColor += (specPayload.albedo / 3.14159265) * (directSun + skyAmbColor * skyFactor);
                        } else {
                            float3 skyColor = lerp(pow(float3(0.8, 0.85, 0.9), 2.2), pow(float3(0.4, 0.6, 0.9), 2.2), saturate(specDir.y * 0.5 + 0.5));
                            reflectedColor += skyColor;
                        }

                        glassReflection += throughput * F * reflectedColor;
                    }

                    throughput *= (1.0 - F);

                    // 3. Сохранение параметров среды на входе
                    entryAlpha = payload.opacity;
                    entryAlbedo = surfaceAlbedo;
                    inMedium = true;
                    currentIor = payload.ior;

                    // 4. Физическое преломление луча (Snell's law: air -> glass, eta = 1.0 / IOR)
                    float eta = 1.0 / payload.ior;
                    float3 refrDir = refract(rayDir, N, eta);
                    if (dot(refrDir, refrDir) > 0.01) {
                        rayDir = normalize(refrDir);
                    }

                    // 5. Микрофасетное рассеивание при входе в шероховатую среду (Roughness / Frosting)
                    if (payload.roughness > 0.0) {
                        float3 b1, b2;
                        BuildOrthonormalBasis(rayDir, b1, b2);
                        float2 uRefr = GetPmj02bn2D(launchIndex, sampleIdx, 3, bounce * 4 + 2);
                        float2 disk = SampleUnitDisk(uRefr) * (payload.roughness * payload.roughness);
                        float3 scatteredDir = normalize(rayDir + b1 * disk.x + b2 * disk.y);
                        if (dot(scatteredDir, -N) > 0.01) {
                            rayDir = scatteredDir;
                        }
                    }

                    rayOrigin = surfaceHitPos + rayDir * 0.001;
                    continue;
                } else {
                    // --- ВЫХОД ИЗ СРЕДЫ (BACK FACE) ---
                    // 1. Расчет спектрального поглощения по закону Бера-Ламберта
                    float distInMedium = max(payload.hitDistance, 0.001);
                    float exitBodyAlpha = payload.opacity;
                    float3 exitBodyAlbedo = surfaceAlbedo;

                    // Усреднение прозрачности и альбедо на входе и выходе
                    float avgAlpha = 0.5 * (entryAlpha + exitBodyAlpha);
                    float3 avgAlbedo = 0.5 * (entryAlbedo + exitBodyAlbedo);

                    // Спектральное поглощение по всем каналам RGB и альфа
                    float absorptionScale = max(payload.absorption, 1.0);
                    float3 sigmaA = avgAlpha * absorptionScale * (float3(1.0, 1.0, 1.0) - avgAlbedo);
                    float3 transmittance = exp(-sigmaA * distInMedium);
                    throughput *= transmittance;
                    if (max(throughput.r, max(throughput.g, throughput.b)) < 0.02) {
                        break;
                    }

                    // 2. Преломление луча при выходе в воздух (Snell's law: glass -> air, eta = currentIor)
                    float eta = currentIor;
                    float3 refrDir = refract(rayDir, N, eta);
                    if (dot(refrDir, refrDir) > 0.01) {
                        rayDir = normalize(refrDir);

                        // 4. Микрофасетное рассеивание при выходе из шероховатой среды
                        if (payload.roughness > 0.0) {
                            float3 b1, b2;
                            BuildOrthonormalBasis(rayDir, b1, b2);
                            float2 uRefrExit = GetPmj02bn2D(launchIndex, sampleIdx, 3, bounce * 4 + 3);
                            float2 disk = SampleUnitDisk(uRefrExit) * (payload.roughness * payload.roughness);
                            float3 scatteredDir = normalize(rayDir + b1 * disk.x + b2 * disk.y);
                            if (dot(scatteredDir, -N) > 0.01) {
                                rayDir = scatteredDir;
                            }
                        }

                        inMedium = false;
                        currentIor = 1.0;
                        rayOrigin = surfaceHitPos + rayDir * 0.001;
                        continue;
                    } else {
                        // Полное внутреннее отражение (TIR): луч отражается обратно внутрь стекла
                        rayDir = reflect(rayDir, N);
                        inMedium = true;
                        rayOrigin = surfaceHitPos + rayDir * 0.001;
                        continue;
                    }
                }
            }

            float3 safeOrigin = surfaceHitPos + surfaceNormal * 0.001;

            // Direct Lighting (Next Event Estimation)
            float3 sunDir = normalize(cam.SunDirection.xyz);
            float2 sunDiskRand = GetPmj02bn2D(launchIndex, sampleIdx, 2, bounce * 4 + 0);
            float3 lightDir = SampleSunDirection(sunDir, sunDiskRand, 0.05);

            float nDotL = max(dot(surfaceNormal, lightDir), 0.0);
            if (nDotL > 0.0) {
                Payload shadowPayload;
                shadowPayload.hitDistance = 1.0; 
                
                RayDesc shadowRay;
                shadowRay.Origin = safeOrigin;
                shadowRay.TMin = 0.0;
                shadowRay.Direction = lightDir;
                shadowRay.TMax = 10000.0;

                TraceRay(Scene, shadowFlags, cullMask, 0, 0, 0, shadowRay, shadowPayload);

                if (shadowPayload.hitDistance < 0.0) {
                    float3 sunLightColor = pow(float3(1.0, 0.95, 0.8), 2.2) * 5.0; 
                    float3 diffuse = surfaceAlbedo / 3.14159265;
                    float3 directRadiance = throughput * diffuse * sunLightColor * nDotL;

                    if (!firstOpaqueHitProcessed) {
                        diffuseRadiance += (1.0 - surfaceMetallic) * directRadiance;
                        specularRadiance += surfaceMetallic * directRadiance;
                    } else if (pathIsSpecular) {
                        specularRadiance += directRadiance;
                    } else {
                        diffuseRadiance += directRadiance;
                    }
                }
            } else if (inMedium) {
                // Обратная сторона тонкой грани стекла (просвечивание прямого солнечного света с внешней стороны)
                float backNdotL = max(dot(-surfaceNormal, lightDir), 0.0);
                if (backNdotL > 0.0) {
                    Payload shadowPayload;
                    shadowPayload.hitDistance = 1.0;
                    RayDesc shadowRay;
                    shadowRay.Origin = surfaceHitPos - surfaceNormal * 0.001;
                    shadowRay.TMin = 0.0;
                    shadowRay.Direction = lightDir;
                    shadowRay.TMax = 10000.0;
                    TraceRay(Scene, shadowFlags, cullMask, 0, 0, 0, shadowRay, shadowPayload);
                    if (shadowPayload.hitDistance < 0.0) {
                        float3 sunLightColor = pow(float3(1.0, 0.95, 0.8), 2.2) * 5.0;
                        float3 diffuse = surfaceAlbedo / 3.14159265;
                        float3 directRadiance = throughput * diffuse * sunLightColor * backNdotL * 0.7;

                        if (!firstOpaqueHitProcessed) {
                            diffuseRadiance += (1.0 - surfaceMetallic) * directRadiance;
                            specularRadiance += surfaceMetallic * directRadiance;
                        } else if (pathIsSpecular) {
                            specularRadiance += directRadiance;
                        } else {
                            diffuseRadiance += directRadiance;
                        }
                    }
                }
            }

            float2 diffuseRand = GetPmj02bn2D(launchIndex, sampleIdx, 0, bounce * 4 + 1);
            float3 diffuseDir = SampleCosineHemisphere(surfaceNormal, diffuseRand);
            
            float2 matRand = GetPmj02bn2D(launchIndex, sampleIdx, 3, bounce * 4 + 3);
            bool isSpecular = matRand.y < surfaceMetallic;
            if (!firstOpaqueHitProcessed) {
                pathIsSpecular = isSpecular;
                firstOpaqueHitProcessed = true;
            }

            float3 nextDir;
            float3 pathWeight;

            if (isSpecular) {
                float2 reflectRand = GetPmj02bn2D(launchIndex, sampleIdx, 1, bounce * 4 + 2);
                float3 V = -rayDir;
                float3 H;
                float effectiveRoughness = surfaceRoughness;
                if (surfaceBounces > 0) {
                    effectiveRoughness = max(surfaceRoughness, 0.25);
                }
                if (effectiveRoughness < 0.02) {
                    H = surfaceNormal;
                } else {
                    H = SampleGGXVNDF(V, surfaceNormal, effectiveRoughness, reflectRand);
                }
                float3 L = reflect(-V, H);
                // Гарантируем, что отраженный луч не уходит под геометрический горизонт поверхности
                if (dot(L, surfaceNormal) < 0.001) {
                    L = normalize(L - 2.0 * dot(L, surfaceNormal) * surfaceNormal);
                }
                nextDir = L;
                float3 f0 = lerp(float3(0.04, 0.04, 0.04), surfaceAlbedo, surfaceMetallic);
                pathWeight = EvalGGXVNDFWeight(V, L, H, surfaceNormal, effectiveRoughness, f0);
            } else {
                diffuseBounces++;
                nextDir = diffuseDir;
                pathWeight = surfaceAlbedo;
            }

            surfaceBounces++;
            throughput *= pathWeight;

            // Early out for paths with negligible energy (< 2%)
            float pSurvive = saturate(max(throughput.r, max(throughput.g, throughput.b)));
            if (pSurvive < 0.02) {
                break;
            }

            // Unbiased Russian Roulette path termination с контролем дисперсии
            if (diffuseBounces >= 2 || surfaceBounces >= 3) {
                float clampedPSurvive = clamp(pSurvive, 0.15, 0.95);
                if (matRand.x > clampedPSurvive) {
                    break;
                }
                throughput /= clampedPSurvive;
            }

            if (diffuseBounces >= MAX_DIFFUSE_BOUNCES) {
                break;
            }

            rayOrigin = safeOrigin;
            rayDir = nextDir;
        }
        
        specularRadiance += glassReflection;

        if (any(isnan(diffuseRadiance))) diffuseRadiance = float3(0.0, 0.0, 0.0);
        if (any(isnan(specularRadiance))) specularRadiance = float3(0.0, 0.0, 0.0);
        if (any(isnan(primaryEmission))) primaryEmission = float3(0.0, 0.0, 0.0);

        totalDiffuseRadiance += diffuseRadiance;
        totalSpecularRadiance += specularRadiance;
        totalPrimaryEmission += primaryEmission;
        float3 baseSpecular = max(specularRadiance - glassReflection, float3(0.0, 0.0, 0.0));
        totalColorBeforeTransparency += diffuseRadiance + baseSpecular + primaryEmission;
    }
    
    totalDiffuseRadiance /= float(cam.SamplesPerPixel);
    totalSpecularRadiance /= float(cam.SamplesPerPixel);
    totalColorBeforeTransparency /= float(cam.SamplesPerPixel);
    totalPrimaryEmission /= float(cam.SamplesPerPixel);

    float3 combinedNoisy = totalDiffuseRadiance + totalSpecularRadiance + totalPrimaryEmission;

    // Корректная демодуляция яркости для DLSS Ray Reconstruction / NRD с безопасным ограничением
    float3 demodulatedDiffuse;
    if (primaryLinearDepth >= 9999.0 || all(primaryDiffuseAlbedo <= 0.0001)) {
        demodulatedDiffuse = totalDiffuseRadiance;
    } else {
        demodulatedDiffuse = totalDiffuseRadiance / max(primaryDiffuseAlbedo, float3(0.01, 0.01, 0.01));
    }
    demodulatedDiffuse = min(demodulatedDiffuse, float3(20.0, 20.0, 20.0));

    float3 demodulatedSpecular;
    if (primaryLinearDepth >= 9999.0 || all(primarySpecularAlbedo <= 0.0001)) {
        demodulatedSpecular = totalSpecularRadiance;
    } else {
        demodulatedSpecular = totalSpecularRadiance / max(primarySpecularAlbedo, float3(0.02, 0.02, 0.02));
    }
    demodulatedSpecular = min(demodulatedSpecular, float3(20.0, 20.0, 20.0));

    // Запись G-буферов с кодированием дистанций в альфа-каналы
    DiffuseHitNoisyTarget[launchIndex] = float4(demodulatedDiffuse, primaryLinearDepth);
    SpecularHitNoisyTarget[launchIndex] = float4(demodulatedSpecular, specularHitDistance);
    NoisyColorTarget[launchIndex] = float4(combinedNoisy, 1.0);
    NormalTarget[launchIndex] = float4(primaryNormal, primaryRoughness);
    AlbedoTarget[launchIndex] = float4(primaryDiffuseAlbedo, 1.0);
    SpecularAlbedoTarget[launchIndex] = float4(primarySpecularAlbedo, 1.0);
    MotionVectorsTarget[launchIndex] = primaryMotionVector;
    DepthTarget[launchIndex] = primaryDepth;
    LinearDepthTarget[launchIndex] = primaryLinearDepth;
    ColorBeforeTransparencyTarget[launchIndex] = float4(totalColorBeforeTransparency, 1.0);
    SpecularMotionVectorsTarget[launchIndex] = specularMotionVector;
    SpecularHitDistanceTarget[launchIndex] = specularHitDistance;
}
