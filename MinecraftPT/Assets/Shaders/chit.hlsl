#include "common.hlsli"

RaytracingAccelerationStructure Scene : register(t0, space0);
ConstantBuffer<Camera> cam : register(b2, space0);

[[vk::combinedImageSampler]]
[[vk::binding(3, 0)]]
Texture2DArray TexArray;

[[vk::combinedImageSampler]]
[[vk::binding(3, 0)]]
SamplerState TexArray_sampler;

StructuredBuffer<InstanceData> instances : register(t4, space0);
StructuredBuffer<MaterialData> materials : register(t5, space0);

[shader("closesthit")]
void main(inout Payload payload, BuiltInTriangleIntersectionAttributes attribs) {
    uint instId = InstanceID();
    uint primId = PrimitiveIndex();
    InstanceData inst = instances[instId];

    uint geomIdx = GeometryIndex();
    uint baseIndex = inst.IndexOffset + ((geomIdx > 0) ? inst.OpaqueIndexCount : 0) + (primId * 3);

    vk::BufferPointer<Vertices> verts = vk::BufferPointer<Vertices>(inst.VertexAddress);
    vk::BufferPointer<Indices> inds = vk::BufferPointer<Indices>(inst.IndexAddress);

    uint i0 = uint(inds.Get().i[baseIndex + 0]);
    uint i1 = uint(inds.Get().i[baseIndex + 1]);
    uint i2 = uint(inds.Get().i[baseIndex + 2]);

    ChunkVertex v0 = verts.Get().v[inst.VertexOffset + i0];
    ChunkVertex v1 = verts.Get().v[inst.VertexOffset + i1];
    ChunkVertex v2 = verts.Get().v[inst.VertexOffset + i2];

    float3 p0 = float3(v0.x, v0.y, v0.z);
    float3 p1 = float3(v1.x, v1.y, v1.z);
    float3 p2 = float3(v2.x, v2.y, v2.z);

    float3 e1 = p1 - p0;
    float3 e2 = p2 - p0;
    float3 geomNormal = normalize(cross(e1, e2));
    float3 normal = geomNormal;

    uint pd = v0.packedData;
    int texIndex = int(pd & 0xFFF);
    int overlayTexIndex = int((pd >> 12) & 0xFFF);
    if (overlayTexIndex == 0xFFF) overlayTexIndex = -1;
    uint tintType = (pd >> 26) & 0x7;

    float2 uvs[4] = { float2(0,0), float2(0,1), float2(1,1), float2(1,0) };
    float2 uv0 = uvs[(v0.packedData >> 24) & 0x3];
    float2 uv1 = uvs[(v1.packedData >> 24) & 0x3];
    float2 uv2 = uvs[(v2.packedData >> 24) & 0x3];
    
    float3 barycentrics = float3(1.0 - attribs.barycentrics.x - attribs.barycentrics.y, attribs.barycentrics.x, attribs.barycentrics.y);
    float2 uv = uv0 * barycentrics.x + uv1 * barycentrics.y + uv2 * barycentrics.z;

    float4 baseTint = float4(1.0, 1.0, 1.0, 1.0);
    float4 overTint = float4(1.0, 1.0, 1.0, 1.0);
    if (tintType == 1) baseTint = float4(145.0/255.0, 189.0/255.0, 89.0/255.0, 1.0);
    else if (tintType == 2) overTint = float4(145.0/255.0, 189.0/255.0, 89.0/255.0, 1.0);
    else if (tintType == 3) baseTint = float4(72.0/255.0, 181.0/255.0, 72.0/255.0, 1.0);

    float4 texColor = TexArray.SampleLevel(TexArray_sampler, float3(uv, float(texIndex)), 0.0) * baseTint;

    if (overlayTexIndex >= 0) {
        float4 overlayTex = TexArray.SampleLevel(TexArray_sampler, float3(uv, float(overlayTexIndex)), 0.0);
        if (overlayTex.a > 0.5) texColor = overlayTex * overTint;
    }

    texColor.rgb = pow(texColor.rgb, 2.2);

    MaterialData mat = materials[texIndex];

    float3 rayDirInChit = WorldRayDirection();
    bool isExitHit = (dot(rayDirInChit, normal) > 0.0);
    float3 shadingNormal = isExitHit ? -normal : normal;

    payload.hitDistance = RayTCurrent();
    payload.normal = shadingNormal;
    payload.roughness = mat.Roughness;
    payload.metallic = mat.Metallic;
    payload.emission = texColor.rgb * mat.Emission;

    if (mat.Type == 2.0) {
        // Полупрозрачный материал (стекло, вода, лед):
        if (texColor.a > 0.85) {
            // Непрозрачная рамка / прожилки текстуры (как на внешней, так и на внутренней/обратной грани):
            // Полноправная непрозрачная поверхность с прямым (NEE) и непрямым (GI) освещением
            payload.opacity = 1.0;
            payload.albedo = texColor.rgb;
            payload.ior = 1.0;
            payload.absorption = 0.0;
        } else {
            // Прозрачное тело материала / выходная грань среды: физическая преломляющая среда
            float bodyAlpha = (texColor.a > 0.0) ? texColor.a : ((mat.Opacity > 0.0) ? mat.Opacity : 0.2);
            if (mat.Opacity > 0.0 && mat.Opacity < 1.0 && texColor.a > 0.0) {
                bodyAlpha *= mat.Opacity;
            }
            payload.opacity = max(bodyAlpha, 0.02);
            payload.albedo = (dot(texColor.rgb, texColor.rgb) > 0.001) ? texColor.rgb : float3(0.92, 0.96, 0.98);
            payload.ior = mat.Ior;
            payload.absorption = mat.Absorption;
        }
    } else if (mat.Type == 1.0) {
        payload.opacity = texColor.a;
        payload.albedo = texColor.rgb;
        payload.ior = mat.Ior;
        payload.absorption = mat.Absorption;
    } else {
        payload.opacity = 1.0;
        payload.albedo = texColor.rgb;
        payload.ior = mat.Ior;
        payload.absorption = mat.Absorption;
    }
    
    float uvDet = (uv1.x - uv0.x) * (uv2.y - uv0.y) - (uv1.y - uv0.y) * (uv2.x - uv0.x);
    payload.frontFacing = (uvDet < 0.0) ? 1.0 : 0.0;
}
