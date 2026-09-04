#include "common.hlsli"

[shader("miss")]
void main(inout Payload payload) {
    payload.hitDistance = -1.0;
    payload.emission = pow(float3(0.4, 0.6, 0.9), 2.2); // Sky color
    payload.frontFacing = 1.0;
}
