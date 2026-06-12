#ifndef __HLSL_VERSION
typedef unsigned short uint16_t;
typedef unsigned long long uint64_t;
#endif

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

struct ChunkVertex {
    float x;
    float y;
    float z;
    uint packedData;
};

struct Vertices {
    ChunkVertex v[16777216];
};

struct Indices {
    uint16_t i[33554432];
};

struct InstanceData {
    uint VertexOffset;
    uint IndexOffset;
    uint OpaqueIndexCount;
    uint Pad2;
    uint64_t VertexAddress;
    uint64_t IndexAddress;
};

struct MaterialData {
    float Roughness;
    float Metallic;
    float Emission;
    float Opacity;
    float Type;
    float Ior;
    float Absorption;
    float Pad;
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
    bool isFrontFace = dot(geomNormal, WorldRayDirection()) < 0.0;
    float3 normal = isFrontFace ? geomNormal : -geomNormal;

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

    payload.hitPos = WorldRayOrigin() + WorldRayDirection() * RayTCurrent();
    payload.hitDistance = RayTCurrent();
    payload.normal = normal;
    payload.roughness = mat.Roughness;
    payload.albedo = (mat.Type == 2.0) ? lerp(float3(1.0, 1.0, 1.0), texColor.rgb, texColor.a) : texColor.rgb;
    payload.metallic = mat.Metallic;
    payload.emission = texColor.rgb * mat.Emission;
    payload.opacity = (mat.Type == 2.0) ? lerp(mat.Opacity, 1.0, texColor.a) : 1.0;
    payload.ior = mat.Ior;
    payload.absorption = mat.Absorption;
    payload.frontFacing = isFrontFace ? 1.0 : 0.0;
}
