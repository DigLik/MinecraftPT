#ifndef STRUCTS_HLSLI
#define STRUCTS_HLSLI

#ifndef __HLSL_VERSION
typedef unsigned short uint16_t;
typedef unsigned long long uint64_t;
#endif

// Структура полезной нагрузки луча (RayPayload) для всех стадий DXR (64 байта)
struct Payload {
    float hitDistance;      // 0..3B (4B, < 0 при промахе)
    float3 normal;          // 4..15B (12B, мировая геометрическая нормаль)
    float roughness;        // 16..19B (4B)
    float3 albedo;          // 20..31B (12B, базовое альбедо поверхности)
    float metallic;         // 32..35B (4B)
    float3 emission;        // 36..47B (12B, цвет излучения / цвет неба)
    float opacity;          // 48..51B (4B, непрозрачность 0..1)
    float ior;              // 52..55B (4B, показатель преломления)
    float absorption;       // 56..59B (4B, коэффициент поглощения Бера-Ламберта)
    float frontFacing;      // 60..63B (4B, 1.0 = вход в среду, 0.0 = выход из среды)
};

// Вершина воксельной геометрии чанка (16 байт)
struct ChunkVertex {
    float x;
    float y;
    float z;
    uint packedData;
};

// Буферы адресации устройств (Vulkan Buffer Device Address)
struct Vertices {
    ChunkVertex v[16777216];
};

struct Indices {
    uint16_t i[33554432];
};

// Данные экземпляра чанка (32 байта, std430)
struct InstanceData {
    uint VertexOffset;      // 4B
    uint IndexOffset;       // 4B
    uint OpaqueIndexCount;  // 4B
    uint Pad2;              // 4B
    uint64_t VertexAddress; // 8B (BDA)
    uint64_t IndexAddress;  // 8B (BDA)
};

// Данные воксельного материала (32 байта, std430)
struct MaterialData {
    float Roughness;        // 4B
    float Metallic;         // 4B
    float Emission;         // 4B
    float Opacity;          // 4B
    float Type;             // 4B (0: Opaque, 1: Cutout Alpha, 2: Translucent Glass/Water)
    float Ior;              // 4B
    float Absorption;       // 4B
    float Pad;              // 4B
};

// Параметры камеры и глобального кадра (288 байт, std140)
struct Camera {
    column_major float4x4 ViewProj;        // 0..63B
    column_major float4x4 InverseViewProj; // 64..127B
    column_major float4x4 PrevViewProj;    // 128..191B
    int3 ChunkPosition;                    // 192..203B
    uint FrameCount;                       // 204..207B
    float3 LocalPosition;                  // 208..219B
    uint SamplesPerPixel;                  // 220..223B
    float4 SunDirection;                   // 224..239B
    float3 CameraUp;                       // 240..251B
    uint Seed;                             // 252..255B
    float3 CameraRight;                    // 256..267B
    float JitterX;                         // 268..271B
    float3 CameraFwd;                      // 272..283B
    float JitterY;                         // 284..287B
};

#endif // STRUCTS_HLSLI
