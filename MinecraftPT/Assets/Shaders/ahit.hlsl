#include "common.hlsli"

ConstantBuffer<Camera> cam : register(b2, space0);

[[vk::combinedImageSampler]]
[[vk::binding(3, 0)]]
Texture2DArray texSampler;

[[vk::combinedImageSampler]]
[[vk::binding(3, 0)]]
SamplerState texSampler_sampler;

StructuredBuffer<InstanceData> instances : register(t4, space0);
StructuredBuffer<MaterialData> materials : register(t5, space0);

[shader("anyhit")]
void main(inout Payload payload, BuiltInTriangleIntersectionAttributes attribs) {
    uint instanceID = InstanceID();
    uint geometryIndex = GeometryIndex();
    uint primitiveID = PrimitiveIndex();
    float3 worldRayDirection = WorldRayDirection();
    float3 worldRayOrigin = WorldRayOrigin();
    uint incomingRayFlags = RayFlags();
    float hitT = RayTCurrent();
    uint2 launchIndex = DispatchRaysIndex().xy;
    uint2 launchSize = DispatchRaysDimensions().xy;

    InstanceData inst = instances[instanceID];
    
    vk::BufferPointer<Vertices> verts = vk::BufferPointer<Vertices>(inst.VertexAddress);
    vk::BufferPointer<Indices> inds = vk::BufferPointer<Indices>(inst.IndexAddress);
    
    uint baseIndex = inst.IndexOffset + ((geometryIndex > 0) ? inst.OpaqueIndexCount : 0) + (primitiveID * 3);
    
    uint i0 = uint(inds.Get().i[baseIndex + 0]);
    uint i1 = uint(inds.Get().i[baseIndex + 1]);
    uint i2 = uint(inds.Get().i[baseIndex + 2]);

    ChunkVertex v0 = verts.Get().v[inst.VertexOffset + i0];
    ChunkVertex v1 = verts.Get().v[inst.VertexOffset + i1];
    ChunkVertex v2 = verts.Get().v[inst.VertexOffset + i2];

    uint packed0 = v0.packedData;
    int texIndex = int(packed0 & 0xFFF);

    float2 uvs[4] = { float2(0,0), float2(0,1), float2(1,1), float2(1,0) };
    float2 uv0 = uvs[(packed0 >> 24) & 0x3];
    float2 uv1 = uvs[(v1.packedData >> 24) & 0x3];
    float2 uv2 = uvs[(v2.packedData >> 24) & 0x3];

    float3 barycentrics = float3(1.0 - attribs.barycentrics.x - attribs.barycentrics.y, attribs.barycentrics.x, attribs.barycentrics.y);
    float2 uv = uv0 * barycentrics.x + uv1 * barycentrics.y + uv2 * barycentrics.z;

    float4 texColor = texSampler.SampleLevel(texSampler_sampler, float3(uv, float(texIndex)), 0.0);
    MaterialData mat = materials[texIndex];

    if (mat.Type == 1.0) 
    {
        uint s = cam.Seed + launchIndex.y * launchSize.x + launchIndex.x;
        s = hash(s);
        if (texColor.a * mat.Opacity < rnd(s)) 
            IgnoreHit();
    }
    else if (mat.Type == 2.0)
    {
        if ((incomingRayFlags & RAY_FLAG_SKIP_CLOSEST_HIT_SHADER) != 0U)
        {
            // Прямой солнечный свет: прозрачная часть стекла пропускает 100% света без шумного дизеринга
            if (texColor.a <= 0.85)
            {
                IgnoreHit();
            }
        }
    }
}
