using MinecraftPT.Utils.Noise;

using Silk.NET.Vulkan;

namespace MinecraftPT.Graphics.Vulkan.Core;

/// <summary>
/// Генератор и менеджер текстурного массива PMJ02BN (Progressive Multi-Jittered (0,2) Sample Sequences with Blue Noise).
/// Предоставляет стратифицированные сэмплы с низким уровнем дисперсии и свойствами синего шума для различных фаз трассировки лучей.
/// </summary>
public unsafe class Pmj02bnTexture : IDisposable
{
    private readonly VulkanDevice _device;

    public Image Image;
    public DeviceMemory ImageMemory;
    public ImageView ImageView;
    public Sampler Sampler;

    public const int TileWidth = 64;
    public const int TileHeight = 64;
    public const int NumSamples = 64;
    public const int NumLayers = 128; // 64 слоя для BRDF (Diffuse + Specular) и 64 слоя для Света (Sun Shadow + Secondary)

    public Pmj02bnTexture(VulkanDevice device)
    {
        _device = device;
        CreateTexture();
    }

    private void CreateTexture()
    {
        byte[] data = GeneratePmj02bnData();
        ulong totalSize = (ulong)data.Length;

        VulkanBuffer stagingBuffer = new(_device, totalSize, BufferUsageFlags.TransferSrcBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

        fixed (byte* pData = data)
        {
            System.Buffer.MemoryCopy(pData, (byte*)stagingBuffer.MappedMemory, totalSize, totalSize);
        }

        ImageCreateInfo imageInfo = new()
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Extent = new Extent3D(TileWidth, TileHeight, 1),
            MipLevels = 1,
            ArrayLayers = NumLayers,
            Format = Format.R16G16B16A16Unorm,
            Tiling = ImageTiling.Optimal,
            InitialLayout = ImageLayout.Undefined,
            Usage = ImageUsageFlags.TransferDstBit | ImageUsageFlags.SampledBit,
            SharingMode = SharingMode.Exclusive,
            Samples = SampleCountFlags.Count1Bit
        };

        if (_device.Vk.CreateImage(_device.Device, in imageInfo, null, out Image) != Result.Success)
            throw new Exception("Failed to create PMJ02BN image!");

        _device.Vk.GetImageMemoryRequirements(_device.Device, Image, out var memRequirements);

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memRequirements.Size,
            MemoryTypeIndex = _device.FindMemoryType(memRequirements.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit)
        };

        if (_device.Vk.AllocateMemory(_device.Device, in allocInfo, null, out ImageMemory) != Result.Success)
            throw new Exception("Failed to allocate PMJ02BN image memory!");

        _device.Vk.BindImageMemory(_device.Device, Image, ImageMemory, 0);

        TransitionImageLayout(ImageLayout.Undefined, ImageLayout.TransferDstOptimal, NumLayers);
        CopyBufferToImage(stagingBuffer.Buffer, TileWidth, TileHeight, NumLayers);
        TransitionImageLayout(ImageLayout.TransferDstOptimal, ImageLayout.ShaderReadOnlyOptimal, NumLayers);

        stagingBuffer.Dispose();

        ImageViewCreateInfo viewInfo = new()
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = Image,
            ViewType = ImageViewType.Type2DArray,
            Format = Format.R16G16B16A16Unorm,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, NumLayers)
        };

        if (_device.Vk.CreateImageView(_device.Device, in viewInfo, null, out ImageView) != Result.Success)
            throw new Exception("Failed to create PMJ02BN image view!");

        SamplerCreateInfo samplerInfo = new()
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Nearest,
            MinFilter = Filter.Nearest,
            AddressModeU = SamplerAddressMode.Repeat,
            AddressModeV = SamplerAddressMode.Repeat,
            AddressModeW = SamplerAddressMode.Repeat,
            AnisotropyEnable = Vk.False,
            BorderColor = BorderColor.IntOpaqueBlack,
            UnnormalizedCoordinates = Vk.False,
            CompareEnable = Vk.False,
            CompareOp = CompareOp.Always,
            MipmapMode = SamplerMipmapMode.Nearest,
            MipLodBias = 0.0f,
            MinLod = 0.0f,
            MaxLod = 0.0f
        };

        if (_device.Vk.CreateSampler(_device.Device, in samplerInfo, null, out Sampler) != Result.Success)
            throw new Exception("Failed to create PMJ02BN sampler!");
    }

    private static byte[] GeneratePmj02bnData()
    {
        // 64x64 пикселей x 128 слоев x 4 канала (R16G16B16A16 = 8 байт на пиксель)
        int bytesPerPixel = 8;
        int layerSize = TileWidth * TileHeight * bytesPerPixel;
        byte[] buffer = new byte[layerSize * NumLayers];

        // 1. Генерация базовой PMJ02 последовательности для 64 сэмплов (2D точки)
        var basePmj0 = Pmj02bnGenerator.GeneratePmj02Sequence(NumSamples, 0x12345678);
        var basePmj1 = Pmj02bnGenerator.GeneratePmj02Sequence(NumSamples, 0x87654321);
        var basePmj2 = Pmj02bnGenerator.GeneratePmj02Sequence(NumSamples, 0xABCDEF01);
        var basePmj3 = Pmj02bnGenerator.GeneratePmj02Sequence(NumSamples, 0x13579BDF);

        // 2. Генерация карты синего шума (Void and Cluster dither array) для 64x64 пикселей
        float[,] blueNoise0 = Pmj02bnGenerator.GenerateBlueNoiseMask(TileWidth, TileHeight, 101);
        float[,] blueNoise1 = Pmj02bnGenerator.GenerateBlueNoiseMask(TileWidth, TileHeight, 202);
        float[,] blueNoise2 = Pmj02bnGenerator.GenerateBlueNoiseMask(TileWidth, TileHeight, 303);
        float[,] blueNoise3 = Pmj02bnGenerator.GenerateBlueNoiseMask(TileWidth, TileHeight, 404);

        fixed (byte* pBuf = buffer)
        {
            ushort* pData = (ushort*)pBuf;

            // Заполнение слоев 0..63: BRDF Diffuse (XY) + BRDF Specular (ZW)
            for (int s = 0; s < NumSamples; s++)
            {
                int layerOffset = s * TileWidth * TileHeight * 4;

                for (int y = 0; y < TileHeight; y++)
                {
                    for (int x = 0; x < TileWidth; x++)
                    {
                        int pixelIndex = layerOffset + (y * TileWidth + x) * 4;

                        // Cranley-Patterson rotation по синему шуму
                        float u0 = (basePmj0[s].X + blueNoise0[x, y]) % 1.0f;
                        float u1 = (basePmj0[s].Y + blueNoise1[x, y]) % 1.0f;
                        float u2 = (basePmj1[s].X + blueNoise2[x, y]) % 1.0f;
                        float u3 = (basePmj1[s].Y + blueNoise3[x, y]) % 1.0f;

                        pData[pixelIndex + 0] = (ushort)Math.Clamp((int)(u0 * 65535.0f + 0.5f), 0, 65535);
                        pData[pixelIndex + 1] = (ushort)Math.Clamp((int)(u1 * 65535.0f + 0.5f), 0, 65535);
                        pData[pixelIndex + 2] = (ushort)Math.Clamp((int)(u2 * 65535.0f + 0.5f), 0, 65535);
                        pData[pixelIndex + 3] = (ushort)Math.Clamp((int)(u3 * 65535.0f + 0.5f), 0, 65535);
                    }
                }
            }

            // Заполнение слоев 64..127: Sun Light / Shadows (XY) + Secondary / AA (ZW)
            for (int s = 0; s < NumSamples; s++)
            {
                int layerOffset = (64 + s) * TileWidth * TileHeight * 4;

                for (int y = 0; y < TileHeight; y++)
                {
                    for (int x = 0; x < TileWidth; x++)
                    {
                        int pixelIndex = layerOffset + (y * TileWidth + x) * 4;

                        float u0 = (basePmj2[s].X + blueNoise2[x, y]) % 1.0f;
                        float u1 = (basePmj2[s].Y + blueNoise3[x, y]) % 1.0f;
                        float u2 = (basePmj3[s].X + blueNoise0[x, y]) % 1.0f;
                        float u3 = (basePmj3[s].Y + blueNoise1[x, y]) % 1.0f;

                        pData[pixelIndex + 0] = (ushort)Math.Clamp((int)(u0 * 65535.0f + 0.5f), 0, 65535);
                        pData[pixelIndex + 1] = (ushort)Math.Clamp((int)(u1 * 65535.0f + 0.5f), 0, 65535);
                        pData[pixelIndex + 2] = (ushort)Math.Clamp((int)(u2 * 65535.0f + 0.5f), 0, 65535);
                        pData[pixelIndex + 3] = (ushort)Math.Clamp((int)(u3 * 65535.0f + 0.5f), 0, 65535);
                    }
                }
            }
        }

        return buffer;
    }

    private void TransitionImageLayout(ImageLayout oldLayout, ImageLayout newLayout, uint layers)
    {
        var cmd = _device.BeginSingleTimeCommands();

        ImageMemoryBarrier barrier = new()
        {
            SType = StructureType.ImageMemoryBarrier,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = Image,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, layers)
        };

        PipelineStageFlags sourceStage;
        PipelineStageFlags destinationStage;

        if (oldLayout == ImageLayout.Undefined && newLayout == ImageLayout.TransferDstOptimal)
        {
            barrier.SrcAccessMask = 0;
            barrier.DstAccessMask = AccessFlags.TransferWriteBit;
            sourceStage = PipelineStageFlags.TopOfPipeBit;
            destinationStage = PipelineStageFlags.TransferBit;
        }
        else if (oldLayout == ImageLayout.TransferDstOptimal && newLayout == ImageLayout.ShaderReadOnlyOptimal)
        {
            barrier.SrcAccessMask = AccessFlags.TransferWriteBit;
            barrier.DstAccessMask = 0;
            sourceStage = PipelineStageFlags.TransferBit;
            destinationStage = PipelineStageFlags.BottomOfPipeBit;
        }
        else
        {
            throw new ArgumentException("Unsupported layout transition!");
        }

        _device.Vk.CmdPipelineBarrier(cmd, sourceStage, destinationStage, 0, 0, null, 0, null, 1, in barrier);
        _device.EndSingleTimeCommands(cmd);
    }

    private void CopyBufferToImage(Silk.NET.Vulkan.Buffer buffer, uint width, uint height, uint layers)
    {
        var cmd = _device.BeginSingleTimeCommands();

        var regions = stackalloc BufferImageCopy[(int)layers];
        ulong layerSize = (ulong)(width * height * 8); // 8 bytes per pixel (R16G16B16A16)

        for (uint i = 0; i < layers; i++)
        {
            regions[i] = new BufferImageCopy
            {
                BufferOffset = layerSize * i,
                BufferRowLength = 0,
                BufferImageHeight = 0,
                ImageSubresource = new ImageSubresourceLayers
                {
                    AspectMask = ImageAspectFlags.ColorBit,
                    MipLevel = 0,
                    BaseArrayLayer = i,
                    LayerCount = 1
                },
                ImageOffset = new Offset3D(0, 0, 0),
                ImageExtent = new Extent3D(width, height, 1)
            };
        }

        _device.Vk.CmdCopyBufferToImage(cmd, buffer, Image, ImageLayout.TransferDstOptimal, layers, regions);
        _device.EndSingleTimeCommands(cmd);
    }

    public void Dispose()
    {
        if (Sampler.Handle != 0) _device.Vk.DestroySampler(_device.Device, Sampler, null);
        if (ImageView.Handle != 0) _device.Vk.DestroyImageView(_device.Device, ImageView, null);
        if (Image.Handle != 0) _device.Vk.DestroyImage(_device.Device, Image, null);
        if (ImageMemory.Handle != 0) _device.Vk.FreeMemory(_device.Device, ImageMemory, null);
    }
}
