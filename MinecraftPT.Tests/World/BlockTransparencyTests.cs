using System.Reflection;
using MinecraftPT.Game.World.Blocks;
using MinecraftPT.Game.World.Blocks.Services;
using MinecraftPT.Game.World.Meshing;
using Xunit;

namespace MinecraftPT.Tests.World;

public class BlockTransparencyTests
{
    [Fact]
    public void BlockTransparency_EnumContainsExpectedValues()
    {
        Assert.True(Enum.IsDefined(typeof(BlockTransparency), BlockTransparency.Opaque));
        Assert.True(Enum.IsDefined(typeof(BlockTransparency), BlockTransparency.Transparent));
        Assert.True(Enum.IsDefined(typeof(BlockTransparency), BlockTransparency.Foliage));
        Assert.False(Enum.IsDefined(typeof(BlockTransparency), (byte)99));
    }

    [Fact]
    public void BlockService_ParsesGlassAsTransparent()
    {
        var blockService = new BlockService();
        ref readonly var glassDef = ref blockService.GetBlock(BlockId.Glass);
        Assert.Equal(BlockTransparency.Transparent, glassDef.Transparency);
    }

    [Fact]
    public void BlockService_ParsesOakLeavesAsFoliage()
    {
        var blockService = new BlockService();
        ref readonly var leavesDef = ref blockService.GetBlock(BlockId.OakLeaves);
        Assert.Equal(BlockTransparency.Foliage, leavesDef.Transparency);
    }

    [Fact]
    public void ShouldRenderFace_BetweenSameTransparentBlocks_ReturnsFalse()
    {
        var currentDef = new BlockDefinition(BlockId.Glass, new BlockFaceTextures(1, 1, 1), BlockTransparency.Transparent);
        var neighborDef = new BlockDefinition(BlockId.Glass, new BlockFaceTextures(1, 1, 1), BlockTransparency.Transparent);

        bool result = InvokeShouldRenderFace(currentDef, neighborDef, 0);
        Assert.False(result);
    }

    [Fact]
    public void ShouldRenderFace_BetweenDifferentTransparentBlocks_ReturnsTrue()
    {
        var glass1 = new BlockDefinition(BlockId.Glass, new BlockFaceTextures(1, 1, 1), BlockTransparency.Transparent);
        var glass2 = new BlockDefinition(BlockId.WhiteConcrete, new BlockFaceTextures(2, 2, 2), BlockTransparency.Transparent);

        bool result = InvokeShouldRenderFace(glass1, glass2, 0);
        Assert.True(result);
    }

    [Fact]
    public void ShouldRenderFace_BetweenFoliageBlocks_ReturnsTrue()
    {
        var leaf1 = new BlockDefinition(BlockId.OakLeaves, new BlockFaceTextures(1, 1, 1), BlockTransparency.Foliage);
        var leaf2 = new BlockDefinition(BlockId.OakLeaves, new BlockFaceTextures(1, 1, 1), BlockTransparency.Foliage);

        bool result = InvokeShouldRenderFace(leaf1, leaf2, 0);
        Assert.True(result);
    }

    [Fact]
    public void ShouldRenderFace_TransparentNextToAir_ReturnsTrue()
    {
        var glass = new BlockDefinition(BlockId.Glass, new BlockFaceTextures(1, 1, 1), BlockTransparency.Transparent);
        var air = new BlockDefinition(BlockId.Air, default, BlockTransparency.Transparent);

        bool result = InvokeShouldRenderFace(glass, air, 0);
        Assert.True(result);
    }

    private static bool InvokeShouldRenderFace(in BlockDefinition current, in BlockDefinition neighbor, int neighborFaceIndex)
    {
        var method = typeof(ChunkMesher).GetMethod("ShouldRenderFace", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        // Dummy uninitialized instance to invoke method
        var mesher = (ChunkMesher)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(ChunkMesher));
        object[] parameters = [current, neighbor, neighborFaceIndex];
        return (bool)method.Invoke(mesher, parameters)!;
    }
}
