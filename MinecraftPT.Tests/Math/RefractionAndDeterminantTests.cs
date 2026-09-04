using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

namespace MinecraftPT.Tests.Mathematics;

public class RefractionAndDeterminantTests
{
    private static readonly Vector3[][] MesherFaceVertices = [
        [new(0, 1, 1), new(0, 0, 1), new(1, 0, 1), new(1, 1, 1)], // Top (Z+)
        [new(1, 1, 0), new(1, 0, 0), new(0, 0, 0), new(0, 1, 0)], // Bottom (Z-)
        [new(0, 1, 1), new(0, 1, 0), new(0, 0, 0), new(0, 0, 1)], // Side X-
        [new(1, 0, 1), new(1, 0, 0), new(1, 1, 0), new(1, 1, 1)], // Side X+
        [new(1, 1, 1), new(1, 1, 0), new(0, 1, 0), new(0, 1, 1)], // Side Y+
        [new(0, 0, 1), new(0, 0, 0), new(1, 0, 0), new(1, 0, 1)]  // Side Y-
    ];

    private static readonly Vector3[] ExpectedFaceOutwardNormals = [
        new(0, 0, 1),
        new(0, 0, -1),
        new(-1, 0, 0),
        new(1, 0, 0),
        new(0, 1, 0),
        new(0, -1, 0)
    ];

    private static readonly Vector2[] FaceUVTable = [
        new(0, 0), new(0, 1), new(1, 1), new(1, 0)
    ];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float ComputeUvDeterminant(Vector2 uv0, Vector2 uv1, Vector2 uv2)
    {
        return (uv1.X - uv0.X) * (uv2.Y - uv0.Y) - (uv1.Y - uv0.Y) * (uv2.X - uv0.X);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Vector3 RefractGlsl(Vector3 incident, Vector3 normal, float eta)
    {
        float dotNI = Vector3.Dot(normal, incident);
        float k = 1.0f - eta * eta * (1.0f - dotNI * dotNI);
        if (k < 0.0f)
            return Vector3.Zero; // Total internal reflection
        return eta * incident - (eta * dotNI + MathF.Sqrt(k)) * normal;
    }

    [Fact]
    public void Refraction_UvDeterminantAndNormals_PreservesWindingAcrossCubeFaces()
    {
        for (int faceIdx = 0; faceIdx < 6; faceIdx++)
        {
            var fVerts = MesherFaceVertices[faceIdx];
            var expectedOutward = ExpectedFaceOutwardNormals[faceIdx];

            Vector3 p0 = fVerts[0], p1 = fVerts[1], p2 = fVerts[2], p3 = fVerts[3];
            Vector2 uv0 = FaceUVTable[0], uv1 = FaceUVTable[1], uv2 = FaceUVTable[2], uv3 = FaceUVTable[3];

            // Front side
            float uvDetFront0 = ComputeUvDeterminant(uv0, uv1, uv2);
            float frontFacing0 = (uvDetFront0 < 0.0f) ? 1.0f : 0.0f;
            Vector3 geomNormalFront0 = Vector3.Normalize(Vector3.Cross(p1 - p0, p2 - p0));

            float uvDetFront1 = ComputeUvDeterminant(uv2, uv3, uv0);
            float frontFacing1 = (uvDetFront1 < 0.0f) ? 1.0f : 0.0f;
            Vector3 geomNormalFront1 = Vector3.Normalize(Vector3.Cross(p3 - p2, p0 - p2));

            Assert.Equal(1.0f, frontFacing0);
            Assert.Equal(1.0f, frontFacing1);
            Assert.True((geomNormalFront0 - expectedOutward).Length() <= 1e-4f);
            Assert.True((geomNormalFront1 - expectedOutward).Length() <= 1e-4f);

            // Back side (doubleSided)
            float uvDetBack0 = ComputeUvDeterminant(uv0, uv3, uv2);
            float frontFacingBack0 = (uvDetBack0 < 0.0f) ? 1.0f : 0.0f;
            Vector3 geomNormalBack0 = Vector3.Normalize(Vector3.Cross(p3 - p0, p2 - p0));

            float uvDetBack1 = ComputeUvDeterminant(uv2, uv1, uv0);
            float frontFacingBack1 = (uvDetBack1 < 0.0f) ? 1.0f : 0.0f;
            Vector3 geomNormalBack1 = Vector3.Normalize(Vector3.Cross(p1 - p2, p0 - p2));

            Assert.Equal(0.0f, frontFacingBack0);
            Assert.Equal(0.0f, frontFacingBack1);
            Assert.True((geomNormalBack0 - (-expectedOutward)).Length() <= 1e-4f);
            Assert.True((geomNormalBack1 - (-expectedOutward)).Length() <= 1e-4f);
        }
    }

    [Fact]
    public void SnellLaw_WaterRefractionAndTIR_MatchesPhysics()
    {
        float waterIor = 1.333f;
        Vector3 incidentDir = Vector3.Normalize(new Vector3(0.5f, 0.0f, -1.0f));
        Vector3 frontNormal = new Vector3(0, 0, 1);

        float etaEntry = 1.0f / waterIor;
        Vector3 refractDirInside = RefractGlsl(incidentDir, frontNormal, etaEntry);
        Assert.NotEqual(Vector3.Zero, refractDirInside);

        float cosAir = MathF.Abs(Vector3.Dot(incidentDir, frontNormal));
        float sinAir = MathF.Sqrt(1.0f - cosAir * cosAir);
        float cosWater = MathF.Abs(Vector3.Dot(refractDirInside, frontNormal));
        float sinWater = MathF.Sqrt(1.0f - cosWater * cosWater);
        float snellDiffEntry = MathF.Abs(sinAir * 1.0f - sinWater * waterIor);
        Assert.True(snellDiffEntry < 1e-4f);

        // TIR at 60 degrees (> critical angle ~48.6 deg)
        Vector3 tirIncident = new Vector3(MathF.Sin(60.0f * MathF.PI / 180.0f), 0.0f, -MathF.Cos(60.0f * MathF.PI / 180.0f));
        Vector3 tirRefract = RefractGlsl(tirIncident, new Vector3(0, 0, 1), waterIor);
        Assert.Equal(Vector3.Zero, tirRefract);
    }
}
