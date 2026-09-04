using System.Numerics;
using MinecraftPT.Utils.Math;
using MinecraftPT.Utils.Noise;
using Xunit;

namespace MinecraftPT.Tests.Sampling;

public class LowDiscrepancyTests
{
    private static double ComputeL2StarDiscrepancy(ReadOnlySpan<Vector2> points)
    {
        int n = points.Length;
        if (n == 0) return 1.0;

        double term1 = 1.0 / 9.0;
        double sum1 = 0.0;
        for (int i = 0; i < n; i++)
        {
            double xi = points[i].X;
            double yi = points[i].Y;
            sum1 += ((1.0 - xi * xi) / 2.0) * ((1.0 - yi * yi) / 2.0);
        }
        double term2 = (2.0 / n) * sum1;

        double sum2 = 0.0;
        for (int i = 0; i < n; i++)
        {
            double xi = points[i].X;
            double yi = points[i].Y;
            for (int k = 0; k < n; k++)
            {
                double xk = points[k].X;
                double yk = points[k].Y;
                sum2 += (1.0 - Math.Max(xi, xk)) * (1.0 - Math.Max(yi, yk));
            }
        }
        double term3 = sum2 / ((double)n * n);

        double t2 = term1 - term2 + term3;
        return Math.Sqrt(Math.Max(0.0, t2));
    }

    private static double ComputeChiSquareUniformity2D(ReadOnlySpan<Vector2> points, int gridDim)
    {
        int totalBins = gridDim * gridDim;
        int n = points.Length;
        int[] binCounts = new int[totalBins];

        for (int i = 0; i < n; i++)
        {
            int bx = (int)(points[i].X * gridDim);
            int by = (int)(points[i].Y * gridDim);
            bx = Math.Clamp(bx, 0, gridDim - 1);
            by = Math.Clamp(by, 0, gridDim - 1);
            binCounts[by * gridDim + bx]++;
        }

        double expected = (double)n / totalBins;
        double chiSquare = 0.0;
        for (int b = 0; b < totalBins; b++)
        {
            double diff = binCounts[b] - expected;
            chiSquare += (diff * diff) / expected;
        }
        return chiSquare;
    }

    private static float ComputeMinDistance(ReadOnlySpan<Vector2> points)
    {
        int n = points.Length;
        float minDist = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                float dx = points[i].X - points[j].X;
                float dy = points[i].Y - points[j].Y;
                float dist = MathF.Sqrt(dx * dx + dy * dy);
                if (dist < minDist) minDist = dist;
            }
        }
        return minDist;
    }

    [Fact]
    public void VanDerCorput_Base2_AnalyticalAndBitReversalEquivalence()
    {
        float[] expectedBase2 = [0.5f, 0.25f, 0.75f, 0.125f, 0.625f];
        for (uint i = 1; i <= 5; i++)
        {
            float val = LowDiscrepancy.RadicalInverseBase2(i);
            Assert.True(Math.Abs(val - expectedBase2[i - 1]) < 1e-6f);
        }

        for (uint i = 1; i <= 10000; i++)
        {
            float v1 = LowDiscrepancy.RadicalInverseBase2(i);
            float v2 = LowDiscrepancy.RadicalInverse(i, 2);
            Assert.True(Math.Abs(v1 - v2) < 1e-6f);
        }
    }

    [Fact]
    public void VanDerCorput_Base3_AnalyticalValues()
    {
        float[] expectedBase3 = [1f / 3f, 2f / 3f, 1f / 9f, 4f / 9f, 7f / 9f, 2f / 9f, 5f / 9f, 8f / 9f, 1f / 27f];
        for (uint i = 1; i <= 9; i++)
        {
            float val = LowDiscrepancy.RadicalInverseBase3(i);
            Assert.True(Math.Abs(val - expectedBase3[i - 1]) < 1e-6f);
        }
    }

    [Fact]
    public void R2_PlasticConstant_AndFixedPointConstants()
    {
        double phi = LowDiscrepancy.PlasticConstant;
        double rootEq = (phi * phi * phi) - phi - 1.0;
        Assert.True(Math.Abs(rootEq) < 1e-5);

        float alpha1Approx = (LowDiscrepancy.R2Const1 >> 8) / 16777216.0f;
        float alpha2Approx = (LowDiscrepancy.R2Const2 >> 8) / 16777216.0f;
        Assert.True(Math.Abs(alpha1Approx - LowDiscrepancy.R2Alpha1) < 2e-5f);
        Assert.True(Math.Abs(alpha2Approx - LowDiscrepancy.R2Alpha2) < 2e-5f);

        Vector2 offset = LowDiscrepancy.R2CycleOffset(5);
        uint t1 = unchecked(5u * 0xC140A7A0u);
        uint t2 = unchecked(5u * 0x91E10DA6u);
        Vector2 hlslOffset = new((t1 >> 8) / 16777216.0f, (t2 >> 8) / 16777216.0f);
        Assert.True(Vector2.Distance(offset, hlslOffset) < 1e-6f);
    }

    [Fact]
    public void Sobol_DyadicStratification_PowersOfTwo()
    {
        for (int m = 2; m <= 8; m++)
        {
            int n = 1 << m;
            Vector2[] sobolPoints = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                sobolPoints[i] = LowDiscrepancy.Sobol2D((uint)i);
            }

            for (int a = 0; a <= m; a++)
            {
                int b = m - a;
                int numCols = 1 << a;
                int numRows = 1 << b;
                int[,] grid = new int[numCols, numRows];

                for (int i = 0; i < n; i++)
                {
                    int c = (int)(sobolPoints[i].X * numCols);
                    int r = (int)(sobolPoints[i].Y * numRows);
                    c = Math.Clamp(c, 0, numCols - 1);
                    r = Math.Clamp(r, 0, numRows - 1);
                    grid[c, r]++;
                }

                for (int c = 0; c < numCols; c++)
                {
                    for (int r = 0; r < numRows; r++)
                    {
                        Assert.Equal(1, grid[c, r]);
                    }
                }
            }
        }
    }

    [Fact]
    public void L2StarDiscrepancy_OutperformsPRNG()
    {
        const int N = 256;
        Vector2[] sobolPts = new Vector2[N];
        Vector2[] haltonPts = new Vector2[N];
        Vector2[] r2Pts = new Vector2[N];
        Vector2[] randPts = new Vector2[N];

        Random prng = new(1337);
        for (int i = 0; i < N; i++)
        {
            sobolPts[i] = LowDiscrepancy.Sobol2D((uint)i);
            haltonPts[i] = LowDiscrepancy.Halton2D((uint)(i + 1));
            r2Pts[i] = LowDiscrepancy.R2Sample((uint)i);
            randPts[i] = new Vector2((float)prng.NextDouble(), (float)prng.NextDouble());
        }

        double discSobol = ComputeL2StarDiscrepancy(sobolPts);
        double discHalton = ComputeL2StarDiscrepancy(haltonPts);
        double discR2 = ComputeL2StarDiscrepancy(r2Pts);
        double discRand = ComputeL2StarDiscrepancy(randPts);

        Assert.True(discSobol < discRand);
        Assert.True(discHalton < discRand);
        Assert.True(discR2 < discRand);
    }

    [Fact]
    public void ChiSquare_2DUniformity_BelowCriticalThreshold()
    {
        const int N = 1024;
        Vector2[] sobolPts = new Vector2[N];
        Vector2[] haltonPts = new Vector2[N];
        Vector2[] r2Pts = new Vector2[N];

        for (int i = 0; i < N; i++)
        {
            sobolPts[i] = LowDiscrepancy.Sobol2D((uint)i);
            haltonPts[i] = LowDiscrepancy.Halton2D((uint)(i + 1));
            r2Pts[i] = LowDiscrepancy.R2Sample((uint)i);
        }

        double chiSobol = ComputeChiSquareUniformity2D(sobolPts, 8);
        double chiHalton = ComputeChiSquareUniformity2D(haltonPts, 8);
        double chiR2 = ComputeChiSquareUniformity2D(r2Pts, 8);

        Assert.Equal(0.0, chiSobol);
        Assert.True(chiHalton < 25.0);
        Assert.True(chiR2 < 25.0);
    }

    [Fact]
    public void AntiClustering_MinimalDistance_ExceedsPRNG()
    {
        const int N = 256;
        Vector2[] sobolPts = new Vector2[N];
        Vector2[] r2Pts = new Vector2[N];
        Vector2[] haltonPts = new Vector2[N];
        Vector2[] randPts = new Vector2[N];

        Random prng = new(42);
        for (int i = 0; i < N; i++)
        {
            sobolPts[i] = LowDiscrepancy.Sobol2D((uint)i);
            r2Pts[i] = LowDiscrepancy.R2Sample((uint)i);
            haltonPts[i] = LowDiscrepancy.Halton2D((uint)(i + 1));
            randPts[i] = new Vector2((float)prng.NextDouble(), (float)prng.NextDouble());
        }

        float dMinSobol = ComputeMinDistance(sobolPts);
        float dMinR2 = ComputeMinDistance(r2Pts);
        float dMinHalton = ComputeMinDistance(haltonPts);
        float dMinRand = ComputeMinDistance(randPts);

        Assert.True(dMinSobol > 0.004f);
        Assert.True(dMinR2 > 0.02f);
        Assert.True(dMinHalton > 0.015f);
        Assert.True(dMinRand < 0.002f);
    }

    [Fact]
    public void HaltonJitter_Properties_And_ZeroAlloc()
    {
        Vector2 sumJitter = Vector2.Zero;
        HashSet<Vector2> uniquePhases = new();
        bool inBounds = true;

        for (uint f = 0; f < 16; f++)
        {
            Vector2 jitter = LowDiscrepancy.HaltonJitter(f, 16);
            if (jitter.X <= -0.5f || jitter.X >= 0.5f || jitter.Y <= -0.5f || jitter.Y >= 0.5f)
                inBounds = false;
            sumJitter += jitter;
            uniquePhases.Add(jitter);
        }

        Vector2 meanJitter = sumJitter / 16f;
        Assert.True(inBounds);
        Assert.True(Math.Abs(meanJitter.X) < 0.05f && Math.Abs(meanJitter.Y) < 0.05f);
        Assert.Equal(16, uniquePhases.Count);

        GC.Collect();
        long allocBefore = GC.GetAllocatedBytesForCurrentThread();
        for (uint f = 0; f < 100_000; f++)
        {
            _ = LowDiscrepancy.HaltonJitter(f, 16);
        }
        long allocAfter = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(0, allocAfter - allocBefore);
    }
}
