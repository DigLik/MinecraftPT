#ifndef SAMPLING_HLSLI
#define SAMPLING_HLSLI

// 32-битный целочисленный хэш (Thomas Wang / Bob Jenkins)
uint hash(uint x) {
    x = ((x >> 16) ^ x) * 0x45d9f3bu;
    x = ((x >> 16) ^ x) * 0x45d9f3bu;
    x = (x >> 16) ^ x;
    return x;
}

// Быстрый псевдослучайный генератор (PCG/Xorshift)
float rnd(inout uint seed) {
    seed = seed * 747796405u + 2891336453u;
    uint word = ((seed >> ((seed >> 28u) + 4u)) ^ seed) * 277803737u;
    return float((word >> 22u) ^ word) / 4294967296.0;
}

// Равномерная выборка единичного 2D диска
float2 SampleUnitDisk(float2 u) {
    float r = sqrt(u.x);
    float theta = u.y * 6.28318530718;
    return float2(r * cos(theta), r * sin(theta));
}

// Равномерная выборка поверхности единичной 3D сферы
float3 SampleUnitSphere(float2 u) {
    float z = u.x * 2.0 - 1.0;
    float phi = u.y * 6.28318530718;
    float r = sqrt(max(0.0, 1.0 - z * z));
    return float3(r * cos(phi), r * sin(phi), z);
}

// Построение безветвевого ортонормированного базиса (Duff et al. 2017)
void BuildOrthonormalBasis(float3 n, out float3 b1, out float3 b2) {
    float sign = (n.z >= 0.0f) ? 1.0f : -1.0f;
    float a = -1.0f / (sign + n.z);
    float b = n.x * n.y * a;
    b1 = float3(1.0f + sign * n.x * n.x * a, sign * b, -sign * n.x);
    b2 = float3(b, sign + n.y * n.y * a, -n.y);
}

// Выборка направления на диск солнца с использованием ортонормированного базиса
float3 SampleSunDirection(float3 sunDir, float2 u, float sunRadius) {
    float3 b1, b2;
    BuildOrthonormalBasis(sunDir, b1, b2);
    float2 disk = SampleUnitDisk(u) * sunRadius;
    return normalize(sunDir + b1 * disk.x + b2 * disk.y);
}

// Косинусоидально-взвешенная выборка полусферы
float3 SampleCosineHemisphere(float3 n, float2 u) {
    float phi = u.x * 6.28318530718;
    float r = sqrt(u.y);
    float x = r * cos(phi);
    float y = r * sin(phi);
    float z = sqrt(max(0.0, 1.0 - u.y));
    
    float3 u_vec, v_vec;
    BuildOrthonormalBasis(n, u_vec, v_vec);
    return x * u_vec + y * v_vec + z * n;
}

// PMJ02BN низкодисперсионная выборка 2D сэмплов со стратифицированным синим шумом
// dimension: 0 = Diffuse BRDF (.xy слоя 0..63), 1 = Specular BRDF (.zw слоя 0..63),
//            2 = Sun Light (.xy слоя 64..127), 3 = Secondary / Material (.zw слоя 64..127)
float2 SamplePmj02bn2D(Texture2DArray<float4> tex, uint2 pixel, uint sampleIndex, uint dimension, int bounce)
{
    uint sampleSlot = (sampleIndex + uint(bounce) * 17u + (dimension / 4u) * 23u) % 64u;
    uint layerBank = (dimension >> 1) & 1u;
    uint layer = layerBank * 64u + sampleSlot;
    float4 rawSample = tex.Load(int4(pixel.x % 64, pixel.y % 64, layer, 0));
    float2 u = ((dimension & 1u) == 0u) ? rawSample.xy : rawSample.zw;

    // Вращение Крэнли-Паттерсона применяется только при переходе на новый цикл из 64 кадров,
    // сохраняя идеальную временную стратификацию для DLSS-RR внутри каждого 64-кадрового окна
    uint cycle = sampleIndex / 64u;
    if (cycle > 0u) {
        uint t1 = cycle * 0xC140A7A0u; // 0.75487766624 * 2^32 (R2 alpha 1)
        uint t2 = cycle * 0x91E10DA6u; // 0.56984029099 * 2^32 (R2 alpha 2)
        float2 cycleOffset = float2(t1 >> 8, t2 >> 8) / 16777216.0;
        u = frac(u + cycleOffset);
    }
    return u;
}

// =================================================================================================
// Аналитическая аппроксимация Split-Sum для зеркального EnvBRDF (GGX Smith)
// Референс: Brian Karis (2013), "Real Shading in Unreal Engine 4" / Dimitar Lazarov (2013)
// Гарантирует строгое сохранение энергии, отсутствие делений на ноль и exact 1.0 при NoV=1, rough=0.
// =================================================================================================
float2 EnvBRDFApproxLazarov(float Roughness, float NoV)
{
    const float4 c0 = float4(-1.0, -0.0275, -0.572, 0.022);
    const float4 c1 = float4( 1.0,  0.0425,  1.040, -0.040);
    float4 r = Roughness * c0 + c1;
    float a004 = min(r.x * r.x, exp2(-9.28 * NoV)) * r.x + r.y;
    float2 AB = float2(-1.04, 1.04) * a004 + r.zw;
    return saturate(AB);
}

float3 EnvBRDFApprox(float3 SpecularColor, float Roughness, float NoV)
{
    float2 AB = EnvBRDFApproxLazarov(Roughness, NoV);
    return SpecularColor * AB.x + AB.y;
}

// Скорректированная полиномиальная аппроксимация (совместимость с сигнатурой RTG Ch. 32 / NVIDIA NRD)
// Принимает линейную шероховатость roughness = sqrt(alpha) и устраняет потерю 42% энергии при NoV=1
float3 EnvBRDFApprox2(float3 SpecularColor, float alpha, float NoV)
{
    float roughness = sqrt(saturate(alpha));
    return EnvBRDFApprox(SpecularColor, roughness, NoV);
}

// =================================================================================================
// Выборка видимых нормалей микрограней GGX VNDF (Eric Heitz 2018, JCGT)
// Обеспечивает физически корректное распределение зеркальных лучей, сохранение энергии и
// устранение мерцания/кипения отражений.
// =================================================================================================
float3 SampleGGXVNDF(float3 V, float3 N, float roughness, float2 u)
{
    float alpha = max(roughness * roughness, 0.001);

    float3 T, B;
    BuildOrthonormalBasis(N, T, B);

    // Преобразование вектора наблюдения V (-rayDir) в локальное пространство
    float3 V_local = float3(dot(V, T), dot(V, B), dot(V, N));
    if (V_local.z <= 0.0) {
        V_local.z = 0.001;
    }

    // Растягивание полусферы по параметру шероховатости alpha
    float3 Vh = normalize(float3(alpha * V_local.x, alpha * V_local.y, V_local.z));

    // Ортонормированный базис вокруг Vh
    float lensq = Vh.x * Vh.x + Vh.y * Vh.y;
    float3 T1 = (lensq > 1e-7) ? float3(-Vh.y, Vh.x, 0.0) * rsqrt(lensq) : float3(1.0, 0.0, 0.0);
    float3 T2 = cross(Vh, T1);

    // Параметризация единичного диска
    float r = sqrt(u.x);
    float phi = 6.28318530718 * u.y;
    float t1 = r * cos(phi);
    float t2 = r * sin(phi);
    float s = 0.5 * (1.0 + Vh.z);
    t2 = (1.0 - s) * sqrt(max(0.0, 1.0 - t1 * t1)) + s * t2;

    // Репроекция на верхнюю полусферу
    float3 Nh = t1 * T1 + t2 * T2 + sqrt(max(0.0, 1.0 - t1 * t1 - t2 * t2)) * Vh;

    // Сжатие обратно в пространство эллипсоида (нормаль микрограни H в мировых координатах)
    float3 H = normalize(T * (alpha * Nh.x) + B * (alpha * Nh.y) + N * max(0.0, Nh.z));
    return H;
}

// Оценка веса пути для выборки GGX VNDF: W = F * G2 / G1
float3 EvalGGXVNDFWeight(float3 V, float3 L, float3 H, float3 N, float roughness, float3 f0)
{
    float alpha = max(roughness * roughness, 0.001);
    float alpha2 = alpha * alpha;

    float NdotV = max(dot(N, V), 0.001);
    float NdotL = max(dot(N, L), 0.001);
    float VdotH = saturate(dot(V, H));

    // Аналитическая функция затенения Смита (height-correlated Smith G2 / G1)
    // G2 / G1 = (NdotL * (Av + NdotV)) / (NdotL * Av + NdotV * Al)
    float Av = sqrt(alpha2 + (1.0 - alpha2) * NdotV * NdotV);
    float Al = sqrt(alpha2 + (1.0 - alpha2) * NdotL * NdotL);
    float num = NdotL * (Av + NdotV);
    float den = NdotL * Av + NdotV * Al;
    float G2OverG1 = saturate(num / max(den, 1e-5));

    // Френель Шлика для металлов и диэлектриков
    float3 F = lerp(f0, float3(1.0, 1.0, 1.0), pow(1.0 - VdotH, 5.0));

    return F * G2OverG1;
}

#endif // SAMPLING_HLSLI
