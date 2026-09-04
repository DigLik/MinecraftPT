using System.Numerics;
using MinecraftPT.Utils.Math;
using Xunit;

namespace MinecraftPT.Tests.Mathematics;

public class ChunkTranslationMatrixTests
{
    [Fact]
    public void HistoricalMatrix_ChunkShiftTranslation_PreservesPreviousClipPosition()
    {
        var oldChunk = new Vector3Int(0, 0, 0);
        var newChunk = new Vector3Int(2, -1, 3);
        var deltaChunk = newChunk - oldChunk;
        var offset = new Vector3(deltaChunk.X * 16.0f, deltaChunk.Y * 16.0f, deltaChunk.Z * 16.0f);

        var camPosOld = new Vector3(8.0f, 8.0f, 8.0f);
        var viewOld = Matrix4x4.CreateLookAt(camPosOld, camPosOld + Vector3.UnitX, Vector3.UnitZ);
        var proj = Matrix4x4.CreatePerspectiveFieldOfView(float.Pi / 2.5f, 16.0f / 9.0f, 0.1f, 3000f);
        var vpOld = viewOld * proj;

        // Shift compensation matrix
        var prevViewProjAdjusted = Matrix4x4.CreateTranslation(offset) * vpOld;

        // Physical point P in world: old chunk coord = (40, -8, 56) => P_old = (40, -8, 56)
        // In new chunk (2, -1, 3) * 16 = (32, -16, 48): P_new = (40 - 32, -8 - (-16), 56 - 48) = (8, 8, 8)
        var pNew = new Vector4(8.0f, 8.0f, 8.0f, 1.0f);
        var clipPrevFromAdjusted = Vector4.Transform(pNew, prevViewProjAdjusted);

        var pOld = new Vector4(40.0f, -8.0f, 56.0f, 1.0f);
        var clipPrevFromOriginal = Vector4.Transform(pOld, vpOld);

        float errorClipX = System.Math.Abs(clipPrevFromAdjusted.X - clipPrevFromOriginal.X);
        float errorClipY = System.Math.Abs(clipPrevFromAdjusted.Y - clipPrevFromOriginal.Y);
        float errorClipZ = System.Math.Abs(clipPrevFromAdjusted.Z - clipPrevFromOriginal.Z);
        float errorClipW = System.Math.Abs(clipPrevFromAdjusted.W - clipPrevFromOriginal.W);

        Assert.True(errorClipX < 1e-4f && errorClipY < 1e-4f && errorClipZ < 1e-4f && errorClipW < 1e-4f,
            $"Chunk shift translation T(deltaChunk * 16) * VP_prev perfectly preserves clip position. Max error: {System.Math.Max(System.Math.Max(errorClipX, errorClipY), System.Math.Max(errorClipZ, errorClipW)):E3}");
    }

    [Fact]
    public void RealisticGameplay_ChunkJumps_ProduceExactClipCoordsAndNdc()
    {
        var rng = new Random(1337);
        const int numRealisticIterations = 5000;
        float maxRealisticClipErr = 0.0f;
        float maxRealisticNdcErr = 0.0f;
        float maxRealisticClipToPrevErr = 0.0f;

        for (int i = 0; i < numRealisticIterations; i++)
        {
            var oldChunk = new Vector3Int(rng.Next(-100, 100), rng.Next(-100, 100), rng.Next(-30, 30));
            var newChunk = oldChunk + new Vector3Int(rng.Next(-3, 4), rng.Next(-3, 4), rng.Next(-3, 4));

            var deltaChunk = newChunk - oldChunk;
            var offset = new Vector3(deltaChunk.X * 16.0f, deltaChunk.Y * 16.0f, deltaChunk.Z * 16.0f);

            var localPosOld = new Vector3((float)rng.NextDouble() * 16.0f, (float)rng.NextDouble() * 16.0f, (float)rng.NextDouble() * 16.0f + 1.62f);
            float yaw = ((float)rng.NextDouble() * 2.0f - 1.0f) * MathF.PI;
            float pitch = ((float)rng.NextDouble() - 0.5f) * (MathF.PI * 0.8f);

            float cx = MathF.Sin(yaw) * MathF.Cos(pitch);
            float cy = MathF.Cos(yaw) * MathF.Cos(pitch);
            float cz = MathF.Sin(pitch);
            var forward = Vector3.Normalize(new Vector3(cx, cy, cz));
            var up = Vector3.UnitZ;

            var viewOld = Matrix4x4.CreateLookAt(localPosOld, localPosOld + forward, up);
            float aspect = 16.0f / 9.0f;
            float fov = MathF.PI / 2.5f;
            var proj = Matrix4x4.CreatePerspectiveFieldOfView(fov, aspect, 0.1f, 3000.0f);
            proj.M33 = -proj.M33 - 1.0f;
            proj.M43 = -proj.M43;
            proj.M22 *= -1;

            var vpOld = viewOld * proj;
            Matrix4x4.Invert(vpOld, out _);

            var localPosNew = new Vector3((float)rng.NextDouble() * 16.0f, (float)rng.NextDouble() * 16.0f, (float)rng.NextDouble() * 16.0f + 1.62f);
            var viewNew = Matrix4x4.CreateLookAt(localPosNew, localPosNew + forward, up);
            var vpNew = viewNew * proj;
            Matrix4x4.Invert(vpNew, out var invVpNew);

            var prevViewProjAdjusted = Matrix4x4.CreateTranslation(offset) * vpOld;

            for (int k = 1; k <= 5; k++)
            {
                float dist = k * 10.0f;
                var hitWorldOld = localPosOld + forward * dist + new Vector3((float)rng.NextDouble() * 2.0f - 1.0f, (float)rng.NextDouble() * 2.0f - 1.0f, (float)rng.NextDouble() * 2.0f - 1.0f);
                var pOld = new Vector4(hitWorldOld, 1.0f);
                var pNew = new Vector4(hitWorldOld - offset, 1.0f);

                var clipExpected = Vector4.Transform(pOld, vpOld);
                var clipAdjusted = Vector4.Transform(pNew, prevViewProjAdjusted);

                float errX = MathF.Abs(clipAdjusted.X - clipExpected.X);
                float errY = MathF.Abs(clipAdjusted.Y - clipExpected.Y);
                float errZ = MathF.Abs(clipAdjusted.Z - clipExpected.Z);
                float errW = MathF.Abs(clipAdjusted.W - clipExpected.W);
                float maxErr = MathF.Max(MathF.Max(errX, errY), MathF.Max(errZ, errW));
                if (maxErr > maxRealisticClipErr) maxRealisticClipErr = maxErr;

                if (clipExpected.W > 0.1f && clipAdjusted.W > 0.1f)
                {
                    var ndcExpected = new Vector3(clipExpected.X / clipExpected.W, clipExpected.Y / clipExpected.W, clipExpected.Z / clipExpected.W);
                    var ndcAdjusted = new Vector3(clipAdjusted.X / clipAdjusted.W, clipAdjusted.Y / clipAdjusted.W, clipAdjusted.Z / clipAdjusted.W);
                    float ndcErr = Vector3.Distance(ndcExpected, ndcAdjusted);
                    if (ndcErr > maxRealisticNdcErr) maxRealisticNdcErr = ndcErr;
                }

                var clipToPrev = invVpNew * prevViewProjAdjusted;
                var clipCurrent = Vector4.Transform(pNew, vpNew);
                var clipPrevFromClipToPrev = Vector4.Transform(clipCurrent, clipToPrev);

                float ctpErr = MathF.Max(MathF.Max(MathF.Abs(clipPrevFromClipToPrev.X - clipExpected.X), MathF.Abs(clipPrevFromClipToPrev.Y - clipExpected.Y)),
                                         MathF.Max(MathF.Abs(clipPrevFromClipToPrev.Z - clipExpected.Z), MathF.Abs(clipPrevFromClipToPrev.W - clipExpected.W)));
                if (ctpErr > maxRealisticClipToPrevErr) maxRealisticClipToPrevErr = ctpErr;
            }
        }

        Assert.True(maxRealisticClipErr < 1e-4f, $"Max clip error: {maxRealisticClipErr:E3}");
        Assert.True(maxRealisticNdcErr < 1e-4f, $"Max NDC error: {maxRealisticNdcErr:E3}");
        Assert.True(maxRealisticClipToPrevErr < 1e-4f, $"Max ClipToPrev error: {maxRealisticClipToPrevErr:E3}");
    }

    [Fact]
    public void LargeChunkJumps_MatchMachinePrecision()
    {
        var rng = new Random(1337);
        const int numLargeIterations = 5000;
        float maxRelativeErr = 0.0f;

        for (int i = 0; i < numLargeIterations; i++)
        {
            var oldChunk = new Vector3Int(rng.Next(-10000, 10000), rng.Next(-10000, 10000), rng.Next(-10000, 10000));
            var newChunk = new Vector3Int(rng.Next(-10000, 10000), rng.Next(-10000, 10000), rng.Next(-10000, 10000));
            var deltaChunk = newChunk - oldChunk;
            var offset = new Vector3(deltaChunk.X * 16.0f, deltaChunk.Y * 16.0f, deltaChunk.Z * 16.0f);

            var localPosOld = new Vector3((float)rng.NextDouble() * 16.0f, (float)rng.NextDouble() * 16.0f, (float)rng.NextDouble() * 16.0f);
            var forward = Vector3.Normalize(new Vector3((float)rng.NextDouble() * 2 - 1, (float)rng.NextDouble() * 2 - 1, (float)rng.NextDouble() * 2 - 1));
            var viewOld = Matrix4x4.CreateLookAt(localPosOld, localPosOld + forward, Vector3.UnitZ);
            var proj = Matrix4x4.CreatePerspectiveFieldOfView(float.Pi / 2.5f, 16.0f / 9.0f, 0.1f, 3000f);
            var vpOld = viewOld * proj;
            var prevViewProjAdjusted = Matrix4x4.CreateTranslation(offset) * vpOld;

            var hitWorldOld = localPosOld + forward * 20.0f;
            var pOld = new Vector4(hitWorldOld, 1.0f);
            var pNew = new Vector4(hitWorldOld - offset, 1.0f);

            var clipExpected = Vector4.Transform(pOld, vpOld);
            var clipAdjusted = Vector4.Transform(pNew, prevViewProjAdjusted);

            float absErr = MathF.Max(MathF.Max(MathF.Abs(clipAdjusted.X - clipExpected.X), MathF.Abs(clipAdjusted.Y - clipExpected.Y)),
                                     MathF.Max(MathF.Abs(clipAdjusted.Z - clipExpected.Z), MathF.Abs(clipAdjusted.W - clipExpected.W)));
            float offsetMagnitude = offset.Length() + 1.0f;
            float relErr = absErr / offsetMagnitude;
            if (relErr > maxRelativeErr) maxRelativeErr = relErr;
        }

        Assert.True(maxRelativeErr < 1e-5f, $"Max relative error: {maxRelativeErr:E3}");
    }

    [Fact]
    public void BoundaryEdgeCases_PassExactly()
    {
        Vector3Int[] testJumps = [
            new(0, 0, 0),
            new(1, 0, 0),
            new(-1, 0, 0),
            new(0, 1, 0),
            new(0, -1, 0),
            new(0, 0, 1),
            new(0, 0, -1),
            new(1000, -500, 750),
            new(-1000, 500, -750)
        ];

        foreach (var dChunk in testJumps)
        {
            var offset = new Vector3(dChunk.X * 16.0f, dChunk.Y * 16.0f, dChunk.Z * 16.0f);
            var lookAt = Matrix4x4.CreateLookAt(new Vector3(4, 5, 6), new Vector3(4, 5, 7), Vector3.UnitY);
            var proj = Matrix4x4.CreatePerspectiveFieldOfView(float.Pi / 3.0f, 1.777f, 0.1f, 1000f);
            var vp = lookAt * proj;
            var vpAdj = Matrix4x4.CreateTranslation(offset) * vp;

            var pWorld = new Vector3(12.3f, 45.6f, 78.9f);
            var pOld = new Vector4(pWorld, 1.0f);
            var pNew = new Vector4(pWorld - offset, 1.0f);

            var cOld = Vector4.Transform(pOld, vp);
            var cAdj = Vector4.Transform(pNew, vpAdj);

            float err = Vector4.Distance(cOld, cAdj);
            float relErr = err / (offset.Length() + 1.0f);
            Assert.True(relErr <= 1e-5f, $"Edge case failed for jump {dChunk}: relErr={relErr:E3}");
        }
    }

    [Fact]
    public void ContinuousMultiFrameTrajectory_MaintainsHistoryWithoutDrift()
    {
        var curChunk = new Vector3Int(0, 0, 0);
        var curLocal = new Vector3(15.5f, 8.0f, 8.0f);
        var curProj = Matrix4x4.CreatePerspectiveFieldOfView(float.Pi / 2.5f, 16.0f / 9.0f, 0.1f, 3000f);
        curProj.M33 = -curProj.M33 - 1.0f;
        curProj.M43 = -curProj.M43;
        curProj.M22 *= -1;

        var curVp = Matrix4x4.CreateLookAt(curLocal, curLocal + Vector3.UnitX, Vector3.UnitZ) * curProj;
        var prevVp = curVp;
        var prevChunk = curChunk;

        for (int f = 0; f < 50; f++)
        {
            curLocal.X += 0.6f;
            if (curLocal.X >= 16.0f)
            {
                curLocal.X -= 16.0f;
                curChunk = new Vector3Int(curChunk.X + 1, curChunk.Y, curChunk.Z);
            }

            var nextVp = Matrix4x4.CreateLookAt(curLocal, curLocal + Vector3.UnitX, Vector3.UnitZ) * curProj;

            var dC = curChunk - prevChunk;
            var dOff = new Vector3(dC.X * 16.0f, dC.Y * 16.0f, dC.Z * 16.0f);
            var adjPrev = Matrix4x4.CreateTranslation(dOff) * prevVp;

            var fixedVoxelWorld = new Vector3(20.0f, 8.0f, 8.0f);
            var ptPrev = new Vector4(fixedVoxelWorld.X - prevChunk.X * 16.0f, fixedVoxelWorld.Y - prevChunk.Y * 16.0f, fixedVoxelWorld.Z - prevChunk.Z * 16.0f, 1.0f);
            var ptCur = new Vector4(fixedVoxelWorld.X - curChunk.X * 16.0f, fixedVoxelWorld.Y - curChunk.Y * 16.0f, fixedVoxelWorld.Z - curChunk.Z * 16.0f, 1.0f);

            var clipFromPrev = Vector4.Transform(ptPrev, prevVp);
            var clipFromAdj = Vector4.Transform(ptCur, adjPrev);

            float dist = Vector4.Distance(clipFromPrev, clipFromAdj);
            Assert.True(dist <= 1e-4f, $"Frame {f} drift detected: dist={dist:E3}");

            prevVp = nextVp;
            prevChunk = curChunk;
        }
    }
}
