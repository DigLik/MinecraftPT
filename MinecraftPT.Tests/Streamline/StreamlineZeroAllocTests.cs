using Xunit;
using MinecraftPT.Streamline;

namespace MinecraftPT.Tests.Streamline;

[Collection("Streamline")]
public unsafe class StreamlineZeroAllocTests
{
    public StreamlineZeroAllocTests()
    {
        StreamlineMockHelper.EnsureMockFunctionPointers();
    }

    [Fact]
    public void ReflexAndPcl_ZeroAlloc_100kCalls()
    {
        var dummyToken = new FrameToken();
        FrameToken* pDummyToken = &dummyToken;
        var viewport = new ViewportHandle(1);

        // Warm up JIT
        for (int i = 0; i < 1000; i++)
        {
            ReflexAPI.Sleep(pDummyToken);
            ReflexAPI.Sleep(in dummyToken);
            PclAPI.SetMarker(PCLMarker.eSimulationStart, pDummyToken);
            PclAPI.SetMarker(PCLMarker.eSimulationStart, in dummyToken);
            var dlssOpt = DLSSOptions.Create();
            DlssAPI.SetOptions(in viewport, in dlssOpt);
            DlssAPI.GetOptimalSettings(in dlssOpt, out _);
            var dlssdOpt = DLSSDOptions.Create();
            DlssdAPI.SetOptions(in viewport, in dlssdOpt);
            DlssdAPI.GetOptimalSettings(in dlssdOpt, out _);
        }

        const int Iterations = 50_000;

        StreamlineMockHelper.LogTimeline = false;
        try
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long allocBefore = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < Iterations; i++)
            {
                ReflexAPI.Sleep(pDummyToken);
                ReflexAPI.Sleep(in dummyToken);
                PclAPI.SetMarker(PCLMarker.eSimulationStart, pDummyToken);
                PclAPI.SetMarker(PCLMarker.eSimulationStart, in dummyToken);
            }
            long allocAfter = GC.GetAllocatedBytesForCurrentThread();
            long delta = allocAfter - allocBefore;

            Assert.Equal(0, delta);
        }
        finally
        {
            StreamlineMockHelper.LogTimeline = true;
        }
    }
}
