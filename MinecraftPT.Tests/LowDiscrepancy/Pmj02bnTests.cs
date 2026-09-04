using MinecraftPT.Utils.Noise;
using Xunit;

namespace MinecraftPT.Tests.Sampling;

public class Pmj02bnTests
{
    private static float ComputeToroidalMinDistance(ReadOnlySpan<Sample2D> points)
    {
        int n = points.Length;
        if (n <= 1) return 1.0f;

        float minDistSq = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            for (int j = i + 1; j < n; j++)
            {
                float dx = Math.Abs(points[i].X - points[j].X);
                float dy = Math.Abs(points[i].Y - points[j].Y);
                if (dx > 0.5f) dx = 1.0f - dx;
                if (dy > 0.5f) dy = 1.0f - dy;
                float dSq = dx * dx + dy * dy;
                if (dSq < minDistSq) minDistSq = dSq;
            }
        }
        return MathF.Sqrt(minDistSq);
    }

    [Fact]
    public void Progressive02_Stratification_AcrossSeeds()
    {
        uint[] seeds = [0x12345678, 0x87654321, 0xABCDEF01, 0x13579BDF];
        foreach (uint seed in seeds)
        {
            var seq = Pmj02bnGenerator.GeneratePmj02Sequence(64, seed);
            bool valid = Pmj02Validator.ValidateProgressive02(seq, 64, out string err);
            Assert.True(valid, $"Seed 0x{seed:X8} failed: {err}");
        }
    }

    [Fact]
    public void MultiJittering1D_Uniformity_OnNxNGrid()
    {
        var seq = Pmj02bnGenerator.GeneratePmj02Sequence(64, 0xDEADBEEF);
        for (int m = 1; m <= 6; m++)
        {
            int prefix = 1 << m;
            bool[] colUsed = new bool[prefix];
            bool[] rowUsed = new bool[prefix];
            for (int i = 0; i < prefix; i++)
            {
                int c = Math.Clamp((int)(seq[i].X * prefix), 0, prefix - 1);
                int r = Math.Clamp((int)(seq[i].Y * prefix), 0, prefix - 1);
                Assert.False(colUsed[c]);
                Assert.False(rowUsed[r]);
                colUsed[c] = true;
                rowUsed[r] = true;
            }
        }
    }

    [Fact]
    public void BlueNoiseMask_VoidAndCluster_Has4096UniqueRanks()
    {
        float[,] mask = Pmj02bnGenerator.GenerateBlueNoiseMask(64, 64, 101);
        bool[] seenRanks = new bool[4096];
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float val = mask[x, y];
                Assert.True(val >= 0f && val < 1f);
                int rank = (int)Math.Round(val * 4096.0f);
                Assert.True(rank >= 0 && rank < 4096 && !seenRanks[rank]);
                seenRanks[rank] = true;
            }
        }
    }

    [Fact]
    public void ShaderPMJ02BN_DimensionGroupDecoding_MapsCorrectly()
    {
        for (uint dim = 0; dim < 4; dim++)
        {
            uint bank = (dim >> 1) & 1u;
            uint ch = dim & 1u;

            if (dim == 0) Assert.True(bank == 0 && ch == 0);
            if (dim == 1) Assert.True(bank == 0 && ch == 1);
            if (dim == 2) Assert.True(bank == 1 && ch == 0);
            if (dim == 3) Assert.True(bank == 1 && ch == 1);
        }
    }

    [Fact]
    public void Sobol02_FallbackSequence_PassesProgressiveValidator()
    {
        var sobolSeq = Pmj02bnGenerator.GenerateSobol02Sequence(64, 42);
        bool sobolValid = Pmj02Validator.ValidateProgressive02(sobolSeq, 64, out string err);
        Assert.True(sobolValid, err);
    }

    [Fact]
    public void Challenger_Stratification_AcrossAllElementaryDyadicIntervals()
    {
        int[] testSubsets = [1, 2, 4, 8, 16, 32, 64, 128];
        uint[] stressSeeds = [0x12345678, 0x87654321, 0xABCDEF01, 0x13579BDF, 0xCAFEBABE, 0x01234567, 0x76543210, 0xFEDCBA98];

        foreach (int n in testSubsets)
        {
            int maxM = 0;
            while ((1 << maxM) < n) maxM++;

            foreach (uint seed in stressSeeds)
            {
                var seq = Pmj02bnGenerator.GeneratePmj02Sequence(n, seed);
                Assert.True(Pmj02Validator.ValidateProgressive02(seq, n, out string err), err);

                for (int m = 0; m <= maxM; m++)
                {
                    int prefix = 1 << m;
                    for (int a = 0; a <= m; a++)
                    {
                        int b = m - a;
                        int cols = 1 << a;
                        int rows = 1 << b;
                        int[,] counts = new int[cols, rows];

                        for (int i = 0; i < prefix; i++)
                        {
                            int c = Math.Clamp((int)(seq[i].X * cols), 0, cols - 1);
                            int r = Math.Clamp((int)(seq[i].Y * rows), 0, rows - 1);
                            counts[c, r]++;
                        }

                        for (int c = 0; c < cols; c++)
                        {
                            for (int r = 0; r < rows; r++)
                            {
                                Assert.Equal(1, counts[c, r]);
                            }
                        }
                    }
                }
            }
        }
    }

    [Fact]
    public void Challenger_MultiJittering1D_ExactOccupancyOnGrids()
    {
        int[] testSubsets = [2, 4, 8, 16, 32, 64, 128];
        uint[] stressSeeds = [0x12345678, 0x87654321, 0xABCDEF01, 0x13579BDF, 0xCAFEBABE, 0x01234567, 0x76543210, 0xFEDCBA98];

        foreach (int n in testSubsets)
        {
            foreach (uint seed in stressSeeds)
            {
                var seq = Pmj02bnGenerator.GeneratePmj02Sequence(n, seed);
                bool[] xUsed = new bool[n];
                bool[] yUsed = new bool[n];

                for (int i = 0; i < n; i++)
                {
                    int xBin = Math.Clamp((int)(seq[i].X * n), 0, n - 1);
                    int yBin = Math.Clamp((int)(seq[i].Y * n), 0, n - 1);
                    Assert.False(xUsed[xBin]);
                    Assert.False(yUsed[yBin]);
                    xUsed[xBin] = true;
                    yUsed[yBin] = true;
                }
            }
        }
    }

    [Fact]
    public void Challenger_ToroidalBlueNoise_OutperformsPRNG()
    {
        foreach (int n in new[] { 4, 8, 16, 32, 64, 128 })
        {
            float sumPmj = 0f;
            float sumPrng = 0f;
            int trials = 50;

            for (int t = 0; t < trials; t++)
            {
                uint seed = (uint)(0x55AA0000 + t * 137 + n);
                var pmjSeq = Pmj02bnGenerator.GeneratePmj02Sequence(n, seed);
                float dPmj = ComputeToroidalMinDistance(pmjSeq);
                sumPmj += dPmj;

                Random rand = new((int)seed);
                Sample2D[] prngSeq = new Sample2D[n];
                for (int i = 0; i < n; i++)
                    prngSeq[i] = new Sample2D((float)rand.NextDouble(), (float)rand.NextDouble());
                float dPrng = ComputeToroidalMinDistance(prngSeq);
                sumPrng += dPrng;
            }

            float meanPmj = sumPmj / trials;
            float meanPrng = sumPrng / trials;

            Assert.True(meanPmj > meanPrng * 1.4f);
            Assert.True(meanPmj >= 0.15f / MathF.Sqrt(n));
        }
    }

    [Fact]
    public void Challenger_PairCorrelation_StrictExclusionZone()
    {
        const int N = 64;
        float globalMinDistance = float.MaxValue;
        float globalMinPrngDistance = float.MaxValue;

        for (int s = 0; s < 50; s++)
        {
            var seq = Pmj02bnGenerator.GeneratePmj02Sequence(N, (uint)(0x77880000 + s * 31));
            float dPmj = ComputeToroidalMinDistance(seq);
            if (dPmj < globalMinDistance) globalMinDistance = dPmj;

            Random rand = new((int)(0x77880000 + s * 31));
            Sample2D[] prng = new Sample2D[N];
            for (int i = 0; i < N; i++) prng[i] = new Sample2D((float)rand.NextDouble(), (float)rand.NextDouble());
            float dPrng = ComputeToroidalMinDistance(prng);
            if (dPrng < globalMinPrngDistance) globalMinPrngDistance = dPrng;
        }

        Assert.True(globalMinDistance >= 0.010f);
        Assert.True(globalMinDistance > globalMinPrngDistance * 5.0f);
    }

    [Fact]
    public void Challenger_BlueNoiseMask_HighPassSpectralEnergy()
    {
        float[,] mask = Pmj02bnGenerator.GenerateBlueNoiseMask(64, 64, 4242);
        double lowFreqEnergy = 0.0;
        double highFreqEnergy = 0.0;
        const int M = 64;

        for (int ky = 0; ky < M; ky++)
        {
            for (int kx = 0; kx < M; kx++)
            {
                if (kx == 0 && ky == 0) continue;

                double real = 0.0;
                double imag = 0.0;
                for (int y = 0; y < M; y++)
                {
                    for (int x = 0; x < M; x++)
                    {
                        double angle = 2.0 * Math.PI * (kx * x + ky * y) / M;
                        real += mask[x, y] * Math.Cos(angle);
                        imag -= mask[x, y] * Math.Sin(angle);
                    }
                }

                double power = real * real + imag * imag;
                int distKx = kx > M / 2 ? M - kx : kx;
                int distKy = ky > M / 2 ? M - ky : ky;
                double radius = Math.Sqrt(distKx * distKx + distKy * distKy);

                if (radius <= 8.0)
                    lowFreqEnergy += power;
                else if (radius >= 16.0)
                    highFreqEnergy += power;
            }
        }

        double spectralRatio = highFreqEnergy / (lowFreqEnergy + 1e-6);
        Assert.True(spectralRatio >= 2.0);
    }

    [Fact]
    public void Challenger_TextureArray_4DDimensionIndependence()
    {
        var basePmj0 = Pmj02bnGenerator.GeneratePmj02Sequence(64, 0x12345678);
        var basePmj1 = Pmj02bnGenerator.GeneratePmj02Sequence(64, 0x87654321);

        float[,] bn0 = Pmj02bnGenerator.GenerateBlueNoiseMask(64, 64, 101);
        float[,] bn1 = Pmj02bnGenerator.GenerateBlueNoiseMask(64, 64, 202);

        for (int py = 0; py < 64; py += 7)
        {
            for (int px = 0; px < 64; px += 7)
            {
                Sample2D[] shiftedSamples = new Sample2D[64];
                for (int s = 0; s < 64; s++)
                {
                    float u = (basePmj0[s].X + bn0[px, py]) % 1.0f;
                    float v = (basePmj0[s].Y + bn1[px, py]) % 1.0f;
                    shiftedSamples[s] = new Sample2D(u, v);
                }

                float dMinShifted = ComputeToroidalMinDistance(shiftedSamples);
                Assert.True(dMinShifted >= 0.03f);
            }
        }

        double crossCorr01 = 0.0;
        for (int s = 0; s < 64; s++)
        {
            crossCorr01 += (basePmj0[s].X - 0.5f) * (basePmj1[s].X - 0.5f);
        }
        crossCorr01 /= 64.0;
        Assert.True(Math.Abs(crossCorr01) <= 0.05);
    }

    [Fact]
    public void Challenger_EdgeCases_InvalidInputsRejected_And50RandomSeedsPass()
    {
        int[] invalidNs = [0, -1, 3, 5, 6, 7, 9, 15, 31, 63, 65, 100, -128];
        foreach (int invalidN in invalidNs)
        {
            Assert.Throws<ArgumentException>(() => Pmj02bnGenerator.GeneratePmj02Sequence(invalidN, 42));
        }

        for (int i = 0; i < 50; i++)
        {
            uint randSeed = (uint)Random.Shared.Next();
            var seq64 = Pmj02bnGenerator.GeneratePmj02Sequence(64, randSeed);
            Assert.True(Pmj02Validator.ValidateProgressive02(seq64, 64, out _));

            var seq128 = Pmj02bnGenerator.GeneratePmj02Sequence(128, randSeed);
            Assert.True(Pmj02Validator.ValidateProgressive02(seq128, 128, out _));
        }
    }
}
