using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace MinecraftPT.Game.World.Meshing;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public record struct ChunkVertex(
    float X, float Y, float Z,
    uint PackedData
);

public record struct ChunkMesh(List<ChunkVertex>? Vertices = default, List<ushort>? Indices = default, uint OpaqueIndexCount = 0)
{
    public readonly bool IsEmpty => Vertices == null || Indices == null || Vertices.Count == 0 || Indices.Count == 0;
}