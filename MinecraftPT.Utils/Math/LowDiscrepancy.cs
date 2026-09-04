using System.Numerics;
using System.Runtime.CompilerServices;

namespace MinecraftPT.Utils.Math;

/// <summary>
/// Высокопроизводительные, аллокационно-чистые генераторы низкодисперсионных последовательностей (Halton, Sobol, R2).
/// Оптимизированы для .NET 10, SIMD и побитовых инструкций.
/// </summary>
public static class LowDiscrepancy
{
    public const float PlasticConstant = 1.324717957244746f;
    public const float R2Alpha1 = 0.7548776662466928f; // 1 / phi_2
    public const float R2Alpha2 = 0.5698402909980533f; // 1 / phi_2^2
    public const double R2Alpha1Double = 0.75487766624669276005;
    public const double R2Alpha2Double = 0.56984029099805326591;
    public const uint R2Const1 = 0xC140A7A0u; // round(Alpha1Double * 2^32)
    public const uint R2Const2 = 0x91E10DA6u; // round(Alpha2Double * 2^32)

    private const float InvTwoPow32 = 2.3283064365386963e-10f; // 1.0f / 2^32

    // 32 предвычисленных направляющих числа Соболя / Нидеррайтера для размерности 0 и 1
    private static readonly uint[] SobolV0 = InitializeSobolV0();
    private static readonly uint[] SobolV1 = InitializeSobolV1();

    private static uint[] InitializeSobolV0()
    {
        uint[] v = new uint[32];
        for (int k = 0; k < 32; k++)
        {
            v[k] = 1u << (31 - k);
        }
        return v;
    }

    private static uint[] InitializeSobolV1()
    {
        uint[] v = new uint[32];
        for (int k = 0; k < 32; k++)
        {
            uint val = 0;
            for (int j = 0; j <= k; j++)
            {
                // Lucas theorem: C(k, j) is odd iff (j & k) == j
                if ((j & k) == j)
                {
                    val |= (1u << (31 - j));
                }
            }
            v[k] = val;
        }
        return v;
    }

    #region Bit Manipulation

    /// <summary>
    /// Побитовая реверсия 32-битного целого беззнакового числа.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint ReverseBits(uint x)
    {
        x = ((x & 0x55555555u) << 1) | ((x & 0xAAAAAAAAu) >> 1);
        x = ((x & 0x33333333u) << 2) | ((x & 0xCCCCCCCCu) >> 2);
        x = ((x & 0x0F0F0F0Fu) << 4) | ((x & 0xF0F0F0F0u) >> 4);
        x = ((x & 0x00FF00FFu) << 8) | ((x & 0xFF00FF00u) >> 8);
        return (x << 16) | (x >> 16);
    }

    #endregion

    #region Halton & Radical Inverse

    /// <summary>
    /// Радикальная инверсия Ван дер Корпута по основанию 2.
    /// Выполняется через побитовую реверсию 32-битного целого.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float RadicalInverseBase2(uint index)
    {
        return ReverseBits(index) * InvTwoPow32;
    }

    /// <summary>
    /// Радикальная инверсия Ван дер Корпута по основанию 3.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float RadicalInverseBase3(uint index)
    {
        float result = 0f;
        float f = 1.0f / 3.0f;
        uint i = index;
        while (i > 0)
        {
            result += f * (i % 3);
            i /= 3;
            f *= (1.0f / 3.0f);
        }
        return result;
    }

    /// <summary>
    /// Обобщенная радикальная инверсия по произвольному основанию base >= 2.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float RadicalInverse(uint index, uint @base)
    {
        if (@base == 2) return RadicalInverseBase2(index);
        if (@base == 3) return RadicalInverseBase3(index);

        float result = 0f;
        float invBase = 1.0f / @base;
        float f = invBase;
        uint i = index;
        while (i > 0)
        {
            result += f * (i % @base);
            i /= @base;
            f *= invBase;
        }
        return result;
    }

    /// <summary>
    /// Генерация 2D точки последовательности Халтона (основания 2 и 3) в диапазоне [0, 1)^2.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Halton2D(uint index)
    {
        return new Vector2(RadicalInverseBase2(index), RadicalInverseBase3(index));
    }

    /// <summary>
    /// Генерация субпиксельного джиттера для TAA / DLSS в диапазоне [-0.5, 0.5]^2.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 HaltonJitter(uint frameIndex, uint phaseCount = 16)
    {
        uint index = (frameIndex % phaseCount) + 1;
        return new Vector2(RadicalInverseBase2(index) - 0.5f, RadicalInverseBase3(index) - 0.5f);
    }

    #endregion

    #region R2 Sequence (Plastic Constant)

    /// <summary>
    /// Генерация 2D точки последовательности R2 (Мартин Робертс, пластическое число).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 R2Sample(uint index, Vector2 offset = default)
    {
        float x = (offset.X + index * R2Alpha1) % 1.0f;
        float y = (offset.Y + index * R2Alpha2) % 1.0f;
        if (x < 0) x += 1.0f;
        if (y < 0) y += 1.0f;
        return new Vector2(x, y);
    }

    /// <summary>
    /// Вычисление циклического смещения R2 с фиксированной точкой (аналог HLSL raygen.hlsl:33-35).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 R2CycleOffset(uint cycle)
    {
        uint t1 = unchecked(cycle * R2Const1);
        uint t2 = unchecked(cycle * R2Const2);
        return new Vector2((t1 >> 8) / 16777216.0f, (t2 >> 8) / 16777216.0f);
    }

    #endregion

    #region Sobol Sequence (Antonov-Saleev / Niederreiter)

    /// <summary>
    /// Генерация 2D сэмпла Соболя / Нидеррайтера (0,2)-последовательности по индексу i.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Sobol2D(uint index)
    {
        uint x0 = 0;
        uint x1 = 0;

        for (int k = 0; k < 32 && (index >> k) > 0; k++)
        {
            if (((index >> k) & 1) != 0)
            {
                x0 ^= SobolV0[k];
                x1 ^= SobolV1[k];
            }
        }

        return new Vector2(x0 * InvTwoPow32, x1 * InvTwoPow32);
    }

    #endregion
}
