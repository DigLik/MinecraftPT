using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

namespace MinecraftPT.Tests.Mathematics;

public class DuffOrthonormalBasisTests
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void BuildOrthonormalBasisDuff(Vector3 n, out Vector3 b1, out Vector3 b2)
    {
        float sign = (n.Z >= 0.0f) ? 1.0f : -1.0f;
        float a = -1.0f / (sign + n.Z);
        float b = n.X * n.Y * a;
        b1 = new Vector3(1.0f + sign * n.X * n.X * a, sign * b, -sign * n.X);
        b2 = new Vector3(b, sign + n.Y * n.Y * a, -n.Y);
    }

    [Fact]
    public void DuffONB_CanonicalAxes_AreOrthonormalAndRightHanded()
    {
        const float Epsilon = 1e-5f;
        Vector3[] canonicalNormals =
        [
            new Vector3(1, 0, 0),
            new Vector3(-1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, -1, 0),
            new Vector3(0, 0, 1),
            new Vector3(0, 0, -1)
        ];

        foreach (var n in canonicalNormals)
        {
            BuildOrthonormalBasisDuff(n, out var b1, out var b2);
            float dotB1B2 = MathF.Abs(Vector3.Dot(b1, b2));
            float dotB1N = MathF.Abs(Vector3.Dot(b1, n));
            float dotB2N = MathF.Abs(Vector3.Dot(b2, n));
            float lenB1 = MathF.Abs(b1.Length() - 1.0f);
            float lenB2 = MathF.Abs(b2.Length() - 1.0f);
            var cross = Vector3.Cross(b1, b2);
            float rightHandDiff = (cross - n).Length();

            Assert.True(dotB1B2 <= Epsilon);
            Assert.True(dotB1N <= Epsilon);
            Assert.True(dotB2N <= Epsilon);
            Assert.True(lenB1 <= Epsilon);
            Assert.True(lenB2 <= Epsilon);
            Assert.True(rightHandDiff <= Epsilon);
        }
    }

    [Fact]
    public void DuffONB_EdgeCasesAndPoles_Robust()
    {
        const float Epsilon = 1e-5f;
        Vector3[] edgeCases =
        [
            new Vector3(0, 0, 1.0f),
            new Vector3(0, 0, -1.0f),
            Vector3.Normalize(new Vector3(1e-7f, 1e-7f, 1.0f)),
            Vector3.Normalize(new Vector3(1e-7f, 1e-7f, -1.0f)),
            Vector3.Normalize(new Vector3(1.0f, 0.0f, 0.0f)),
            Vector3.Normalize(new Vector3(0.0f, 1.0f, 0.0f)),
            Vector3.Normalize(new Vector3(1.0f, 1.0f, 0.0f)),
            Vector3.Normalize(new Vector3(1.0f, -1.0f, 0.0f)),
            Vector3.Normalize(new Vector3(1e-6f, 1e-6f, 1e-6f)),
            Vector3.Normalize(new Vector3(-1e-6f, -1e-6f, -1e-6f))
        ];

        foreach (var n in edgeCases)
        {
            BuildOrthonormalBasisDuff(n, out var b1, out var b2);
            Assert.False(float.IsNaN(b1.X));
            Assert.False(float.IsNaN(b2.X));

            float dotB1B2 = MathF.Abs(Vector3.Dot(b1, b2));
            float dotB1N = MathF.Abs(Vector3.Dot(b1, n));
            float dotB2N = MathF.Abs(Vector3.Dot(b2, n));
            float lenB1 = MathF.Abs(b1.Length() - 1.0f);
            float lenB2 = MathF.Abs(b2.Length() - 1.0f);
            var cross = Vector3.Cross(b1, b2);
            float rightHandDiff = (cross - n).Length();

            Assert.True(dotB1B2 <= Epsilon);
            Assert.True(dotB1N <= Epsilon);
            Assert.True(dotB2N <= Epsilon);
            Assert.True(lenB1 <= Epsilon);
            Assert.True(lenB2 <= Epsilon);
            Assert.True(rightHandDiff <= Epsilon);
        }
    }

    [Fact]
    public void DuffONB_RandomSphericalVectors_StrictPrecision()
    {
        const float Epsilon = 1e-5f;
        const int Samples = 100_000;
        var rng = new Random(42);

        for (int i = 0; i < Samples; i++)
        {
            float z = (float)(rng.NextDouble() * 2.0 - 1.0);
            float phi = (float)(rng.NextDouble() * Math.PI * 2.0);
            float r = MathF.Sqrt(MathF.Max(0.0f, 1.0f - z * z));
            Vector3 n = new Vector3(r * MathF.Cos(phi), r * MathF.Sin(phi), z);

            BuildOrthonormalBasisDuff(n, out var b1, out var b2);

            Assert.False(float.IsNaN(b1.X) || float.IsNaN(b2.X) || float.IsInfinity(b1.X) || float.IsInfinity(b2.X));

            float dotB1B2 = MathF.Abs(Vector3.Dot(b1, b2));
            float dotB1N = MathF.Abs(Vector3.Dot(b1, n));
            float dotB2N = MathF.Abs(Vector3.Dot(b2, n));
            float lenErrB1 = MathF.Abs(b1.Length() - 1.0f);
            float lenErrB2 = MathF.Abs(b2.Length() - 1.0f);
            var cross = Vector3.Cross(b1, b2);
            float crossDiff = (cross - n).Length();

            Assert.True(dotB1B2 < Epsilon);
            Assert.True(dotB1N < Epsilon);
            Assert.True(dotB2N < Epsilon);
            Assert.True(lenErrB1 < Epsilon);
            Assert.True(lenErrB2 < Epsilon);
            Assert.True(crossDiff < Epsilon);
        }
    }
}
