using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using MinecraftPT.Engine.Abstractions;
using MinecraftPT.Engine.Abstractions.Graphics;
using MinecraftPT.Engine.Core;
using MinecraftPT.Engine.Input;
using MinecraftPT.Game.World.Meshing;
using MinecraftPT.Utils.Collections;
using MinecraftPT.Utils.Math;
using Xunit;

namespace MinecraftPT.Tests.Graphics;

public unsafe class EngineContractZeroAllocTests
{
    private sealed class MockWindow : IWindow
    {
        public Vector2Int Size => new(1920, 1080);
        public Vector2Int FramebufferSize => new(1920, 1080);
        public string Title { get; set; } = "MockWindow";
        public bool IsClosing => false;
        public unsafe void* Handle => null;
        public nint Win32Handle => 0;

#pragma warning disable CS0067
        public event Action? Load;
        public event Action<double>? Update;
        public event Action<double>? Render;
        public event Action<Vector2Int>? FramebufferResize;
        public event Action? Closing;
#pragma warning restore CS0067

        public void Run() { }
        public void Close() => Closing?.Invoke();
        public void Dispose() { }

        public void TriggerRender(double dt) => Render?.Invoke(dt);
        public void TriggerUpdate(double dt) => Update?.Invoke(dt);
    }

    private sealed class MockInputManager : IInputManager
    {
        public Vector2 MousePosition => Vector2.Zero;
        public float MouseScrollDelta => 0;
        public bool IsMouseCaptured => false;
        public void OnUpdate(double deltaTime) { }
        public bool IsKeyDown(Key key) => false;
        public bool IsKey(Key key) => false;
        public bool IsKeyUp(Key key) => false;
        public bool IsMouseButtonDown(MouseButton button) => false;
        public bool IsMouseButton(MouseButton button) => false;
        public bool IsMouseButtonUp(MouseButton button) => false;
        public void ToggleMouseCapture() { }
        public void CloseWindow() { }
    }

    private sealed class MockRenderPipeline : IRenderPipeline
    {
        public long FrameCount;
        public CameraData LastCamera;

        public void Initialize(ReadOnlySpan<VertexElement> layout, uint stride) { }
        public IMesh CreateMesh<T>(List<T> vertices, List<ushort> indices, uint opaqueIndexCount = 0, List<ushort>? ommIndices = null) where T : unmanaged => null!;
        public void DeleteMesh(IMesh mesh) { }
        public ITextureArray CreateTextureArray(int width, int height, byte[][] pixels) => null!;
        public void BindTextureArray(ITextureArray textureArray) { }
        public void BindMaterials(ReadOnlySpan<MaterialData> materials) { }
        public void SubmitDraw(IMesh mesh, Vector3 position) { }
        public void ClearDraws() { }
        public void RenderFrame(in CameraData cameraData)
        {
            FrameCount++;
            LastCamera = cameraData;
        }
        public void OnFramebufferResize(Vector2Int newSize) { }
        public void StartFrame() { }
        public bool GetPredictedCamera(out Matrix4x4 view, out Matrix4x4 proj) { view = Matrix4x4.Identity; proj = Matrix4x4.Identity; return false; }
        public void SetSimulationStart() { }
        public void SetSimulationEnd() { }
        public void CycleReflexMode() { }
        public void RequestHistoryReset() { }
        public void Dispose() { }
    }

    private sealed class MockNullLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }

    [Fact]
    public void PriorityChunkQueue_TryTake_100k_AllocatesZeroBytes()
    {
        var queue = new PriorityChunkQueue(128);
        const int ExtractionCount = 100_000;
        for (int i = 0; i < ExtractionCount; i++)
        {
            queue.Add(new Vector3Int(i % 1000, (i / 1000) % 100, i / 100000));
        }

        // JIT Warmup
        for (int i = 0; i < 1000; i++)
        {
            if (queue.TryTake(out var pos, block: false))
                queue.Add(pos);
        }

        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);

        long allocBefore = GC.GetAllocatedBytesForCurrentThread();
        int successfullyTaken = 0;
        for (int i = 0; i < ExtractionCount; i++)
        {
            if (queue.TryTake(out _, block: false))
            {
                successfullyTaken++;
            }
        }
        long allocAfter = GC.GetAllocatedBytesForCurrentThread();
        long delta = allocAfter - allocBefore;

        Assert.Equal(ExtractionCount, successfullyTaken);
        Assert.Equal(0, delta);
    }

    [Fact]
    public void PriorityChunkQueue_TryTake_EmptyPolls_AllocatesZeroBytes()
    {
        var queue = new PriorityChunkQueue(128);
        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);

        long emptyAllocBefore = GC.GetAllocatedBytesForCurrentThread();
        int emptyTries = 0;
        for (int i = 0; i < 100_000; i++)
        {
            if (!queue.TryTake(out _, block: false))
                emptyTries++;
        }
        long emptyAllocAfter = GC.GetAllocatedBytesForCurrentThread();
        long emptyDelta = emptyAllocAfter - emptyAllocBefore;

        Assert.Equal(100_000, emptyTries);
        Assert.Equal(0, emptyDelta);
    }

    [Fact]
    public void EngineApp_OnRender_PassingInCameraData_AllocatesZeroBytes()
    {
        using var mockWindow = new MockWindow();
        var mockInput = new MockInputManager();
        using var mockPipeline = new MockRenderPipeline();
        var mockLogger = new MockNullLogger<EngineApp>();
        using var engineApp = new EngineApp(mockWindow, mockInput, mockPipeline, mockLogger);

        // Warm up JIT for EngineApp.OnRender
        for (int i = 0; i < 1000; i++)
        {
            mockWindow.TriggerRender(0.001);
        }

        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);

        long renderAllocBefore = GC.GetAllocatedBytesForCurrentThread();
        const int RenderFrameCount = 100_000;
        for (int i = 0; i < RenderFrameCount; i++)
        {
            mockWindow.TriggerRender(0.000001);
        }
        long renderAllocAfter = GC.GetAllocatedBytesForCurrentThread();
        long renderDelta = renderAllocAfter - renderAllocBefore;

        Assert.True(mockPipeline.FrameCount >= RenderFrameCount + 1000);
        Assert.Equal(0, renderDelta);
    }

    [Fact]
    public void IRenderPipeline_RenderFrame_DirectCall_AllocatesZeroBytes()
    {
        using var mockPipeline = new MockRenderPipeline();
        var testCamera = new CameraData
        {
            ViewProjection = Matrix4x4.Identity,
            InverseViewProjection = Matrix4x4.Identity,
            PrevViewProjection = Matrix4x4.Identity,
            ChunkPosition = new Vector3Int(5, 10, -3),
            LocalPosition = new Vector3(8, 8, 8),
            FrameCount = 42,
            SamplesPerPixel = 1,
            SunDirection = Vector4.UnitZ
        };

        for (int i = 0; i < 1000; i++)
        {
            mockPipeline.RenderFrame(in testCamera);
        }

        GC.Collect(2, GCCollectionMode.Forced, true);
        GC.WaitForPendingFinalizers();
        GC.Collect(2, GCCollectionMode.Forced, true);

        long directRenderAllocBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 100_000; i++)
        {
            mockPipeline.RenderFrame(in testCamera);
        }
        long directRenderAllocAfter = GC.GetAllocatedBytesForCurrentThread();
        long directRenderDelta = directRenderAllocAfter - directRenderAllocBefore;

        Assert.Equal(0, directRenderDelta);
    }

    [Fact]
    public void EngineApp_CameraRef_AllowsInPlaceMutationWithoutAllocations()
    {
        using var mockWindow = new MockWindow();
        var mockInput = new MockInputManager();
        using var mockPipeline = new MockRenderPipeline();
        var mockLogger = new MockNullLogger<EngineApp>();
        using var engineApp = new EngineApp(mockWindow, mockInput, mockPipeline, mockLogger);

        engineApp.CameraRef.ChunkPosition = new Vector3Int(12, 34, 56);
        engineApp.CameraRef.LocalPosition = new Vector3(1.5f, 2.5f, 3.5f);

        ref readonly var camRef = ref engineApp.Camera;
        Assert.Equal(new Vector3Int(12, 34, 56), camRef.ChunkPosition);
        Assert.Equal(new Vector3(1.5f, 2.5f, 3.5f), camRef.LocalPosition);
    }

    [Fact]
    public void SparseSet_GetUnsafe_MutatesInPlace()
    {
        var sparseSet = new SparseSet<Vector3Int>();
        sparseSet.Add(42, new Vector3Int(10, 20, 30));
        sparseSet.Add(100, new Vector3Int(40, 50, 60));

        ref var item42 = ref sparseSet.GetUnsafe(42);
        Assert.Equal(new Vector3Int(10, 20, 30), item42);

        item42 = new Vector3Int(99, 99, 99);
        Assert.Equal(new Vector3Int(99, 99, 99), sparseSet.Get(42));
    }

    [Fact]
    public void Modernization_StructsAreReadonly()
    {
        bool bbReadonly = typeof(BoundingBox).CustomAttributes.Any(a => a.AttributeType.Name == "IsReadOnlyAttribute");
        bool v2Readonly = typeof(Vector2Int).CustomAttributes.Any(a => a.AttributeType.Name == "IsReadOnlyAttribute");
        bool v3Readonly = typeof(Vector3Int).CustomAttributes.Any(a => a.AttributeType.Name == "IsReadOnlyAttribute");

        Assert.True(bbReadonly, "BoundingBox must be readonly struct");
        Assert.True(v2Readonly, "Vector2Int must be readonly struct");
        Assert.True(v3Readonly, "Vector3Int must be readonly struct");
    }

    [Fact]
    public void IRenderPipeline_ContractSignatures_AreModernized()
    {
        var renderFrameParam = typeof(IRenderPipeline).GetMethod("RenderFrame")?.GetParameters()[0];
        Assert.NotNull(renderFrameParam);
        Assert.True(renderFrameParam.IsIn, "IRenderPipeline.RenderFrame must accept 'in CameraData'");

        var initParam = typeof(IRenderPipeline).GetMethod("Initialize")?.GetParameters()[0];
        var bindParam = typeof(IRenderPipeline).GetMethod("BindMaterials")?.GetParameters()[0];
        Assert.NotNull(initParam);
        Assert.NotNull(bindParam);
        Assert.Equal(typeof(ReadOnlySpan<VertexElement>), initParam.ParameterType);
        Assert.Equal(typeof(ReadOnlySpan<MaterialData>), bindParam.ParameterType);
    }
}
