using Xunit;
using MinecraftPT.Streamline;

namespace MinecraftPT.Tests.Streamline;

[Collection("Streamline")]
public unsafe class StreamlineStressTests
{
    public StreamlineStressTests()
    {
        StreamlineMockHelper.EnsureMockFunctionPointers();
    }

    [Fact]
    public void Interposer_NullPointerParameters_SafeHandling()
    {
        FrameToken* nullToken = null;
        var vp = new ViewportHandle(1);

        var resReflexSleepNull = ReflexAPI.Sleep(nullToken);
        Assert.Equal(Result.eErrorNotInitialized, resReflexSleepNull);

        var resPclMarkerNull = PclAPI.SetMarker(PCLMarker.eSimulationStart, nullToken);
        Assert.Equal(Result.eErrorNotInitialized, resPclMarkerNull);

        var resDlssEvalNull = DlssAPI.Evaluate(nullToken, in vp, null);
        Assert.Equal(Result.eErrorInvalidParameter, resDlssEvalNull);

        var resDlssdEvalNull = DlssdAPI.Evaluate(nullToken, in vp, null);
        Assert.Equal(Result.eErrorInvalidParameter, resDlssdEvalNull);
    }
}
