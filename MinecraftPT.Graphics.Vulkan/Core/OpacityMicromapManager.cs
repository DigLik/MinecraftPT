using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using MinecraftPT.Engine.Abstractions.Graphics;

using Silk.NET.Vulkan;

namespace MinecraftPT.Graphics.Vulkan.Core;

public unsafe class OpacityMicromapManager : IDisposable
{
    private readonly VulkanDevice _device;
    private MicromapEXT _micromap;
    private VulkanBuffer? _micromapBuffer;
    private VulkanBuffer? _rawBitsBuffer;
    private VulkanBuffer? _triangleArrayBuffer;
    private bool _isDisposed;

    public MicromapEXT Micromap => _micromap;
    public bool IsValid => _micromap.Handle != 0;

    public OpacityMicromapManager(
        VulkanDevice device,
        int texWidth,
        int texHeight,
        byte[][] texturePixels,
        ReadOnlySpan<MaterialData> materials)
    {
        _device = device;

        if (!_device.IsOpacityMicromapSupported || _device.ExtOpacityMicromap == null)
            return;

        if (materials.Length == 0 || texturePixels.Length == 0)
            return;

        BuildMicromap(texWidth, texHeight, texturePixels, materials);
    }

    private void BuildMicromap(
        int texWidth,
        int texHeight,
        byte[][] texturePixels,
        ReadOnlySpan<MaterialData> materials)
    {
        try
        {
            int totalTriangles = materials.Length * 4;
            byte[] rawBits = new byte[totalTriangles * 32];
            var triangles = new MicromapTriangleEXT[totalTriangles];

            for (int matIdx = 0; matIdx < materials.Length; matIdx++)
            {
                ref readonly var mat = ref materials[matIdx];
                byte[]? pixels = (matIdx < texturePixels.Length) ? texturePixels[matIdx] : null;

                for (int t = 0; t < 4; t++)
                {
                    int triIdx = matIdx * 4 + t;
                    uint dataOffset = (uint)(triIdx * 32);

                    triangles[triIdx] = new MicromapTriangleEXT
                    {
                        DataOffset = dataOffset,
                        SubdivisionLevel = 4,
                        Format = (ushort)OpacityMicromapFormatEXT.Format2StateExt
                    };

                    Span<byte> outMask = new(rawBits, triIdx * 32, 32);

                    if (mat.Type == 1.0f && pixels != null && pixels.Length >= texWidth * texHeight * 4)
                    {
                        // Cutout foliage/leaves
                        BakeTriangleMask(pixels, texWidth, texHeight, t, mat.Opacity, outMask);
                    }
                    else if (mat.Type == 0.0f)
                    {
                        // Fully opaque
                        outMask.Fill(0xFF);
                    }
                    else
                    {
                        // Translucent (glass, water) -> empty mask, special index FullyUnknownOpaque (0xFFFC) is used
                        outMask.Clear();
                    }
                }
            }

            ulong rawBitsSize = (ulong)rawBits.Length;
            ulong triangleArraySize = (ulong)(triangles.Length * sizeof(MicromapTriangleEXT));

            _rawBitsBuffer = new VulkanBuffer(
                _device,
                rawBitsSize,
                BufferUsageFlags.MicromapBuildInputReadOnlyBitExt | BufferUsageFlags.ShaderDeviceAddressBit | BufferUsageFlags.TransferDstBit,
                MemoryPropertyFlags.DeviceLocalBit);

            _triangleArrayBuffer = new VulkanBuffer(
                _device,
                triangleArraySize,
                BufferUsageFlags.MicromapBuildInputReadOnlyBitExt | BufferUsageFlags.ShaderDeviceAddressBit | BufferUsageFlags.TransferDstBit,
                MemoryPropertyFlags.DeviceLocalBit);

            ulong alignedRawSize = (rawBitsSize + 255UL) & ~255UL;
            ulong stagingSize = alignedRawSize + ((triangleArraySize + 255UL) & ~255UL);

            using (var staging = new VulkanBuffer(
                _device,
                stagingSize,
                BufferUsageFlags.TransferSrcBit,
                MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit))
            {
                // Copy data to staging
                rawBits.AsSpan().CopyTo(new Span<byte>(staging.MappedMemory, rawBits.Length));

                fixed (MicromapTriangleEXT* pTriangles = triangles)
                {
                    System.Buffer.MemoryCopy(
                        pTriangles,
                        (byte*)staging.MappedMemory + alignedRawSize,
                        triangleArraySize,
                        triangleArraySize);
                }

                // Transfer to device local
                var transferCmd = _device.BeginSingleTimeCommands();

                BufferCopy2 rawCopy = new() { SType = StructureType.BufferCopy2, SrcOffset = 0, DstOffset = 0, Size = rawBitsSize };
                CopyBufferInfo2 rawCopyInfo = new() { SType = StructureType.CopyBufferInfo2, SrcBuffer = staging.Buffer, DstBuffer = _rawBitsBuffer.Buffer, RegionCount = 1, PRegions = &rawCopy };
                _device.Vk.CmdCopyBuffer2(transferCmd, in rawCopyInfo);

                BufferCopy2 triCopy = new() { SType = StructureType.BufferCopy2, SrcOffset = alignedRawSize, DstOffset = 0, Size = triangleArraySize };
                CopyBufferInfo2 triCopyInfo = new() { SType = StructureType.CopyBufferInfo2, SrcBuffer = staging.Buffer, DstBuffer = _triangleArrayBuffer.Buffer, RegionCount = 1, PRegions = &triCopy };
                _device.Vk.CmdCopyBuffer2(transferCmd, in triCopyInfo);

                MemoryBarrier2 transferBarrier = new()
                {
                    SType = StructureType.MemoryBarrier2,
                    SrcStageMask = PipelineStageFlags2.TransferBit,
                    SrcAccessMask = AccessFlags2.TransferWriteBit,
                    DstStageMask = PipelineStageFlags2.AllCommandsBit,
                    DstAccessMask = AccessFlags2.MemoryReadBit
                };
                DependencyInfo dep1 = new() { SType = StructureType.DependencyInfo, MemoryBarrierCount = 1, PMemoryBarriers = &transferBarrier };
                _device.Vk.CmdPipelineBarrier2(transferCmd, in dep1);

                _device.EndSingleTimeCommands(transferCmd);
            }

            MicromapUsageEXT usage = new()
            {
                Count = (uint)totalTriangles,
                SubdivisionLevel = 4,
                Format = (uint)OpacityMicromapFormatEXT.Format2StateExt
            };

            MicromapBuildInfoEXT buildInfo = new()
            {
                SType = StructureType.MicromapBuildInfoExt,
                Type = MicromapTypeEXT.OpacityMicromapExt,
                Flags = BuildMicromapFlagsEXT.PreferFastTraceBitExt,
                Mode = BuildMicromapModeEXT.BuildExt,
                UsageCountsCount = 1,
                PUsageCounts = &usage,
                Data = new DeviceOrHostAddressConstKHR { DeviceAddress = _rawBitsBuffer.DeviceAddress },
                TriangleArray = new DeviceOrHostAddressConstKHR { DeviceAddress = _triangleArrayBuffer.DeviceAddress },
                TriangleArrayStride = (ulong)sizeof(MicromapTriangleEXT)
            };

            _device.ExtOpacityMicromap!.GetMicromapBuildSizes(_device.Device, AccelerationStructureBuildTypeKHR.DeviceKhr, in buildInfo, out var sizeInfo);

            _micromapBuffer = new VulkanBuffer(
                _device,
                Math.Max(sizeInfo.MicromapSize, 256UL),
                BufferUsageFlags.MicromapStorageBitExt | BufferUsageFlags.ShaderDeviceAddressBit,
                MemoryPropertyFlags.DeviceLocalBit);

            MicromapCreateInfoEXT createInfo = new()
            {
                SType = StructureType.MicromapCreateInfoExt,
                Type = MicromapTypeEXT.OpacityMicromapExt,
                Buffer = _micromapBuffer.Buffer,
                Offset = 0,
                Size = sizeInfo.MicromapSize
            };

            if (_device.ExtOpacityMicromap.CreateMicromap(_device.Device, in createInfo, null, out _micromap) != Result.Success)
            {
                Console.WriteLine("[OpacityMicromap Error] Failed to create VkMicromapEXT!");
                return;
            }

            using var scratch = new VulkanBuffer(
                _device,
                Math.Max(sizeInfo.BuildScratchSize, 256UL),
                BufferUsageFlags.StorageBufferBit | BufferUsageFlags.ShaderDeviceAddressBit,
                MemoryPropertyFlags.DeviceLocalBit);

            buildInfo.DstMicromap = _micromap;
            buildInfo.ScratchData = new DeviceOrHostAddressKHR { DeviceAddress = scratch.DeviceAddress };

            CommandPoolCreateInfo computePoolInfo = new()
            {
                SType = StructureType.CommandPoolCreateInfo,
                Flags = CommandPoolCreateFlags.TransientBit,
                QueueFamilyIndex = _device.ComputeFamilyIndex
            };
            _device.Vk.CreateCommandPool(_device.Device, in computePoolInfo, null, out var computePool);

            CommandBufferAllocateInfo allocInfo = new()
            {
                SType = StructureType.CommandBufferAllocateInfo,
                CommandPool = computePool,
                Level = CommandBufferLevel.Primary,
                CommandBufferCount = 1
            };
            _device.Vk.AllocateCommandBuffers(_device.Device, in allocInfo, out var cmd);

            CommandBufferBeginInfo beginInfo = new() { SType = StructureType.CommandBufferBeginInfo, Flags = CommandBufferUsageFlags.OneTimeSubmitBit };
            _device.Vk.BeginCommandBuffer(cmd, in beginInfo);

            _device.ExtOpacityMicromap.CmdBuildMicromap(cmd, stackalloc[] { buildInfo });

            MemoryBarrier2 buildBarrier = new()
            {
                SType = StructureType.MemoryBarrier2,
                SrcStageMask = PipelineStageFlags2.MicromapBuildBitExt,
                SrcAccessMask = AccessFlags2.MicromapWriteBitExt,
                DstStageMask = PipelineStageFlags2.AccelerationStructureBuildBitKhr,
                DstAccessMask = AccessFlags2.MicromapReadBitExt
            };
            DependencyInfo dep2 = new() { SType = StructureType.DependencyInfo, MemoryBarrierCount = 1, PMemoryBarriers = &buildBarrier };
            _device.Vk.CmdPipelineBarrier2(cmd, in dep2);

            _device.Vk.EndCommandBuffer(cmd);

            CommandBufferSubmitInfo cmdSubmitInfo = new() { SType = StructureType.CommandBufferSubmitInfo, CommandBuffer = cmd };
            SubmitInfo2 submit = new() { SType = StructureType.SubmitInfo2, CommandBufferInfoCount = 1, PCommandBufferInfos = &cmdSubmitInfo };
            lock (_device.ComputeQueueLock)
            {
                _device.Vk.QueueSubmit2(_device.ComputeQueue, 1, in submit, default);
                _device.Vk.QueueWaitIdle(_device.ComputeQueue);
            }

            _device.Vk.DestroyCommandPool(_device.Device, computePool, null);

            Console.WriteLine($"[OpacityMicromap] Successfully built GPU micromap ({totalTriangles} triangles, {sizeInfo.MicromapSize} bytes, level 4, 2-state) on NVIDIA RTX 5080!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[OpacityMicromap Error] Build failed: {ex}");
        }
    }

    public static void BakeTriangleMask(
        ReadOnlySpan<byte> pixels,
        int texWidth,
        int texHeight,
        int triangleType,
        float opacity,
        Span<byte> outMask)
    {
        outMask.Clear();
        int N = 16; // 1 << 4

        fixed (byte* pPixels = pixels)
        fixed (byte* pOutMask = outMask)
        {
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N - i; j++)
                {
                    ProcessCentroid((i + 1f / 3f) / N, (j + 1f / 3f) / N, pPixels, texWidth, texHeight, triangleType, opacity, pOutMask);
                }
            }

            for (int i = 0; i < N - 1; i++)
            {
                for (int j = 0; j < N - 1 - i; j++)
                {
                    ProcessCentroid((i + 2f / 3f) / N, (j + 2f / 3f) / N, pPixels, texWidth, texHeight, triangleType, opacity, pOutMask);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ProcessCentroid(
        float uc, float vc,
        byte* pPixels,
        int texWidth,
        int texHeight,
        int triangleType,
        float opacity,
        byte* pOutMask)
    {
        uint curveIdx = BarycentricsToSpaceFillingCurveIndex(uc, vc, 4);

        float uTex, vTex;
        switch (triangleType)
        {
            case 0: // Front 1: (0,0), (0,1), (1,1) -> U = v, V = u + v
                uTex = vc;
                vTex = uc + vc;
                break;
            case 1: // Front 2: (1,1), (1,0), (0,0) -> U = 1 - v, V = 1 - u - v
                uTex = 1.0f - vc;
                vTex = 1.0f - uc - vc;
                break;
            case 2: // Back 1: (0,0), (1,0), (1,1) -> U = u + v, V = v
                uTex = uc + vc;
                vTex = vc;
                break;
            case 3: // Back 2: (1,1), (0,1), (0,0) -> U = 1 - u - v, V = 1 - v
                uTex = 1.0f - uc - vc;
                vTex = 1.0f - vc;
                break;
            default:
                uTex = 0; vTex = 0;
                break;
        }

        int px = Math.Clamp((int)(uTex * texWidth), 0, texWidth - 1);
        int py = Math.Clamp((int)(vTex * texHeight), 0, texHeight - 1);
        int pixelOffset = (py * texWidth + px) * 4;
        byte a = pPixels[pixelOffset + 3];

        if (a * opacity >= 128f)
        {
            int byteIdx = (int)(curveIdx >> 3);
            int bitIdx = (int)(curveIdx & 7);
            pOutMask[byteIdx] |= (byte)(1 << bitIdx);
        }
    }

    public static uint BarycentricsToSpaceFillingCurveIndex(float u, float v, uint level)
    {
        u = Math.Clamp(u, 0.0f, 1.0f);
        v = Math.Clamp(v, 0.0f, 1.0f);
        uint iu, iv, iw;
        float fu = u * (1u << (int)level);
        float fv = v * (1u << (int)level);
        iu = (uint)fu;
        iv = (uint)fv;
        float uf = fu - (float)iu;
        float vf = fv - (float)iv;
        if (iu >= (1u << (int)level)) iu = (1u << (int)level) - 1u;
        if (iv >= (1u << (int)level)) iv = (1u << (int)level) - 1u;
        uint iuv = iu + iv;
        if (iuv >= (1u << (int)level)) iu -= iuv - (1u << (int)level) + 1u;
        iw = ~(iu + iv);
        if (uf + vf >= 1.0f && iuv < (1u << (int)level) - 1u) --iw;
        uint b0 = ~(iu ^ iw);
        b0 &= ((1u << (int)level) - 1u);
        uint t = (iu ^ iv) & b0;
        uint f = t;
        f ^= f >> 1;
        f ^= f >> 2;
        f ^= f >> 4;
        f ^= f >> 8;
        uint b1 = ((f ^ iu) & ~b0) | t;

        b0 = (b0 | (b0 << 8)) & 0x00ff00ffu;
        b0 = (b0 | (b0 << 4)) & 0x0f0f0f0fu;
        b0 = (b0 | (b0 << 2)) & 0x33333333u;
        b0 = (b0 | (b0 << 1)) & 0x55555555u;
        b1 = (b1 | (b1 << 8)) & 0x00ff00ffu;
        b1 = (b1 | (b1 << 4)) & 0x0f0f0f0fu;
        b1 = (b1 | (b1 << 2)) & 0x33333333u;
        b1 = (b1 | (b1 << 1)) & 0x55555555u;

        return b0 | (b1 << 1);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        if (_micromap.Handle != 0 && _device.ExtOpacityMicromap != null)
        {
            _device.ExtOpacityMicromap.DestroyMicromap(_device.Device, _micromap, null);
            _micromap = default;
        }

        _micromapBuffer?.Dispose();
        _rawBitsBuffer?.Dispose();
        _triangleArrayBuffer?.Dispose();
    }
}
