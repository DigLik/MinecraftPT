using Xunit;
using MinecraftPT.Streamline;
using MinecraftPT.Utils.Math;

namespace MinecraftPT.Tests.Streamline;

public class BufferTaggingTests
{
    private struct TagRecord
    {
        public BufferType Type;
        public ResourceLifecycle Lifecycle;
        public Extent Extent;
    }

    private static List<TagRecord> GetActualTagsForDlssSR()
    {
        var list = new List<TagRecord>();
        var renderSize = new Vector2Int(1280, 720);
        var framebufferSize = new Vector2Int(1920, 1080);
        var extentIn = new Extent((uint)renderSize.X, (uint)renderSize.Y);
        var extentOut = new Extent((uint)framebufferSize.X, (uint)framebufferSize.Y);
        var extentExposure = new Extent(1, 1);

        list.Add(new TagRecord { Type = BufferType.kBufferTypeScalingInputColor, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentIn });
        list.Add(new TagRecord { Type = BufferType.kBufferTypeHUDLessColor, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentIn });
        list.Add(new TagRecord { Type = BufferType.kBufferTypeMotionVectors, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentIn });
        list.Add(new TagRecord { Type = BufferType.kBufferTypeDepth, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentIn });
        list.Add(new TagRecord { Type = BufferType.kBufferTypeScalingOutputColor, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentOut });
        list.Add(new TagRecord { Type = BufferType.kBufferTypeExposure, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentExposure });
        list.Add(new TagRecord { Type = BufferType.kBufferTypeBiasCurrentColorHint, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentExposure });
        list.Add(new TagRecord { Type = BufferType.kBufferTypeSpecularMotionVectors, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentIn });
        list.Add(new TagRecord { Type = BufferType.kBufferTypeSpecularHitDistance, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentIn });
        list.Add(new TagRecord { Type = BufferType.kBufferTypeColorBeforeTransparency, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentIn });

        return list;
    }

    private static List<TagRecord> GetActualTagsForDlssRR()
    {
        var list = new List<TagRecord>();
        var renderSize = new Vector2Int(1280, 720);
        var framebufferSize = new Vector2Int(1920, 1080);
        var extentIn = new Extent((uint)renderSize.X, (uint)renderSize.Y);
        var extentOut = new Extent((uint)framebufferSize.X, (uint)framebufferSize.Y);
        var extentExposure = new Extent(1, 1);

        list.Add(new TagRecord { Type = BufferType.kBufferTypeScalingInputColor, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentIn });
        list.Add(new TagRecord { Type = BufferType.kBufferTypeHUDLessColor, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentIn });
        list.Add(new TagRecord { Type = BufferType.kBufferTypeAlbedo, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentIn });
        list.Add(new TagRecord { Type = BufferType.kBufferTypeSpecularAlbedo, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentIn });
        list.Add(new TagRecord { Type = BufferType.kBufferTypeNormals, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentIn });
        list.Add(new TagRecord { Type = BufferType.kBufferTypeRoughness, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentIn });
        list.Add(new TagRecord { Type = BufferType.kBufferTypeMotionVectors, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentIn });
        list.Add(new TagRecord { Type = BufferType.kBufferTypeLinearDepth, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentIn });
        list.Add(new TagRecord { Type = BufferType.kBufferTypeDepth, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentIn });
        list.Add(new TagRecord { Type = BufferType.kBufferTypeScalingOutputColor, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentOut });
        list.Add(new TagRecord { Type = BufferType.kBufferTypeSpecularMotionVectors, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentIn });
        list.Add(new TagRecord { Type = BufferType.kBufferTypeExposure, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentExposure });
        list.Add(new TagRecord { Type = BufferType.kBufferTypeBiasCurrentColorHint, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentExposure });
        list.Add(new TagRecord { Type = BufferType.kBufferTypeOpaqueColor, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentIn });
        list.Add(new TagRecord { Type = BufferType.kBufferTypeSpecularHitDistance, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentIn });
        list.Add(new TagRecord { Type = BufferType.kBufferTypeColorBeforeTransparency, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentIn });
        list.Add(new TagRecord { Type = BufferType.kBufferTypeDiffuseHitNoisy, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentIn });
        list.Add(new TagRecord { Type = BufferType.kBufferTypeSpecularHitNoisy, Lifecycle = ResourceLifecycle.eValidUntilPresent, Extent = extentIn });

        return list;
    }

    [Fact]
    public void DlssSR_BufferTagging_ContainsAllRequiredBuffers()
    {
        var srExpectedBuffers = new HashSet<BufferType>
        {
            BufferType.kBufferTypeScalingInputColor,
            BufferType.kBufferTypeHUDLessColor,
            BufferType.kBufferTypeMotionVectors,
            BufferType.kBufferTypeDepth,
            BufferType.kBufferTypeScalingOutputColor,
            BufferType.kBufferTypeExposure,
            BufferType.kBufferTypeBiasCurrentColorHint,
            BufferType.kBufferTypeSpecularMotionVectors,
            BufferType.kBufferTypeSpecularHitDistance,
            BufferType.kBufferTypeColorBeforeTransparency
        };

        var srActualTags = GetActualTagsForDlssSR();
        Assert.Equal(10, srActualTags.Count);

        var srActualTypes = new HashSet<BufferType>();
        foreach (var tag in srActualTags)
        {
            srActualTypes.Add(tag.Type);
            Assert.Equal(ResourceLifecycle.eValidUntilPresent, tag.Lifecycle);
        }

        Assert.True(srExpectedBuffers.SetEquals(srActualTypes));
    }

    [Fact]
    public void DlssRR_BufferTagging_ContainsAllRequiredBuffersAndCorrectExtents()
    {
        var rrExpectedBuffers = new HashSet<BufferType>
        {
            BufferType.kBufferTypeScalingInputColor,
            BufferType.kBufferTypeHUDLessColor,
            BufferType.kBufferTypeAlbedo,
            BufferType.kBufferTypeSpecularAlbedo,
            BufferType.kBufferTypeNormals,
            BufferType.kBufferTypeRoughness,
            BufferType.kBufferTypeMotionVectors,
            BufferType.kBufferTypeLinearDepth,
            BufferType.kBufferTypeDepth,
            BufferType.kBufferTypeScalingOutputColor,
            BufferType.kBufferTypeSpecularMotionVectors,
            BufferType.kBufferTypeExposure,
            BufferType.kBufferTypeBiasCurrentColorHint,
            BufferType.kBufferTypeOpaqueColor,
            BufferType.kBufferTypeSpecularHitDistance,
            BufferType.kBufferTypeColorBeforeTransparency,
            BufferType.kBufferTypeDiffuseHitNoisy,
            BufferType.kBufferTypeSpecularHitNoisy
        };

        var rrActualTags = GetActualTagsForDlssRR();
        Assert.Equal(18, rrActualTags.Count);

        var rrActualTypes = new HashSet<BufferType>();
        foreach (var tag in rrActualTags)
        {
            rrActualTypes.Add(tag.Type);
            Assert.Equal(ResourceLifecycle.eValidUntilPresent, tag.Lifecycle);
        }

        Assert.True(rrExpectedBuffers.SetEquals(rrActualTypes));

        var renderSize = new Vector2Int(1280, 720);
        var framebufferSize = new Vector2Int(1920, 1080);
        foreach (var tag in rrActualTags)
        {
            if (tag.Type == BufferType.kBufferTypeScalingOutputColor)
            {
                Assert.Equal((uint)framebufferSize.X, tag.Extent.Width);
                Assert.Equal((uint)framebufferSize.Y, tag.Extent.Height);
            }
            else if (tag.Type is BufferType.kBufferTypeExposure or BufferType.kBufferTypeBiasCurrentColorHint)
            {
                Assert.Equal(1u, tag.Extent.Width);
                Assert.Equal(1u, tag.Extent.Height);
            }
            else
            {
                Assert.Equal((uint)renderSize.X, tag.Extent.Width);
                Assert.Equal((uint)renderSize.Y, tag.Extent.Height);
            }
        }
    }
}
