using Xunit;
using MinecraftPT.Streamline;

namespace MinecraftPT.Tests.Streamline;

[Collection("Streamline")]
public unsafe class ReflexAndPclTests
{
    public ReflexAndPclTests()
    {
        StreamlineMockHelper.EnsureMockFunctionPointers();
    }

    [Fact]
    public void ReflexAndPcl_TimelineExecutionOrder_MatchesStrictEngineSequence()
    {
        StreamlineMockHelper.TimelineEvents.Clear();

        var token = new FrameToken();
        FrameToken* pToken = &token;
        var orderLog = new List<string>();

        // Step 1: StartFrame() -> Reflex Sleep
        ReflexAPI.Sleep(pToken);
        orderLog.Add("ReflexSleep");

        // Step 2: SetSimulationStart()
        PclAPI.SetMarker(PCLMarker.eSimulationStart, pToken);
        orderLog.Add("Marker:SimulationStart");

        // Step 3: Simulation work
        orderLog.Add("SimulateCPUWork");

        // Step 4: SetSimulationEnd()
        PclAPI.SetMarker(PCLMarker.eSimulationEnd, pToken);
        orderLog.Add("Marker:SimulationEnd");

        // Step 5: UpdateStreamlineFrameTokenAndReflex (SetCameraData)
        var vp = new ViewportHandle(1);
        var cam = ReflexCameraData.Create();
        ReflexAPI.SetCameraData(in vp, pToken, in cam);
        orderLog.Add("ReflexSetCameraData");

        // Step 6: RecordCommandBuffer -> eRenderSubmitStart
        PclAPI.SetMarker(PCLMarker.eRenderSubmitStart, pToken);
        orderLog.Add("Marker:RenderSubmitStart");

        // Step 7: Command recording
        orderLog.Add("RecordGpuCommands");

        // Step 8: SubmitAndPresent -> eRenderSubmitEnd
        PclAPI.SetMarker(PCLMarker.eRenderSubmitEnd, pToken);
        orderLog.Add("Marker:RenderSubmitEnd");

        // Step 9: QueueSubmit
        orderLog.Add("QueueSubmit");

        // Step 10: ePresentStart
        PclAPI.SetMarker(PCLMarker.ePresentStart, pToken);
        orderLog.Add("Marker:PresentStart");

        // Step 11: vkQueuePresentKHR
        orderLog.Add("QueuePresent");

        // Step 12: ePresentEnd
        PclAPI.SetMarker(PCLMarker.ePresentEnd, pToken);
        orderLog.Add("Marker:PresentEnd");

        int idxSleep = orderLog.IndexOf("ReflexSleep");
        int idxSimStart = orderLog.IndexOf("Marker:SimulationStart");
        int idxSimEnd = orderLog.IndexOf("Marker:SimulationEnd");
        int idxRenderStart = orderLog.IndexOf("Marker:RenderSubmitStart");
        int idxRenderEnd = orderLog.IndexOf("Marker:RenderSubmitEnd");
        int idxPresentStart = orderLog.IndexOf("Marker:PresentStart");
        int idxPresentEnd = orderLog.IndexOf("Marker:PresentEnd");

        Assert.True(idxSleep < idxSimStart, "ReflexAPI.Sleep occurs BEFORE SimulationStart");
        Assert.True(idxSimStart < idxSimEnd, "SimulationStart occurs BEFORE SimulationEnd");
        Assert.True(idxSimEnd < idxRenderStart, "SimulationEnd occurs BEFORE RenderSubmitStart");
        Assert.True(idxRenderStart < idxRenderEnd, "RenderSubmitStart occurs BEFORE RenderSubmitEnd");
        Assert.True(idxRenderEnd < idxPresentStart, "RenderSubmitEnd occurs BEFORE PresentStart");
        Assert.True(idxPresentStart < idxPresentEnd, "PresentStart occurs BEFORE PresentEnd");

        Assert.Equal(6, StreamlineMockHelper.TimelineEvents.Count);
        Assert.Equal(PCLMarker.eSimulationStart, StreamlineMockHelper.TimelineEvents[0].marker);
        Assert.Equal(PCLMarker.eSimulationEnd, StreamlineMockHelper.TimelineEvents[1].marker);
        Assert.Equal(PCLMarker.eRenderSubmitStart, StreamlineMockHelper.TimelineEvents[2].marker);
        Assert.Equal(PCLMarker.eRenderSubmitEnd, StreamlineMockHelper.TimelineEvents[3].marker);
        Assert.Equal(PCLMarker.ePresentStart, StreamlineMockHelper.TimelineEvents[4].marker);
        Assert.Equal(PCLMarker.ePresentEnd, StreamlineMockHelper.TimelineEvents[5].marker);

        Assert.All(StreamlineMockHelper.TimelineEvents, evt => Assert.Equal((IntPtr)pToken, evt.frameToken));
    }
}
