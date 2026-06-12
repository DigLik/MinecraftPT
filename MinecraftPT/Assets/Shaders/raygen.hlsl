struct Payload {
    float3 hitPos;
    float hitDistance;
    float3 normal;
    float roughness;
    float3 albedo;
    float metallic;
    float3 emission;
    float opacity;
    float ior;
    float absorption;
    float frontFacing;
};

struct Camera {
    column_major float4x4 ViewProj;
    column_major float4x4 InverseViewProj;
    column_major float4x4 PrevViewProj;
    int3 ChunkPosition;
    uint FrameCount;
    float3 LocalPosition;
    uint SamplesPerPixel;
    float4 SunDirection;
    float3 CameraUp;
    uint Seed;
    float3 CameraRight;
    float JitterX;
    float3 CameraFwd;
    float JitterY;
};

RaytracingAccelerationStructure Scene : register(t0, space0);
RWTexture2D<float4> RenderTarget : register(u1, space0);
ConstantBuffer<Camera> cam : register(b2, space0);

RWTexture2D<float4> NoisyColorTarget : register(u6, space0);
RWTexture2D<float4> NormalTarget : register(u7, space0);
RWTexture2D<float> RoughnessTarget : register(u8, space0);
RWTexture2D<float4> AlbedoTarget : register(u9, space0);
RWTexture2D<float4> SpecularAlbedoTarget : register(u10, space0);
RWTexture2D<float2> MotionVectorsTarget : register(u11, space0);
RWTexture2D<float> DepthTarget : register(u12, space0);
RWTexture2D<float2> SpecularMotionVectorsTarget : register(u13, space0);
RWTexture2D<float> SpecularHitDistanceTarget : register(u14, space0);
RWTexture2D<float> LinearDepthTarget : register(u15, space0);
RWTexture2D<float4> ColorBeforeTransparencyTarget : register(u16, space0);

static uint seed;
uint pcg_hash() {
    seed = seed * 747796405u + 2891336453u;
    uint word = ((seed >> ((seed >> 28u) + 4u)) ^ seed) * 277803737u;
    return (word >> 22u) ^ word;
}

float rnd() {
    return float(pcg_hash()) / 4294967296.0;
}

void update_seed(float3 p) {
    uint h = uint(abs(p.x) * 73856093u) ^ uint(abs(p.y) * 19349663u) ^ uint(abs(p.z) * 83492791u);
    seed = seed ^ h;
    seed = pcg_hash();
}

float3 random_on_unit_sphere() {
    float z = rnd() * 2.0 - 1.0;
    float a = rnd() * 6.28318530718;
    float r = sqrt(1.0 - z * z);
    return float3(r * cos(a), r * sin(a), z);
}

float3 random_in_unit_sphere() {
    return random_on_unit_sphere() * pow(rnd(), 1.0/3.0);
}

float3 random_cosine_hemisphere(float3 n) {
    float r1 = rnd();
    float r2 = rnd();
    float phi = r1 * 6.28318530718;
    float r = sqrt(r2);
    float x = r * cos(phi);
    float y = r * sin(phi);
    float z = sqrt(max(0.0, 1.0 - r2));
    
    float3 w = n;
    float3 u = normalize(cross((abs(w.x) > 0.1 ? float3(0.0, 1.0, 0.0) : float3(1.0, 0.0, 0.0)), w));
    float3 v = cross(w, u);
    
    return x * u + y * v + z * w;
}

[shader("raygeneration")]
void main() {
    uint2 launchIndex = DispatchRaysIndex().xy;
    uint2 launchDim = DispatchRaysDimensions().xy;

    seed = cam.Seed + launchIndex.y * launchDim.x + launchIndex.x;
    seed = pcg_hash();

    float2 crd = float2(launchIndex) / float2(launchDim);
    crd = crd * 2.0 - 1.0;

    if (cam.JitterX != 0.0 || cam.JitterY != 0.0) {
        crd += float2(cam.JitterX, cam.JitterY) * (2.0 / float2(launchDim));
    } else if (cam.FrameCount > 1) {
        float2 jitter = float2(rnd() - 0.5, rnd() - 0.5) / float2(launchDim);
        crd += jitter;
    }

    float4 target = mul(cam.InverseViewProj, float4(crd.x, crd.y, 1.0, 1.0));
    float3 baseRayDir = normalize(target.xyz / target.w - cam.LocalPosition);

    uint rayFlags = RAY_FLAG_NONE;
    uint cullMask = 0xFF;
    float tmin = 0.0;
    float tmax = 10000.0;

    float3 finalColor = float3(0.0, 0.0, 0.0);
    float3 finalColorBeforeTransparency = float3(0.0, 0.0, 0.0);

    float3 primaryNormal = float3(0.0, 0.0, 0.0);
    float primaryRoughness = 1.0;
    float3 primaryAlbedo = float3(0.0, 0.0, 0.0);
    float3 primarySpecularAlbedo = float3(0.0, 0.0, 0.0);
    float primaryDepth = 0.0;
    float primaryLinearDepth = 10000.0;
    float2 primaryMotionVector = float2(0.0, 0.0);
    float2 specularMotionVector = float2(0.0, 0.0);
    float specularHitDistance = 0.0;

    for (uint s = 0; s < cam.SamplesPerPixel; s++) {
        float3 radiance = float3(0.0, 0.0, 0.0);
        float3 glassReflection = float3(0.0, 0.0, 0.0);
        float3 throughput = float3(1.0, 1.0, 1.0);
        float3 rayOrigin = cam.LocalPosition;
        float3 rayDir = baseRayDir;
        bool primaryIsSpecular = false;

        float currentIor = 1.0;
        float currentAbsorption = 0.0;
        float3 currentAlbedo = float3(1.0, 1.0, 1.0);

        bool primaryDataWritten = false;

        for (int bounce = 0; bounce < 32; bounce++) {
            Payload payload;
            payload.hitDistance = -1.0;

            RayDesc ray;
            ray.Origin = rayOrigin;
            ray.TMin = tmin;
            ray.Direction = rayDir;
            ray.TMax = tmax;

            TraceRay(Scene, rayFlags, cullMask, 0, 0, 0, ray, payload);

            if (s == 0 && !primaryDataWritten) {
                if (payload.opacity == 1.0 || payload.hitDistance < 0.0) {
                    if (payload.hitDistance >= 0.0) {
                        primaryNormal = payload.normal;
                        primaryRoughness = payload.roughness;
                        primaryAlbedo = payload.albedo;
                        primarySpecularAlbedo = lerp(float3(0.04, 0.04, 0.04), payload.albedo, payload.metallic);
                        primaryLinearDepth = dot(payload.hitPos - cam.LocalPosition, cam.CameraFwd);

                        float4 clipPos = mul(cam.ViewProj, float4(payload.hitPos, 1.0));
                        float3 ndcPos = clipPos.xyz / clipPos.w;
                        primaryDepth = ndcPos.z;

                        float4 prevClipPos = mul(cam.PrevViewProj, float4(payload.hitPos, 1.0));
                        float3 prevNdcPos = prevClipPos.xyz / prevClipPos.w;

                        primaryMotionVector = ndcPos.xy - prevNdcPos.xy;
                    } else {
                        primaryNormal = float3(0.0, 0.0, 0.0);
                        primaryRoughness = 1.0;
                        primaryAlbedo = float3(0.0, 0.0, 0.0);
                        primarySpecularAlbedo = float3(0.0, 0.0, 0.0);
                        primaryDepth = 0.0;
                        primaryLinearDepth = 10000.0;
                        primaryMotionVector = float2(0.0, 0.0);
                    }
                    primaryDataWritten = true;
                }
            }

            if (payload.hitDistance < 0.0) {
                radiance += throughput * payload.emission;
                break;
            }

            // Apply Beer-Lambert absorption
            float activeAbsorption = (payload.frontFacing == 0.0) ? payload.absorption : currentAbsorption;
            float3 activeAlbedo = (payload.frontFacing == 0.0) ? payload.albedo : currentAlbedo;
            if (activeAbsorption > 0.0 && payload.hitDistance >= 0.0) {
                throughput *= exp(-activeAbsorption * payload.hitDistance * (float3(1.0, 1.0, 1.0) - activeAlbedo));
            }

            radiance += throughput * payload.emission;

            float3 surfaceNormal = payload.normal;
            float3 surfaceAlbedo = payload.albedo;
            float surfaceRoughness = payload.roughness;
            float surfaceMetallic = payload.metallic;
            float3 surfaceHitPos = payload.hitPos;

            // Handle translucent refraction and Fresnel reflections
            if (payload.opacity < 1.0) {
                bool isCoincidentTransition = (payload.frontFacing == 0.0 && currentIor == 1.0);
                float eta = (currentIor == 1.0) ? (1.0 / payload.ior) : (payload.ior / 1.0);
                if (isCoincidentTransition) {
                    eta = 1.0;
                }
                float3 refractDir = refract(rayDir, surfaceNormal, eta);

                bool doRefract = false;
                float F = 0.0;
                if (isCoincidentTransition) {
                    F = 0.0;
                    doRefract = true;
                } else if (any(refractDir != float3(0.0, 0.0, 0.0))) {
                    float r0 = (1.0 - payload.ior) / (1.0 + payload.ior);
                    r0 = r0 * r0;
                    
                    float cosX = abs(dot(rayDir, surfaceNormal));
                    if (currentIor > 1.0) {
                        cosX = abs(dot(refractDir, surfaceNormal));
                    }
                    F = r0 + (1.0 - r0) * pow(1.0 - cosX, 5.0);

                    if (bounce == 0) {
                        doRefract = true;
                    } else {
                        if (rnd() > F) {
                            doRefract = true;
                        }
                    }
                } else {
                    F = 1.0;
                }

                if (doRefract) {
                    if (bounce == 0) {
                        // Ray Splitting: Trace the reflection path analytically to eliminate Monte Carlo noise
                        float3 savedHitPos = surfaceHitPos;
                        float3 savedNormal = surfaceNormal;
                        float3 savedRefractDir = refractDir;
                        float savedF = F;
                        float3 savedAlbedo = surfaceAlbedo;
                        float savedIor = payload.ior;
                        float savedAbsorption = payload.absorption;
                        float savedFrontFacing = payload.frontFacing;

                        float3 specRadiance = float3(0.0, 0.0, 0.0);
                        float3 specThroughput = float3(1.0, 1.0, 1.0);
                        float3 specOrigin = surfaceHitPos + surfaceNormal * 0.001;
                        update_seed(surfaceHitPos);
                        float3 specDir = normalize(reflect(rayDir, surfaceNormal) + random_in_unit_sphere() * surfaceRoughness);
                        
                        float specCurrentIor = currentIor;
                        float specCurrentAbsorption = currentAbsorption;
                        float3 specCurrentAlbedo = currentAlbedo;

                        for (int rBounce = 1; rBounce < 6; rBounce++) {
                            Payload rPayload;
                            rPayload.hitDistance = -1.0;

                            RayDesc rRay;
                            rRay.Origin = specOrigin;
                            rRay.TMin = tmin;
                            rRay.Direction = specDir;
                            rRay.TMax = tmax;

                            TraceRay(Scene, rayFlags, cullMask, 0, 0, 0, rRay, rPayload);

                            if (s == 0 && rBounce == 1) {
                                float3 hitPos = rPayload.hitDistance >= 0.0 ? rPayload.hitPos : (specOrigin + 1000.0 * specDir);
                                float4 clipPos = mul(cam.ViewProj, float4(hitPos, 1.0));
                                float3 ndcPos = clipPos.xyz / clipPos.w;

                                float4 prevClipPos = mul(cam.PrevViewProj, float4(hitPos, 1.0));
                                float3 prevNdcPos = prevClipPos.xyz / prevClipPos.w;

                                specularMotionVector = ndcPos.xy - prevNdcPos.xy;
                                primarySpecularAlbedo = float3(F, F, F);
                                specularHitDistance = rPayload.hitDistance;
                            }

                            if (rPayload.hitDistance < 0.0) {
                                specRadiance += specThroughput * rPayload.emission;
                                break;
                            }

                            // Apply Beer-Lambert absorption
                            float specActiveAbs = (rPayload.frontFacing == 0.0) ? rPayload.absorption : specCurrentAbsorption;
                            float3 specActiveAlb = (rPayload.frontFacing == 0.0) ? rPayload.albedo : specCurrentAlbedo;
                            if (specActiveAbs > 0.0 && rPayload.hitDistance >= 0.0) {
                                specThroughput *= exp(-specActiveAbs * rPayload.hitDistance * (float3(1.0, 1.0, 1.0) - specActiveAlb));
                            }

                            specRadiance += specThroughput * rPayload.emission;

                            float3 rNormal = rPayload.normal;
                            float3 rAlbedo = rPayload.albedo;
                            float rRoughness = rPayload.roughness;
                            float rMetallic = rPayload.metallic;
                            float3 rHitPos = rPayload.hitPos;

                            // Handle secondary translucent hits stochastically
                            if (rPayload.opacity < 1.0) {
                                bool rIsCoincidentTransition = (rPayload.frontFacing == 0.0 && specCurrentIor == 1.0);
                                float rEta = (specCurrentIor == 1.0) ? (1.0 / rPayload.ior) : (rPayload.ior / 1.0);
                                if (rIsCoincidentTransition) {
                                    rEta = 1.0;
                                }
                                float3 rRefract = refract(specDir, rNormal, rEta);
                                bool rDoRefract = false;
                                if (rIsCoincidentTransition) {
                                    rDoRefract = true;
                                } else if (any(rRefract != float3(0.0, 0.0, 0.0))) {
                                    float r_r0 = (1.0 - rPayload.ior) / (1.0 + rPayload.ior);
                                    r_r0 = r_r0 * r_r0;
                                    float rCos = abs(dot(specDir, rNormal));
                                    if (specCurrentIor > 1.0) {
                                        rCos = abs(dot(rRefract, rNormal));
                                    }
                                    float rF = r_r0 + (1.0 - r_r0) * pow(1.0 - rCos, 5.0);
                                    if (rnd() > rF) rDoRefract = true;
                                }

                                if (rDoRefract) {
                                    specDir = rRefract;
                                    specOrigin = rHitPos - rNormal * 0.001;
                                    if (rPayload.frontFacing == 1.0) {
                                        specCurrentIor = rPayload.ior;
                                        specCurrentAbsorption = rPayload.absorption;
                                        specCurrentAlbedo = rAlbedo;
                                    } else {
                                        specCurrentIor = 1.0;
                                        specCurrentAbsorption = 0.0;
                                        specCurrentAlbedo = float3(1.0, 1.0, 1.0);
                                    }
                                    specThroughput *= rAlbedo;
                                    continue;
                                } else {
                                    update_seed(rHitPos);
                                    specDir = normalize(reflect(specDir, rNormal) + random_in_unit_sphere() * rRoughness);
                                    specOrigin = rHitPos + rNormal * 0.001;
                                    continue;
                                }
                            }

                            // Direct Lighting (NEE)
                            float3 rSafeOrigin = rHitPos + rNormal * 0.001;
                            float3 sunDir = normalize(cam.SunDirection.xyz);
                            update_seed(rHitPos);
                            float3 lightDir = normalize(sunDir + random_in_unit_sphere() * 0.05);
                            float nDotL = max(dot(rNormal, lightDir), 0.0);
                            if (nDotL > 0.0) {
                                Payload shadowPayload;
                                shadowPayload.hitDistance = 1.0;
                                uint shadowFlags = RAY_FLAG_ACCEPT_FIRST_HIT_AND_END_SEARCH | RAY_FLAG_SKIP_CLOSEST_HIT_SHADER;
                                
                                RayDesc shadowRay;
                                shadowRay.Origin = rSafeOrigin;
                                shadowRay.TMin = 0.0;
                                shadowRay.Direction = lightDir;
                                shadowRay.TMax = 10000.0;

                                TraceRay(Scene, shadowFlags, cullMask, 0, 0, 0, shadowRay, shadowPayload);

                                if (shadowPayload.hitDistance < 0.0) {
                                    float3 sunLightColor = pow(float3(1.0, 0.95, 0.8), 2.2) * 5.0;
                                    float3 diffuse = rAlbedo / 3.14159265;
                                    specRadiance += specThroughput * diffuse * sunLightColor * nDotL;
                                }
                            }

                            update_seed(rHitPos);
                            float3 diffuseDir = random_cosine_hemisphere(rNormal);
                            float3 reflectDir = normalize(reflect(specDir, rNormal) + random_in_unit_sphere() * rRoughness);
                            bool isSpecular = rnd() < rMetallic;
                            specDir = isSpecular ? reflectDir : diffuseDir;
                            specThroughput *= rAlbedo;
                            specOrigin = rSafeOrigin;

                            if (rBounce > 3) {
                                float maxT = max(max(specThroughput.r, specThroughput.g), specThroughput.b);
                                if (rnd() > maxT) break;
                                specThroughput /= maxT;
                            }
                        }

                        // Accumulate specular reflection weighted by Fresnel coefficient
                        glassReflection = throughput * savedF * specRadiance;

                        // Restore main path variables
                        surfaceHitPos = savedHitPos;
                        surfaceNormal = savedNormal;
                        refractDir = savedRefractDir;
                        F = savedF;
                        surfaceAlbedo = savedAlbedo;
                        payload.ior = savedIor;
                        payload.absorption = savedAbsorption;
                        payload.frontFacing = savedFrontFacing;
                        payload.hitDistance = 0.001; // Fake a small distance to keep absorption state valid
                    }

                    // Refract through the surface
                    rayDir = refractDir;
                    rayOrigin = surfaceHitPos - surfaceNormal * 0.001;

                    if (payload.frontFacing == 1.0) {
                        currentIor = payload.ior;
                        currentAbsorption = payload.absorption;
                        currentAlbedo = surfaceAlbedo;
                    } else {
                        currentIor = 1.0;
                        currentAbsorption = 0.0;
                        currentAlbedo = float3(1.0, 1.0, 1.0);
                    }

                    throughput *= (bounce == 0) ? ((1.0 - F) * surfaceAlbedo) : surfaceAlbedo;
                    continue;
                } else {
                    // Reflect specularly (TIR or Fresnel reflection on secondary bounces)
                    update_seed(surfaceHitPos);
                    rayDir = normalize(reflect(rayDir, surfaceNormal) + random_in_unit_sphere() * surfaceRoughness);
                    rayOrigin = surfaceHitPos + surfaceNormal * 0.001;
                    continue;
                }
            }

            float3 safeOrigin = surfaceHitPos + surfaceNormal * 0.001;

            // Direct Lighting (Next Event Estimation)
            float3 sunDir = normalize(cam.SunDirection.xyz);
            float sunRadius = 0.05; // Create soft shadows with random sun perturbation
            update_seed(surfaceHitPos);
            float3 sunJitter = random_in_unit_sphere() * sunRadius;
            float3 lightDir = normalize(sunDir + sunJitter);

            float nDotL = max(dot(surfaceNormal, lightDir), 0.0);
            if (nDotL > 0.0) {
                Payload shadowPayload;
                shadowPayload.hitDistance = 1.0; 
                uint shadowFlags = RAY_FLAG_ACCEPT_FIRST_HIT_AND_END_SEARCH | RAY_FLAG_SKIP_CLOSEST_HIT_SHADER;
                
                RayDesc shadowRay;
                shadowRay.Origin = safeOrigin;
                shadowRay.TMin = 0.0;
                shadowRay.Direction = lightDir;
                shadowRay.TMax = 10000.0;

                TraceRay(Scene, shadowFlags, cullMask, 0, 0, 0, shadowRay, shadowPayload);

                if (shadowPayload.hitDistance < 0.0) {
                    float3 sunLightColor = pow(float3(1.0, 0.95, 0.8), 2.2) * 5.0; 
                    float3 diffuse = surfaceAlbedo / 3.14159265;
                    radiance += throughput * diffuse * sunLightColor * nDotL;
                }
            }

            update_seed(surfaceHitPos);
            float3 diffuseDir = random_cosine_hemisphere(surfaceNormal);
            float3 reflectDir = normalize(reflect(rayDir, surfaceNormal) + random_in_unit_sphere() * surfaceRoughness);
            
            bool isSpecular = rnd() < surfaceMetallic;
            if (s == 0 && bounce == 0) {
                primaryIsSpecular = isSpecular;
            }
            float3 nextDir = isSpecular ? reflectDir : diffuseDir;

            throughput *= surfaceAlbedo;
            
            rayOrigin = safeOrigin;
            rayDir = nextDir;
            
            if (bounce > 4) {
                float maxThroughput = max(max(throughput.r, throughput.g), throughput.b);
                if (rnd() > maxThroughput) break;
                throughput /= maxThroughput;
            }
        }
        
        if (any(isnan(radiance))) radiance = float3(0.0, 0.0, 0.0);
        finalColorBeforeTransparency += radiance;
        finalColor += radiance + glassReflection;
    }
    
    finalColor /= float(cam.SamplesPerPixel);
    finalColorBeforeTransparency /= float(cam.SamplesPerPixel);

    NoisyColorTarget[launchIndex] = float4(finalColor, 1.0);
    NormalTarget[launchIndex] = float4(primaryNormal, 1.0);
    RoughnessTarget[launchIndex] = primaryRoughness;
    AlbedoTarget[launchIndex] = float4(primaryAlbedo, 1.0);
    SpecularAlbedoTarget[launchIndex] = float4(primarySpecularAlbedo, 1.0);
    MotionVectorsTarget[launchIndex] = primaryMotionVector;
    DepthTarget[launchIndex] = primaryDepth;
    LinearDepthTarget[launchIndex] = primaryLinearDepth;
    ColorBeforeTransparencyTarget[launchIndex] = float4(finalColorBeforeTransparency, 1.0);
    SpecularMotionVectorsTarget[launchIndex] = specularMotionVector;
    SpecularHitDistanceTarget[launchIndex] = specularHitDistance;

    RenderTarget[launchIndex] = float4(finalColor, 1.0);
}
