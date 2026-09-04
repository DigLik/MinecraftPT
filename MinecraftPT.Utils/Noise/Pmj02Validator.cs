using SysMath = global::System.Math;

namespace MinecraftPT.Utils.Noise;

/// <summary>
/// Строгий валидатор математических свойств прогрессивных (0,2)-последовательностей.
/// </summary>
public static class Pmj02Validator
{
    /// <summary>
    /// Проверяет, удовлетворяет ли последовательность точек свойству (0,2)-последовательности
    /// для каждого префикса длины N = 2^m (m = 0..log2(totalN)) и всех разбиений a + b = m.
    /// </summary>
    public static bool ValidateProgressive02(ReadOnlySpan<Sample2D> samples, int totalN, out string errorReason)
    {
        if (totalN <= 0 || (totalN & (totalN - 1)) != 0)
        {
            errorReason = $"Sample count {totalN} is not a positive power of two.";
            return false;
        }

        if (samples.Length < totalN)
        {
            errorReason = $"Sample array length {samples.Length} is less than required totalN {totalN}.";
            return false;
        }

        int maxM = 0;
        while ((1 << maxM) < totalN) maxM++;

        for (int m = 0; m <= maxM; m++)
        {
            int prefixSize = 1 << m;

            for (int a = 0; a <= m; a++)
            {
                int b = m - a;
                int numCols = 1 << a;
                int numRows = 1 << b;

                int[,] counts = new int[numCols, numRows];

                for (int i = 0; i < prefixSize; i++)
                {
                    int col = (int)(samples[i].X * numCols);
                    int row = (int)(samples[i].Y * numRows);
                    col = SysMath.Clamp(col, 0, numCols - 1);
                    row = SysMath.Clamp(row, 0, numRows - 1);

                    counts[col, row]++;
                }

                for (int c = 0; c < numCols; c++)
                {
                    for (int r = 0; r < numRows; r++)
                    {
                        if (counts[c, r] != 1)
                        {
                            errorReason = $"Prefix N={prefixSize} (m={m}) failed (0,2)-stratum property for partition ({a},{b}): cell [{c},{r}] contains {counts[c, r]} samples (expected exactly 1).";
                            return false;
                        }
                    }
                }
            }
        }

        errorReason = string.Empty;
        return true;
    }
}
