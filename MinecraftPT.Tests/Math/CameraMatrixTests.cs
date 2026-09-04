using System.Numerics;
using MinecraftPT.Streamline;
using SlBoolean = MinecraftPT.Streamline.Boolean;
using Xunit;

namespace MinecraftPT.Tests.Mathematics;

public unsafe class CameraMatrixTests
{
    [Fact]
    public void Constants_MvecScale_And_DepthInverted_AreProperlyConfigured()
    {
        var consts = Constants.Create();
        consts.MvecScale = new Vector2(1.0f, 1.0f);
        consts.DepthInverted = SlBoolean.eTrue;
        consts.CameraMotionIncluded = SlBoolean.eTrue;
        consts.MotionVectors3D = SlBoolean.eFalse;
        consts.MotionVectorsJittered = SlBoolean.eFalse;
        consts.MinRelativeLinearDepthObjectSeparation = 40.0f;

        Assert.Equal(1.0f, consts.MvecScale.X);
        Assert.Equal(1.0f, consts.MvecScale.Y);
        Assert.Equal(SlBoolean.eTrue, consts.DepthInverted);
        Assert.Equal(40.0f, consts.MinRelativeLinearDepthObjectSeparation);
    }

    [Fact]
    public void Constants_PrevClipToClip_IsExactInverseOfClipToPrevClip()
    {
        var camPos1 = new Vector3(10, 20, 30);
        var camPos2 = new Vector3(12, 21, 31);
        var view1 = Matrix4x4.CreateLookAt(camPos1, camPos1 + Vector3.UnitZ, Vector3.UnitY);
        var view2 = Matrix4x4.CreateLookAt(camPos2, camPos2 + Vector3.UnitZ, Vector3.UnitY);
        var proj = Matrix4x4.CreatePerspectiveFieldOfView(float.Pi / 2.5f, 16.0f / 9.0f, 0.1f, 3000f);

        var vp1 = view1 * proj;
        var vp2 = view2 * proj;
        Matrix4x4.Invert(vp1, out var invVp1);

        var clipToPrevClip = invVp1 * vp2;
        Matrix4x4.Invert(clipToPrevClip, out var prevClipToClip);
        var product = clipToPrevClip * prevClipToClip;

        bool isIdentity = System.Math.Abs(product.M11 - 1f) < 1e-4f &&
                          System.Math.Abs(product.M22 - 1f) < 1e-4f &&
                          System.Math.Abs(product.M33 - 1f) < 1e-4f &&
                          System.Math.Abs(product.M44 - 1f) < 1e-4f;

        Assert.True(isIdentity, "PrevClipToClip must be exact inverse of ClipToPrevClip");
    }
}
