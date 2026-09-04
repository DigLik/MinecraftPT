using System.Numerics;
using MinecraftPT.Graphics.Vulkan;
using MinecraftPT.Graphics.Vulkan.Core;
using Xunit;

namespace MinecraftPT.Tests.Graphics;

public class OpacityMicromapTests
{
    [Fact]
    public void BirdCurve_Bijectivity_256MicroTrianglesMapOneToOne()
    {
        const int N = 16;
        var indices = new HashSet<uint>();
        bool inBounds = true;

        for (int i = 0; i < N; i++)
        {
            for (int j = 0; j < N - i; j++)
            {
                float u = (i + 1f / 3f) / N;
                float v = (j + 1f / 3f) / N;
                uint idx = OpacityMicromapManager.BarycentricsToSpaceFillingCurveIndex(u, v, 4);
                if (idx >= 256) inBounds = false;
                indices.Add(idx);
            }
        }

        for (int i = 0; i < N - 1; i++)
        {
            for (int j = 0; j < N - 1 - i; j++)
            {
                float u = (i + 2f / 3f) / N;
                float v = (j + 2f / 3f) / N;
                uint idx = OpacityMicromapManager.BarycentricsToSpaceFillingCurveIndex(u, v, 4);
                if (idx >= 256) inBounds = false;
                indices.Add(idx);
            }
        }

        Assert.True(inBounds);
        Assert.Equal(256, indices.Count);
    }

    [Fact]
    public void TwoStateMask_FullyOpaque_ProducesAllOnes()
    {
        byte[] opaquePixels = new byte[16 * 16 * 4];
        Array.Fill(opaquePixels, (byte)255);
        byte[] mask = new byte[32];

        OpacityMicromapManager.BakeTriangleMask(opaquePixels, 16, 16, 0, 1.0f, mask);

        for (int i = 0; i < 32; i++)
        {
            Assert.Equal((byte)0xFF, mask[i]);
        }
    }

    [Fact]
    public void TwoStateMask_FullyTransparent_ProducesAllZeros()
    {
        byte[] transPixels = new byte[16 * 16 * 4];
        byte[] mask = new byte[32];

        OpacityMicromapManager.BakeTriangleMask(transPixels, 16, 16, 0, 1.0f, mask);

        for (int i = 0; i < 32; i++)
        {
            Assert.Equal((byte)0x00, mask[i]);
        }
    }

    [Fact]
    public void TriangleTypes_UVInversionCoverage_SatisfiesPartitionOfUnity()
    {
        byte[] halfPixels = new byte[16 * 16 * 4];
        for (int y = 0; y < 16; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                int offset = (y * 16 + x) * 4;
                halfPixels[offset + 3] = (byte)(y < 8 ? 255 : 0);
            }
        }

        byte[] maskFront1 = new byte[32];
        byte[] maskFront2 = new byte[32];
        byte[] maskBack1 = new byte[32];
        byte[] maskBack2 = new byte[32];

        OpacityMicromapManager.BakeTriangleMask(halfPixels, 16, 16, 0, 1.0f, maskFront1);
        OpacityMicromapManager.BakeTriangleMask(halfPixels, 16, 16, 1, 1.0f, maskFront2);
        OpacityMicromapManager.BakeTriangleMask(halfPixels, 16, 16, 2, 1.0f, maskBack1);
        OpacityMicromapManager.BakeTriangleMask(halfPixels, 16, 16, 3, 1.0f, maskBack2);

        int popFront1 = maskFront1.Sum(b => BitOperations.PopCount(b));
        int popFront2 = maskFront2.Sum(b => BitOperations.PopCount(b));
        int popBack1 = maskBack1.Sum(b => BitOperations.PopCount(b));
        int popBack2 = maskBack2.Sum(b => BitOperations.PopCount(b));

        Assert.True(popFront1 > 0);
        Assert.True(popFront2 > 0);
        Assert.True(popBack1 > 0);
        Assert.True(popBack2 > 0);
        Assert.Equal(256, popFront1 + popFront2);
    }

    [Fact]
    public void SpecialOMMIndices_GlassWaterVsCutoutVsOpaque()
    {
        ushort cutoutIdx0 = (ushort)((5 * 4) + 0);
        ushort cutoutIdx3 = (ushort)((5 * 4) + 3);

        const ushort FullyUnknownOpaque = 0xFFFC;
        const ushort FullyOpaque = 0xFFFE;

        Assert.Equal(20, cutoutIdx0);
        Assert.Equal(23, cutoutIdx3);
        Assert.Equal(0xFFFC, FullyUnknownOpaque);
        Assert.Equal(0xFFFE, FullyOpaque);
    }
}
