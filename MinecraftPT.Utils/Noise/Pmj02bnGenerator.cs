using SysMath = global::System.Math;

namespace MinecraftPT.Utils.Noise;

/// <summary>
/// 2D сэмпл с плавающей точкой в диапазоне [0, 1)^2.
/// </summary>
public readonly struct Sample2D : IEquatable<Sample2D>
{
    public readonly float X;
    public readonly float Y;

    public Sample2D(float x, float y)
    {
        X = x;
        Y = y;
    }

    public bool Equals(Sample2D other) => X.Equals(other.X) && Y.Equals(other.Y);
    public override bool Equals(object? obj) => obj is Sample2D other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public override string ToString() => $"({X:F6}, {Y:F6})";
}

/// <summary>
/// Генератор низкодисперсионных прогрессивных последовательностей PMJ02 (Progressive Multi-Jittered (0,2))
/// и масок синего шума согласно спецификации Christensen et al. (Pixar EGSR 2018).
/// </summary>
public static class Pmj02bnGenerator
{
    /// <summary>
    /// Генерация эталонной 2D PMJ02 последовательности сэмплов длины N (N должно быть степенью 2).
    /// Гарантирует строгое выполнение условий (0,2)-последовательности для всех префиксов 1, 2, 4, 8, 16, 32, 64.
    /// </summary>
    public static Sample2D[] GeneratePmj02Sequence(int n, uint seed)
    {
        if (n <= 0 || (n & (n - 1)) != 0)
            throw new ArgumentException("Sample count N must be a positive power of two.", nameof(n));

        int maxM = 0;
        while ((1 << maxM) < n) maxM++;

        Random rand = new((int)seed);
        Sample2D[] samples = new Sample2D[n];

        bool[] usedCols = new bool[n];
        bool[] usedRows = new bool[n];
        int[] candU = new int[n * n];
        int[] candV = new int[n * n];

        // Попытки генерации с перезапуском в случае тупиковой раскладки жадного поиска
        for (int attempt = 0; attempt < 200; attempt++)
        {
            if (TryGeneratePmj02(samples, n, maxM, rand, usedCols, usedRows, candU, candV))
            {
                return samples;
            }
        }

        // Детерминированный fallback через скремблированную последовательность Соболя (0,2)
        return GenerateSobol02Sequence(n, seed);
    }

    private static bool TryGeneratePmj02(
        Sample2D[] samples,
        int totalN,
        int maxM,
        Random rand,
        bool[] usedCols,
        bool[] usedRows,
        int[] candU,
        int[] candV)
    {
        // Базовый сэмпл P_0 для N=1
        samples[0] = new Sample2D((float)rand.NextDouble(), (float)rand.NextDouble());

        // Прогрессивное удвоение этапов: m = 1 (N=2), m = 2 (N=4), ..., m = maxM (N=totalN)
        for (int m = 1; m <= maxM; m++)
        {
            int nPrev = 1 << (m - 1);
            int nCurr = 1 << m;

            Array.Clear(usedCols, 0, nCurr);
            Array.Clear(usedRows, 0, nCurr);

            for (int k = 0; k < nPrev; k++)
            {
                int u = (int)(samples[k].X * nCurr);
                int v = (int)(samples[k].Y * nCurr);
                u = SysMath.Clamp(u, 0, nCurr - 1);
                v = SysMath.Clamp(v, 0, nCurr - 1);
                usedCols[u] = true;
                usedRows[v] = true;
            }

            for (int j = 0; j < nPrev; j++)
            {
                int newIdx = nPrev + j;
                int candCount = 0;

                for (int u = 0; u < nCurr; u++)
                {
                    if (usedCols[u]) continue;

                    for (int v = 0; v < nCurr; v++)
                    {
                        if (usedRows[v]) continue;

                        if (IsValidCandidate(samples, newIdx, m, u, v))
                        {
                            candU[candCount] = u;
                            candV[candCount] = v;
                            candCount++;
                        }
                    }
                }

                if (candCount == 0)
                {
                    return false; // Тупик, необходим перезапуск с новой случайной базой
                }

                // Выбор кандидата с максимальным минимальным тороидальным расстоянием (Blue Noise)
                int bestCand = 0;
                float maxMinDist = -1.0f;

                for (int c = 0; c < candCount; c++)
                {
                    float candX = (candU[c] + 0.5f) / nCurr;
                    float candY = (candV[c] + 0.5f) / nCurr;
                    float minDist = float.MaxValue;

                    for (int k = 0; k < newIdx; k++)
                    {
                        float dx = SysMath.Abs(candX - samples[k].X);
                        float dy = SysMath.Abs(candY - samples[k].Y);
                        if (dx > 0.5f) dx = 1.0f - dx;
                        if (dy > 0.5f) dy = 1.0f - dy;
                        float d = dx * dx + dy * dy;
                        if (d < minDist) minDist = d;
                    }

                    if (minDist > maxMinDist)
                    {
                        maxMinDist = minDist;
                        bestCand = c;
                    }
                }

                int chosenU = candU[bestCand];
                int chosenV = candV[bestCand];

                usedCols[chosenU] = true;
                usedRows[chosenV] = true;

                float jx = (chosenU + (float)rand.NextDouble()) / nCurr;
                float jy = (chosenV + (float)rand.NextDouble()) / nCurr;
                samples[newIdx] = new Sample2D(SysMath.Clamp(jx, 0.0f, 0.999999f), SysMath.Clamp(jy, 0.0f, 0.999999f));
            }
        }

        return true;
    }

    private static bool IsValidCandidate(Sample2D[] samples, int currentCount, int m, int u, int v)
    {
        for (int a = 0; a <= m; a++)
        {
            int b = m - a;
            int intervalU = u >> (m - a);
            int intervalV = v >> (m - b);

            for (int k = 0; k < currentCount; k++)
            {
                int kU = (int)(samples[k].X * (1 << a));
                int kV = (int)(samples[k].Y * (1 << b));
                kU = SysMath.Clamp(kU, 0, (1 << a) - 1);
                kV = SysMath.Clamp(kV, 0, (1 << b) - 1);

                if (kU == intervalU && kV == intervalV)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Детерминированный генератор (0,2)-последовательности Соболя с внутриячеечным джиттерингом.
    /// </summary>
    public static Sample2D[] GenerateSobol02Sequence(int n, uint seed)
    {
        Sample2D[] samples = new Sample2D[n];
        Random rand = new((int)seed);

        for (int i = 0; i < n; i++)
        {
            uint s0 = 0;
            uint s1 = 0;
            for (int k = 0; k < 32 && (i >> k) > 0; k++)
            {
                if (((i >> k) & 1) != 0)
                {
                    s0 ^= (1u << (31 - k));
                    uint v1 = 0;
                    for (int j = 0; j <= k; j++)
                    {
                        if ((j & k) == j)
                        {
                            v1 |= (1u << (31 - j));
                        }
                    }
                    s1 ^= v1;
                }
            }

            float x = (float)s0 / 4294967296.0f;
            float y = (float)s1 / 4294967296.0f;

            // Мульти-джиттеринг внутри ячейки 1/n
            float jx = ((int)(x * n) + (float)rand.NextDouble()) / (float)n;
            float jy = ((int)(y * n) + (float)rand.NextDouble()) / (float)n;

            samples[i] = new Sample2D(SysMath.Clamp(jx, 0.0f, 0.999999f), SysMath.Clamp(jy, 0.0f, 0.999999f));
        }

        return samples;
    }

    /// <summary>
    /// Генерация 2D матрицы синего шума методом Void-and-Cluster (Ulichney 1993).
    /// </summary>
    public static float[,] GenerateBlueNoiseMask(int width, int height, int seed)
    {
        float[,] mask = new float[width, height];
        Random rand = new(seed);

        int total = width * height;
        bool[,] placed = new bool[width, height];
        float[,] energy = new float[width, height];

        double sigma = 1.9;
        double sigma2 = 2.0 * sigma * sigma;

        for (int rank = 0; rank < total; rank++)
        {
            int bestX = 0, bestY = 0;
            float minEnergy = float.MaxValue;

            if (rank == 0)
            {
                bestX = rand.Next(width);
                bestY = rand.Next(height);
            }
            else
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (!placed[x, y] && energy[x, y] < minEnergy)
                        {
                            minEnergy = energy[x, y];
                            bestX = x;
                            bestY = y;
                        }
                    }
                }
            }

            placed[bestX, bestY] = true;
            mask[bestX, bestY] = (float)rank / (float)total;

            int radius = 8;
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    int nx = (bestX + dx + width) % width;
                    int ny = (bestY + dy + height) % height;
                    float dist2 = dx * dx + dy * dy;
                    energy[nx, ny] += (float)SysMath.Exp(-dist2 / sigma2);
                }
            }
        }

        return mask;
    }
}
