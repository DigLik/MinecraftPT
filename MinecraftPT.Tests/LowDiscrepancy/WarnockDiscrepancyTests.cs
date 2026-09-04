using System.Numerics;
using MinecraftPT.Utils.Math;
using MinecraftPT.Utils.Noise;
using Xunit;

namespace MinecraftPT.Tests.Sampling;

public class WarnockDiscrepancyTests
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

    [Fact]
    public void WarnockL2StarDiscrepancy_MultiScale_OutperformsPrngMeanAndMin()
    {
        int[] sampleCounts = [16, 32, 64, 128, 256, 512, 1024, 2048, 4096];

        foreach (int N in sampleCounts)
        {
            Vector2[] sobolPts = new Vector2[N];
            Vector2[] haltonPts = new Vector2[N];
            Vector2[] r2Pts = new Vector2[N];

            for (int i = 0; i < N; i++)
            {
                sobolPts[i] = LowDiscrepancy.Sobol2D((uint)i);
                haltonPts[i] = LowDiscrepancy.Halton2D((uint)(i + 1));
                r2Pts[i] = LowDiscrepancy.R2Sample((uint)i);
            }

            double dSobol = ComputeL2StarDiscrepancy(sobolPts);
            double dHalton = ComputeL2StarDiscrepancy(haltonPts);
            double dR2 = ComputeL2StarDiscrepancy(r2Pts);

            const int PrngTrials = 100;
            double prngSum = 0.0;
            double prngMin = double.MaxValue;

            for (int s = 0; s < PrngTrials; s++)
            {
                Vector2[] randPts = new Vector2[N];
                Random prng = new(s * 7919 + 42);
                for (int i = 0; i < N; i++)
                {
                    randPts[i] = new Vector2((float)prng.NextDouble(), (float)prng.NextDouble());
                }
                double dRand = ComputeL2StarDiscrepancy(randPts);
                prngSum += dRand;
                if (dRand < prngMin) prngMin = dRand;
            }

            double prngMean = prngSum / PrngTrials;

            Assert.True(dSobol < prngMean);
            Assert.True(dHalton < prngMean);
            Assert.True(dR2 < prngMean);

            if (N >= 64)
            {
                Assert.True(dSobol < prngMin);
                Assert.True(dHalton < prngMin);
                Assert.True(dR2 < prngMin);
            }
        }
    }

    [Fact]
    public void Sobol_DyadicStratification_ScalesUpTo4096()
    {
        for (int m = 1; m <= 12; m++)
        {
            int N = 1 << m;
            Vector2[] pts = new Vector2[N];
            for (int i = 0; i < N; i++)
            {
                pts[i] = LowDiscrepancy.Sobol2D((uint)i);
            }

            for (int a = 0; a <= m; a++)
            {
                int b = m - a;
                int numCols = 1 << a;
                int numRows = 1 << b;
                int[,] grid = new int[numCols, numRows];

                for (int i = 0; i < N; i++)
                {
                    int c = Math.Clamp((int)(pts[i].X * numCols), 0, numCols - 1);
                    int r = Math.Clamp((int)(pts[i].Y * numRows), 0, numRows - 1);
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
}
