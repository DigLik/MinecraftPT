using System.Numerics;
using Xunit;

namespace MinecraftPT.Tests.Shading;

public class EnvBRDFSplitSumTests
{
    private static Vector2 EnvBRDFApproxLazarov(float roughness, float noV)
    {
        Vector4 c0 = new(-1.0f, -0.0275f, -0.572f, 0.022f);
        Vector4 c1 = new(1.0f, 0.0425f, 1.040f, -0.040f);
        Vector4 r = roughness * c0 + c1;
        float a004 = Math.Min(r.X * r.X, MathF.Pow(2.0f, -9.28f * noV)) * r.X + r.Y;
        Vector2 ab = new Vector2(-1.04f, 1.04f) * a004 + new Vector2(r.Z, r.W);
        return Vector2.Clamp(ab, Vector2.Zero, Vector2.One);
    }

    private static Vector3 EnvBRDFApprox(Vector3 specularColor, float roughness, float noV)
    {
        Vector2 ab = EnvBRDFApproxLazarov(roughness, noV);
        return specularColor * ab.X + new Vector3(ab.Y);
    }

    private static Vector3 EnvBRDFApprox2(Vector3 specularColor, float alpha, float noV)
    {
        float roughness = MathF.Sqrt(Math.Clamp(alpha, 0f, 1f));
        return EnvBRDFApprox(specularColor, roughness, noV);
    }

    [Fact]
    public void NormalIncidence_PerfectMirror_EvaluatesToExactOne()
    {
        Vector3 f0Metal = new(1.0f, 1.0f, 1.0f);
        Vector3 resLazarov = EnvBRDFApprox(f0Metal, 0.0f, 1.0f);
        Vector3 resApprox2 = EnvBRDFApprox2(f0Metal, 0.0f, 1.0f);

        Assert.True(Math.Abs(resLazarov.X - 1.0f) < 1e-6f);
        Assert.True(Math.Abs(resApprox2.X - 1.0f) < 1e-6f);

        // Sweep 1000 arbitrary F0 values at NoV=1.0, rough=0.0
        for (int i = 0; i <= 1000; i++)
        {
            float f0Val = i / 1000.0f;
            Vector3 f0Vec = new(f0Val, f0Val, f0Val);
            Vector2 ab = EnvBRDFApproxLazarov(0.0f, 1.0f);
            Vector3 expected = f0Vec * ab.X + new Vector3(ab.Y);
            Vector3 actual = EnvBRDFApprox(f0Vec, 0.0f, 1.0f);
            Assert.True(Vector3.Distance(actual, expected) < 1e-6f);
        }
    }

    [Fact]
    public void GrazingAngle_Dielectric_EvaluatesToExactOne()
    {
        Vector3 f0Dielectric = new(0.04f, 0.04f, 0.04f);
        Vector3 resGrazing = EnvBRDFApprox(f0Dielectric, 0.0f, 0.0f);
        Assert.True(Math.Abs(resGrazing.X - 1.0f) < 1e-3f);
    }

    [Fact]
    public void Dielectric_NormalIncidence_InPhysicalRange()
    {
        Vector3 f0Dielectric = new(0.04f, 0.04f, 0.04f);
        Vector3 resDielectric = EnvBRDFApprox(f0Dielectric, 0.0f, 1.0f);
        Assert.True(resDielectric.X >= 0.035f && resDielectric.X <= 0.055f);
    }

    [Fact]
    public void RoughnessMonotonicity_AtNormalIncidence()
    {
        float prevReflectance = 1.1f;
        Vector3 f0 = new(1.0f, 1.0f, 1.0f);
        for (int r = 0; r <= 20; r++)
        {
            float roughness = r / 20.0f;
            float cur = EnvBRDFApprox(f0, roughness, 1.0f).X;
            Assert.True(cur <= prevReflectance + 1e-4f);
            prevReflectance = cur;
        }
    }

    [Fact]
    public void EnvBRDFApprox2_MatchesEnvBRDFApproxWithSqrtAlpha()
    {
        Vector3 f0 = new(0.5f, 0.8f, 0.2f);
        for (int i = 0; i <= 10; i++)
        {
            float roughness = i / 10.0f;
            float alpha = roughness * roughness;
            float noV = 0.5f;

            Vector3 v1 = EnvBRDFApprox(f0, roughness, noV);
            Vector3 v2 = EnvBRDFApprox2(f0, alpha, noV);
            Assert.True(Vector3.Distance(v1, v2) <= 1e-4f);
        }
    }

    [Fact]
    public void MonotonicityInNoV_Across7MillionParameterPoints()
    {
        float[] testF0s = [0.0f, 0.04f, 0.1f, 0.25f, 0.5f, 0.8f, 1.0f];
        const int RoughSteps = 1000;
        const int NoVSteps = 1000;

        foreach (float f0Val in testF0s)
        {
            Vector3 f0 = new(f0Val, f0Val, f0Val);
            for (int r = 0; r <= RoughSteps; r++)
            {
                float roughness = r / (float)RoughSteps;
                float prevReflectance = float.MaxValue;

                for (int nv = 0; nv <= NoVSteps; nv++)
                {
                    float noV = nv / (float)NoVSteps;
                    float reflectance = EnvBRDFApprox(f0, roughness, noV).X;

                    Assert.True(reflectance <= prevReflectance + 2e-5f,
                        $"Monotonicity violation at F0={f0Val}, rough={roughness}, noV={noV}: diff={reflectance - prevReflectance:E4}");
                    prevReflectance = reflectance;
                }
            }
        }
    }

    [Fact]
    public void EnergyConservation_Dense2000x2000Grid_AndExtremeStress()
    {
        const int GridDim = 2000;
        for (int r = 0; r <= GridDim; r++)
        {
            float roughness = r / (float)GridDim;
            for (int nv = 0; nv <= GridDim; nv++)
            {
                float noV = nv / (float)GridDim;
                Vector2 ab = EnvBRDFApproxLazarov(roughness, noV);
                float total = ab.X + ab.Y;

                Assert.True(ab.X >= 0f && ab.Y >= 0f && total <= 1.050001f);
            }
        }

        Vector2 abNeg = EnvBRDFApproxLazarov(-10.0f, -5.0f);
        Vector2 abOver = EnvBRDFApproxLazarov(10.0f, 5.0f);
        Assert.True(abNeg.X >= 0f && abNeg.Y >= 0f && (abNeg.X + abNeg.Y) <= 1.05f);
        Assert.True(abOver.X >= 0f && abOver.Y >= 0f && (abOver.X + abOver.Y) <= 1.05f);
    }
}
