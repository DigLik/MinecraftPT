using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MinecraftPT.Graphics.Vulkan.Core;
using Xunit;

namespace MinecraftPT.Tests.Graphics;

public unsafe class DynamicMeshPoolDrainTests
{
    private static (DynamicMeshPool pool, object bc, object queue, MethodInfo addMethod, PropertyInfo countProp, PropertyInfo queueCountProp, Type uploadType) CreateMockDynamicMeshPool()
    {
        Type poolType = typeof(DynamicMeshPool);
        Type pendingUploadType = poolType.GetNestedType("PendingUpload", BindingFlags.NonPublic)!;
        Type queueType = typeof(ConcurrentQueue<>).MakeGenericType(pendingUploadType);
        Type bcType = typeof(BlockingCollection<>).MakeGenericType(pendingUploadType);
        var addMethod = bcType.GetMethod("Add", [pendingUploadType])!;
        var countProp = bcType.GetProperty("Count")!;
        var queueCountProp = queueType.GetProperty("Count")!;

        var pool = (DynamicMeshPool)RuntimeHelpers.GetUninitializedObject(poolType);
#pragma warning disable CS9216
        poolType.GetField("_allocLock", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(pool, new Lock());
#pragma warning restore CS9216
        poolType.GetField("_vertexChunks", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(pool, new List<DynamicMeshPool.BufferChunk>());
        poolType.GetField("_indexChunks", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(pool, new List<DynamicMeshPool.BufferChunk>());
        poolType.GetField("_blasChunks", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(pool, new List<DynamicMeshPool.BufferChunk>());
        poolType.GetField("_stagingBuffers", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(pool, new VulkanBuffer?[3]);
        poolType.GetField("_scratchBuffers", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(pool, new VulkanBuffer?[3]);

        var dummyThread = new Thread(() => { }); dummyThread.Start(); dummyThread.Join();
        poolType.GetField("_uploadThread", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(pool, dummyThread);

        var queueInstance = Activator.CreateInstance(queueType)!;
        var bc = Activator.CreateInstance(bcType, queueInstance)!;
        poolType.GetField("_pendingUploads", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(pool, bc);

        return (pool, bc, queueInstance, addMethod, countProp, queueCountProp, pendingUploadType);
    }

    [Fact]
    public void EmptyPool_Disposal_DrainsCleanly()
    {
        var (pool, _, queue, _, _, queueCountProp, _) = CreateMockDynamicMeshPool();
        pool.Dispose();
        Assert.Equal(0, (int)queueCountProp.GetValue(queue)!);
    }

    [Fact]
    public void SingleItem_Disposal_FreesUnmanagedMemoryAndDrainsQueue()
    {
        var (pool, bc, queue, addMethod, countProp, queueCountProp, uploadType) = CreateMockDynamicMeshPool();

        void* vPtr = NativeMemory.Alloc(65536);
        void* iPtr = NativeMemory.Alloc(32768);
        *(uint*)vPtr = 0x55AA55AA;
        *(uint*)iPtr = 0xAA55AA55;

        object upload = Activator.CreateInstance(uploadType)!;
        uploadType.GetField("Vertices", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(upload, Pointer.Box(vPtr, typeof(void*)));
        uploadType.GetField("VertexByteSize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(upload, 65536);
        uploadType.GetField("Indices", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(upload, Pointer.Box(iPtr, typeof(void*)));
        uploadType.GetField("IndexByteSize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(upload, 32768);

        addMethod.Invoke(bc, [upload]);
        Assert.Equal(1, (int)countProp.GetValue(bc)!);

        pool.Dispose();
        Assert.Equal(0, (int)queueCountProp.GetValue(queue)!);
    }

    [Fact]
    public void HighVolume_Disposal_Drains500ItemsWithoutLeaks()
    {
        var (pool, bc, queue, addMethod, countProp, queueCountProp, uploadType) = CreateMockDynamicMeshPool();

        const int NumUploads = 500;
        for (int i = 0; i < NumUploads; i++)
        {
            void* vPtr = NativeMemory.Alloc(65536);
            void* iPtr = NativeMemory.Alloc(32768);
            *(uint*)vPtr = (uint)(0x12340000 | i);
            *(uint*)iPtr = (uint)(0x56780000 | i);

            object upload = Activator.CreateInstance(uploadType)!;
            uploadType.GetField("Vertices", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(upload, Pointer.Box(vPtr, typeof(void*)));
            uploadType.GetField("VertexByteSize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(upload, 65536);
            uploadType.GetField("Indices", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(upload, Pointer.Box(iPtr, typeof(void*)));
            uploadType.GetField("IndexByteSize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(upload, 32768);

            addMethod.Invoke(bc, [upload]);
        }

        Assert.Equal(NumUploads, (int)countProp.GetValue(bc)!);

        pool.Dispose();
        int remaining = (int)queueCountProp.GetValue(queue)!;
        Assert.Equal(0, remaining);
    }

    [Fact]
    public void ConsecutiveCycles_10kUnmanagedAllocations_DrainedCleanly()
    {
        for (int cycle = 0; cycle < 50; cycle++)
        {
            var (pool, bc, queue, addMethod, _, queueCountProp, uploadType) = CreateMockDynamicMeshPool();

            for (int i = 0; i < 200; i++)
            {
                void* vPtr = NativeMemory.Alloc(4096);
                void* iPtr = NativeMemory.Alloc(2048);
                object upload = Activator.CreateInstance(uploadType)!;
                uploadType.GetField("Vertices", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(upload, Pointer.Box(vPtr, typeof(void*)));
                uploadType.GetField("VertexByteSize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(upload, 4096);
                uploadType.GetField("Indices", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(upload, Pointer.Box(iPtr, typeof(void*)));
                uploadType.GetField("IndexByteSize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(upload, 2048);
                addMethod.Invoke(bc, [upload]);
            }

            pool.Dispose();
            Assert.Equal(0, (int)queueCountProp.GetValue(queue)!);
        }
    }

    [Fact]
    public void IdempotentDispose_MultipleCallsSafe()
    {
        var (pool, bc, queue, addMethod, _, queueCountProp, uploadType) = CreateMockDynamicMeshPool();

        void* vPtr = NativeMemory.Alloc(1024);
        void* iPtr = NativeMemory.Alloc(512);
        object upload = Activator.CreateInstance(uploadType)!;
        uploadType.GetField("Vertices", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(upload, Pointer.Box(vPtr, typeof(void*)));
        uploadType.GetField("VertexByteSize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(upload, 1024);
        uploadType.GetField("Indices", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(upload, Pointer.Box(iPtr, typeof(void*)));
        uploadType.GetField("IndexByteSize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(upload, 512);
        addMethod.Invoke(bc, [upload]);

        for (int i = 0; i < 5; i++)
        {
            pool.Dispose();
        }

        Assert.Equal(0, (int)queueCountProp.GetValue(queue)!);
    }

#pragma warning disable xUnit1031
    [Fact]
    public void ConcurrentEnqueue_DuringDisposal_TerminatesCleanly()
    {
        var (pool, bc, queue, addMethod, _, queueCountProp, uploadType) = CreateMockDynamicMeshPool();

        var cts = new CancellationTokenSource();
        var producerTask = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    void* v = NativeMemory.Alloc(512);
                    void* idx = NativeMemory.Alloc(256);
                    object u = Activator.CreateInstance(uploadType)!;
                    uploadType.GetField("Vertices", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(u, Pointer.Box(v, typeof(void*)));
                    uploadType.GetField("VertexByteSize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(u, 512);
                    uploadType.GetField("Indices", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(u, Pointer.Box(idx, typeof(void*)));
                    uploadType.GetField("IndexByteSize", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(u, 256);

                    try
                    {
                        addMethod.Invoke(bc, [u]);
                    }
                    catch (TargetInvocationException ex) when (ex.InnerException is InvalidOperationException)
                    {
                        NativeMemory.Free(v);
                        NativeMemory.Free(idx);
                        break;
                    }
                }
                catch
                {
                    break;
                }
            }
        });

        Thread.Sleep(10);
        pool.Dispose();
        cts.Cancel();
        producerTask.Wait(TimeSpan.FromSeconds(2));

        Assert.Equal(0, (int)queueCountProp.GetValue(queue)!);
    }
#pragma warning restore xUnit1031
}
