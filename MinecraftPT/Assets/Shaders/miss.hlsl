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

[shader("miss")]
void main(inout Payload payload) {
    payload.hitDistance = -1.0;
    payload.emission = pow(float3(0.4, 0.6, 0.9), 2.2); // Sky color
    payload.frontFacing = 1.0;
}
