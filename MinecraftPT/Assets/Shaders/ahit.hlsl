#ifndef __HLSL_VERSION
typedef unsigned short uint16_t;
typedef unsigned long long uint64_t;
#endif

struct ChunkVertex {
    float x;
    float y;
    float z;
    uint packedData;
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

struct Vertices {
    ChunkVertex v[16777216];
};

struct Indices {
    uint16_t i[33554432];
};

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

ConstantBuffer<Camera> cam : register(b2, space0);

[[vk::combinedImageSampler]]
[[vk::binding(3, 0)]]
Texture2DArray texSampler;

[[vk::combinedImageSampler]]
[[vk::binding(3, 0)]]
SamplerState texSampler_sampler;

StructuredBuffer<InstanceData> instances : register(t4, space0);
StructuredBuffer<MaterialData> materials : register(t5, space0);

uint hash(uint x) {
    x = ((x >> 16) ^ x) * 0x45d9f3b;
    x = ((x >> 16) ^ x) * 0x45d9f3b;
    x = (x >> 16) ^ x;
    return x;
}

float rnd(inout uint seed) {
    seed = seed * 747796405u + 2891336453u;
    uint word = ((seed >> ((seed >> 28u) + 4u)) ^ seed) * 277803737u;
    return float((word >> 22u) ^ word) / 4294967296.0;
}

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

    float3 p0 = float3(v0.x, v0.y, v0.z);
    float3 p1 = float3(v1.x, v1.y, v1.z);
    float3 p2 = float3(v2.x, v2.y, v2.z);
    float3 geomNormal = cross(p1 - p0, p2 - p0);
    bool isBackFace = dot(geomNormal, worldRayDirection) > 0.0;

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

    if (mat.Type != 2.0 && isBackFace)
    {
        IgnoreHit();
    }

    if (mat.Type == 1.0) 
    {
        uint s = cam.Seed + launchIndex.y * launchSize.x + launchIndex.x;
        s = hash(s);
        if (texColor.a * mat.Opacity < rnd(s)) 
            IgnoreHit();
    }
    else if (mat.Type == 2.0)
    {
        // 8U corresponds to gl_RayFlagsSkipClosestHitShaderEXT (shadow ray flag)
        if ((incomingRayFlags & 8U) != 0U)
        {
            float opacity = lerp(mat.Opacity, 1.0, texColor.a);
            // Calculate world-space intersection coordinate
            float3 worldPos = worldRayOrigin + worldRayDirection * hitT;
            // Get voxel block coordinates
            float3 blockPos = floor(worldPos + worldRayDirection * 0.1);
            // Spatial dither pattern mixed with large prime hashing of block coordinates (decorrelates parallel layers)
            float3 fracPos = frac(worldPos * 128.0);
            uint s = uint(fracPos.x * 1000.0) ^ uint(fracPos.y * 1000.0) ^ uint(fracPos.z * 1000.0);
            s = s ^ uint(abs(blockPos.x) * 73856093u) ^ uint(abs(blockPos.y) * 19349663u) ^ uint(abs(blockPos.z) * 83492791u);
            s = hash(s);
            if (rnd(s) > opacity)
                IgnoreHit();
        }
    }
}
