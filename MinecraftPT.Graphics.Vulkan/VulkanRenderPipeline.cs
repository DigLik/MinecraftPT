using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging;

using MinecraftPT.Engine.Abstractions;
using MinecraftPT.Engine.Abstractions.Graphics;
using MinecraftPT.Graphics.Vulkan.Core;
using MinecraftPT.Streamline;
using MinecraftPT.Utils.Math;

using Silk.NET.Vulkan;

using Result = Silk.NET.Vulkan.Result;
using Semaphore = Silk.NET.Vulkan.Semaphore;
using SlBoolean = MinecraftPT.Streamline.Boolean;
using SlResult = MinecraftPT.Streamline.Result;

namespace MinecraftPT.Graphics.Vulkan;

public unsafe partial class VulkanRenderPipeline : IRenderPipeline
{
    private struct DrawCall
    {
        public IMesh Mesh;
        public Vector3 Position;
    }

    public struct InstanceData
    {
        public uint VertexOffset;
        public uint IndexOffset;
        public uint OpaqueIndexCount;
        public uint Pad2;
        public ulong VertexAddress;
        public ulong IndexAddress;
    }

    private const int MaxFramesInFlight = 3;
    private int _currentFrame = 0;

    private readonly VulkanDevice _device;
    private VulkanSwapchain _swapchain;
    private VulkanRayTracingPipeline? _pipeline;
    private readonly ILogger<VulkanRenderPipeline> _logger;

    private CommandPool _commandPool;
    private readonly CommandBuffer[] _commandBuffers = new CommandBuffer[MaxFramesInFlight];

    private readonly Semaphore[] _imageAvailableSemaphores = new Semaphore[MaxFramesInFlight];
    private Semaphore[] _renderFinishedSemaphores = [];
    private readonly Fence[] _inFlightFences = new Fence[MaxFramesInFlight];

    private readonly VulkanBuffer[] _cameraBuffers = new VulkanBuffer[MaxFramesInFlight];
    private DescriptorPool _descriptorPool;
    private readonly DescriptorSet[] _descriptorSets = new DescriptorSet[MaxFramesInFlight];

    private Vector2Int _framebufferSize;
    private bool _framebufferResized = false;

    // Output target image (high-res)
    private Image _storageImage;
    private DeviceMemory _storageImageMemory;
    private ImageView _storageImageView;

    // DLSS / G-buffer resources
    private bool _useDLSS_SR = false;
    private bool _useDLSS_RR = false;
    private bool _useReflex = false;
    private ViewportHandle _slViewport;
    private Vector2Int _renderSize;
    private uint _slFrameIndex = 0;
    private float _currentJitterX = 0f;
    private float _currentJitterY = 0f;

    private FrameToken* _currentFrameToken = null;
    private FrameToken* _prevFrameToken = null;
    private Matrix4x4 _prevWorldToView = Matrix4x4.Identity;
    private Matrix4x4 _prevViewToClip = Matrix4x4.Identity;


    private ReflexMode _reflexMode = ReflexMode.eLowLatencyWithBoost;
    private readonly bool[] _materialsDirty = [true, true, true];
    private readonly bool[] _textureDirty = [true, true, true];
    private Image[] _gBufferImages = [];

    private Image _noisyColorImage;
    private DeviceMemory _noisyColorImageMemory;
    private ImageView _noisyColorImageView;

    private Image _normalImage;
    private DeviceMemory _normalImageMemory;
    private ImageView _normalImageView;

    private Image _albedoImage;
    private DeviceMemory _albedoImageMemory;
    private ImageView _albedoImageView;

    private Image _specularAlbedoImage;
    private DeviceMemory _specularAlbedoImageMemory;
    private ImageView _specularAlbedoImageView;

    private Image _motionVectorsImage;
    private DeviceMemory _motionVectorsImageMemory;
    private ImageView _motionVectorsImageView;

    private Image _depthImage;
    private DeviceMemory _depthImageMemory;
    private ImageView _depthImageView;

    private Image _linearDepthImage;
    private DeviceMemory _linearDepthImageMemory;
    private ImageView _linearDepthImageView;

    private Image _colorBeforeTransparencyImage;
    private DeviceMemory _colorBeforeTransparencyImageMemory;
    private ImageView _colorBeforeTransparencyImageView;

    private Image _specularMotionVectorsImage;
    private DeviceMemory _specularMotionVectorsImageMemory;
    private ImageView _specularMotionVectorsImageView;

    private Image _exposureImage;
    private DeviceMemory _exposureImageMemory;
    private ImageView _exposureImageView;

    private Image _biasColorImage;
    private DeviceMemory _biasColorImageMemory;
    private ImageView _biasColorImageView;

    private Image _specularHitDistanceImage;
    private DeviceMemory _specularHitDistanceImageMemory;
    private ImageView _specularHitDistanceImageView;

    private Image _diffuseHitNoisyImage;
    private DeviceMemory _diffuseHitNoisyImageMemory;
    private ImageView _diffuseHitNoisyImageView;

    private Image _specularHitNoisyImage;
    private DeviceMemory _specularHitNoisyImageMemory;
    private ImageView _specularHitNoisyImageView;

    private Pmj02bnTexture? _pmj02bnTexture;

    private VulkanBuffer? _materialBuffer;
    private Matrix4x4 _lastViewProj;
    private Vector3 _lastLocalPos;
    private Vector3Int _lastChunkPos;
    private uint _frameCount = 1;
    private uint _seed = 0;
    private bool _resetHistoryRequested = true;
    private bool _isDisposed;

    private DrawCall[] _drawCalls = new DrawCall[32768];
    private int _drawCallCount = 0;
    private ITextureArray? _currentTextureArray;
    private byte[][]? _cachedPixels;
    private int _cachedTexWidth;
    private int _cachedTexHeight;
    private MaterialData[]? _cachedMaterials;
    private OpacityMicromapManager? _ommManager;

    private DynamicMeshPool _meshPool = null!;
    private readonly ConcurrentQueue<IMesh> _pendingMeshesToDispose = new();
    private readonly List<IMesh>[] _meshesToDispose = new List<IMesh>[MaxFramesInFlight];

    private readonly VulkanBuffer[] _tlasBuffers = new VulkanBuffer[MaxFramesInFlight];
    private readonly AccelerationStructureKHR[] _tlasHandles = new AccelerationStructureKHR[MaxFramesInFlight];
    private readonly VulkanBuffer[] _instancesBuffers = new VulkanBuffer[MaxFramesInFlight];
    private readonly VulkanBuffer[] _instanceDataBuffers = new VulkanBuffer[MaxFramesInFlight];
    private readonly VulkanBuffer[] _tlasScratchBuffers = new VulkanBuffer[MaxFramesInFlight];

    private readonly int[] _tlasCapacities = new int[MaxFramesInFlight];
    private readonly int[] _tlasInstanceCounts = new int[MaxFramesInFlight];
    private readonly ulong[] _tlasScratchCapacities = new ulong[MaxFramesInFlight];
    private readonly bool[] _tlasNeedsRebuild = new bool[MaxFramesInFlight];
    private readonly bool[] _tlasDescriptorDirty = [true, true, true];

    [LoggerMessage(EventId = 10, Level = Microsoft.Extensions.Logging.LogLevel.Warning, Message = "[Streamline] DLSS SR is not supported: {Result}")]
    private partial void LogDlssSrNotSupported(SlResult result);

    [LoggerMessage(EventId = 11, Level = Microsoft.Extensions.Logging.LogLevel.Warning, Message = "[Streamline] DLSS RR is not supported: {Result}")]
    private partial void LogDlssRrNotSupported(SlResult result);

    [LoggerMessage(EventId = 12, Level = Microsoft.Extensions.Logging.LogLevel.Information, Message = "[Streamline] DLSS SR ({Mode}, Preset: {Preset}) initialized successfully. Output: {OutX}x{OutY}, Render size: {RenderX}x{RenderY}")]
    private partial void LogDlssSrSuccess(DLSSMode mode, DLSSPreset preset, int outX, int outY, int renderX, int renderY);

    [LoggerMessage(EventId = 13, Level = Microsoft.Extensions.Logging.LogLevel.Information, Message = "[Streamline] DLSS RR ({Mode}, Preset: {Preset}) initialized successfully. Output: {OutX}x{OutY}, Render size: {RenderX}x{RenderY}")]
    private partial void LogDlssRrSuccess(DLSSMode mode, DLSSDPreset preset, int outX, int outY, int renderX, int renderY);

    [LoggerMessage(EventId = 14, Level = Microsoft.Extensions.Logging.LogLevel.Information, Message = "[Streamline] Reflex 2 initialized. Mode: LowLatencyWithBoost, status: {Result}")]
    private partial void LogReflexSuccess(SlResult result);

    [LoggerMessage(EventId = 15, Level = Microsoft.Extensions.Logging.LogLevel.Warning, Message = "[Streamline] Reflex is not supported: {Result}")]
    private partial void LogReflexNotSupported(SlResult result);

    [LoggerMessage(EventId = 16, Level = Microsoft.Extensions.Logging.LogLevel.Information, Message = "[Streamline] PCL Stats initialized successfully.")]
    private partial void LogPclSuccess();

    [LoggerMessage(EventId = 17, Level = Microsoft.Extensions.Logging.LogLevel.Warning, Message = "[Streamline] PCL Stats is not supported: {Result}")]
    private partial void LogPclNotSupported(SlResult result);

    [LoggerMessage(EventId = 18, Level = Microsoft.Extensions.Logging.LogLevel.Error, Message = "[Streamline] Failed to set Vulkan info: {Result}")]
    private partial void LogFailedSetVulkanInfo(SlResult result);

    [LoggerMessage(EventId = 19, Level = Microsoft.Extensions.Logging.LogLevel.Error, Message = "[Streamline] Error during Vulkan setup: {Error}")]
    private partial void LogVulkanSetupError(string error);

    [LoggerMessage(EventId = 20, Level = Microsoft.Extensions.Logging.LogLevel.Error, Message = "[Streamline] slReflexSetCameraData failed: {Result}")]
    private partial void LogReflexCameraDataError(SlResult result);

    [LoggerMessage(EventId = 21, Level = Microsoft.Extensions.Logging.LogLevel.Error, Message = "[Streamline] DLSS RR Evaluate failed: {Result}")]
    private partial void LogDlssRrEvalError(int result);

    [LoggerMessage(EventId = 22, Level = Microsoft.Extensions.Logging.LogLevel.Error, Message = "[Streamline] DLSS SR Evaluate failed: {Result}")]
    private partial void LogDlssSrEvalError(int result);

    public VulkanRenderPipeline(IWindow window, ILogger<VulkanRenderPipeline> logger)
    {
        _logger = logger;

        // 2. Now create Vulkan device and swapchain
        _framebufferSize = window.FramebufferSize;
        _device = new VulkanDevice(window.Handle);
        _swapchain = new VulkanSwapchain(_device, _framebufferSize);

        for (int i = 0; i < MaxFramesInFlight; i++)
        {
            _meshesToDispose[i] = [];
            _tlasInstanceCounts[i] = -1;
            _tlasNeedsRebuild[i] = true;
            _tlasDescriptorDirty[i] = true;
        }

        CreateCommandPool();
        CreateCommandBuffers();
        CreateSyncObjects();

        InitializeStreamline();
    }

    private DLSSOptions CreateDlssOptions()
    {
        var dlssOptions = DLSSOptions.Create();
        dlssOptions.Mode = DlssConfiguration.SrMode;
        dlssOptions.DlaaPreset = DlssConfiguration.SrPreset;
        dlssOptions.QualityPreset = DlssConfiguration.SrPreset;
        dlssOptions.PerformancePreset = DlssConfiguration.SrPreset;
        dlssOptions.BalancedPreset = DlssConfiguration.SrPreset;
        dlssOptions.UltraPerformancePreset = DlssConfiguration.SrPreset;
        dlssOptions.UltraQualityPreset = DlssConfiguration.SrPreset;
        dlssOptions.OutputWidth = (uint)_framebufferSize.X;
        dlssOptions.OutputHeight = (uint)_framebufferSize.Y;
        dlssOptions.ColorBuffersHDR = SlBoolean.eTrue;
        dlssOptions.UseAutoExposure = SlBoolean.eTrue;
        return dlssOptions;
    }

    private DLSSDOptions CreateDlssdOptions(Matrix4x4? view = null, Matrix4x4? viewInverse = null)
    {
        var dlssdOptions = DLSSDOptions.Create();
        dlssdOptions.Mode = DlssConfiguration.RrMode;
        dlssdOptions.DlaaPreset = DlssConfiguration.RrPreset;
        dlssdOptions.QualityPreset = DlssConfiguration.RrPreset;
        dlssdOptions.BalancedPreset = DlssConfiguration.RrPreset;
        dlssdOptions.PerformancePreset = DlssConfiguration.RrPreset;
        dlssdOptions.UltraPerformancePreset = DlssConfiguration.RrPreset;
        dlssdOptions.UltraQualityPreset = DlssConfiguration.RrPreset;
        dlssdOptions.OutputWidth = (uint)_framebufferSize.X;
        dlssdOptions.OutputHeight = (uint)_framebufferSize.Y;
        dlssdOptions.ColorBuffersHDR = SlBoolean.eTrue;
        dlssdOptions.NormalRoughnessMode = DlssConfiguration.RrNormalRoughnessMode;
        if (view.HasValue) dlssdOptions.WorldToCameraView = view.Value;
        if (viewInverse.HasValue) dlssdOptions.CameraViewToWorld = viewInverse.Value;
        return dlssdOptions;
    }

    private void InitializeStreamline()
    {
        // 3. Set Vulkan Info and query feature support (since Device is now created)
        try
        {
            var vkInfo = VulkanInfo.Create();
            vkInfo.Device = (void*)_device.Device.Handle;
            vkInfo.Instance = (void*)_device.Instance.Handle;
            vkInfo.PhysicalDevice = (void*)_device.PhysicalDevice.Handle;
            vkInfo.ComputeQueueIndex = 0;
            vkInfo.ComputeQueueFamily = _device.GraphicsFamilyIndex;
            vkInfo.GraphicsQueueIndex = 0;
            vkInfo.GraphicsQueueFamily = _device.GraphicsFamilyIndex;
            vkInfo.OpticalFlowQueueIndex = 0;
            vkInfo.OpticalFlowQueueFamily = _device.GraphicsFamilyIndex;
            vkInfo.UseNativeOpticalFlowMode = 0;

            int setVkRes = StreamlineAPI.slSetVulkanInfo(&vkInfo);
            if (setVkRes == (int)SlResult.eOk)
            {
                var adapterInfo = new AdapterInfo((void*)_device.PhysicalDevice.Handle);

                // Check DLSS SR
                int supDlss = StreamlineAPI.slIsFeatureSupported((uint)Feature.kFeatureDLSS, &adapterInfo);
                if (supDlss == (int)SlResult.eOk)
                {
                    _useDLSS_SR = true;
                    DlssAPI.LoadFunctions();
                }
                else
                {
                    LogDlssSrNotSupported((SlResult)supDlss);
                }

                // Check DLSS RR
                int supRes = StreamlineAPI.slIsFeatureSupported((uint)Feature.kFeatureDLSS_RR, &adapterInfo);
                if (supRes == (int)SlResult.eOk)
                {
                    _useDLSS_RR = true;
                    DlssdAPI.LoadFunctions();
                }
                else
                {
                    LogDlssRrNotSupported((SlResult)supRes);
                }

                if (_useDLSS_SR || _useDLSS_RR)
                {
                    _slViewport = new ViewportHandle(1);
                }

                // Initialize DLSS SR options/settings if supported
                if (_useDLSS_SR)
                {
                    if (DlssAPI.IsLoaded)
                    {
                        var dlssOptions = CreateDlssOptions();
                        var vp = _slViewport;
                        DlssAPI.SetOptions(in vp, in dlssOptions);

                        if (DlssAPI.GetOptimalSettings(in dlssOptions, out var dlssSettings) == SlResult.eOk)
                        {
                            if (!_useDLSS_RR)
                            {
                                _renderSize = new Vector2Int((int)dlssSettings.OptimalRenderWidth, (int)dlssSettings.OptimalRenderHeight);
                                LogDlssSrSuccess(dlssOptions.Mode, DlssConfiguration.SrPreset, _framebufferSize.X, _framebufferSize.Y, _renderSize.X, _renderSize.Y);
                            }
                        }
                    }
                }

                // Initialize DLSS RR options/settings if supported
                if (_useDLSS_RR)
                {
                    var dlssdOptions = CreateDlssdOptions();

                    if (DlssdAPI.GetOptimalSettings(in dlssdOptions, out var dlssdSettings) == SlResult.eOk)
                    {
                        _renderSize = new Vector2Int((int)dlssdSettings.OptimalRenderWidth, (int)dlssdSettings.OptimalRenderHeight);
                        LogDlssRrSuccess(dlssdOptions.Mode, DlssConfiguration.RrPreset, _framebufferSize.X, _framebufferSize.Y, _renderSize.X, _renderSize.Y);
                    }
                }

                // Check Reflex
                int supReflex = StreamlineAPI.slIsFeatureSupported((uint)Feature.kFeatureReflex, &adapterInfo);
                if (supReflex == (int)SlResult.eOk)
                {
                    _useReflex = true;
                    ReflexAPI.LoadFunctions();

                    var reflexOpt = ReflexOptions.Create();
                    reflexOpt.Mode = (uint)ReflexMode.eLowLatencyWithBoost;
                    reflexOpt.UseMarkersToOptimize = 0;
                    var setReflexRes = ReflexAPI.SetOptions(in reflexOpt);
                    LogReflexSuccess(setReflexRes);
                }
                else
                {
                    LogReflexNotSupported((SlResult)supReflex);
                }

                // Check PCL
                int supPCL = StreamlineAPI.slIsFeatureSupported((uint)Feature.kFeaturePCL, &adapterInfo);
                if (supPCL == (int)SlResult.eOk)
                {
                    PclAPI.LoadFunctions();
                    LogPclSuccess();
                }
                else
                {
                    LogPclNotSupported((SlResult)supPCL);
                }
            }
            else
            {
                LogFailedSetVulkanInfo((SlResult)setVkRes);
            }
        }
        catch (Exception ex)
        {
            LogVulkanSetupError(ex.Message);
        }

        if (!_useDLSS_RR && !_useDLSS_SR)
        {
            _renderSize = _framebufferSize;
        }
    }

    public void Initialize(ReadOnlySpan<VertexElement> layout, uint stride)
    {
        _pmj02bnTexture = new Pmj02bnTexture(_device);
        _pipeline = new VulkanRayTracingPipeline(_device);
        CreateStorageImage();
        CreateDescriptorPoolAndSets();
        InitStaticDescriptors();
        _meshPool = new DynamicMeshPool(_device);
        if (_ommManager != null)
        {
            _meshPool.SetOpacityMicromapManager(_ommManager);
        }
    }

    private void CreateImageHelper(uint width, uint height, Format format, ImageUsageFlags usage, out Image image, out DeviceMemory memory, out ImageView imageView)
    {
        ImageCreateInfo imageInfo = new()
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Format = format,
            Extent = new Extent3D(width, height, 1),
            MipLevels = 1,
            ArrayLayers = 1,
            Samples = SampleCountFlags.Count1Bit,
            Tiling = ImageTiling.Optimal,
            Usage = usage,
            InitialLayout = ImageLayout.Undefined
        };

        if (_device.Vk.CreateImage(_device.Device, in imageInfo, null, out image) != Result.Success)
            throw new Exception($"Failed to create image with format {format}!");

        _device.Vk.GetImageMemoryRequirements(_device.Device, image, out var memReqs);

        MemoryAllocateInfo allocInfo = new()
        {
            SType = StructureType.MemoryAllocateInfo,
            AllocationSize = memReqs.Size,
            MemoryTypeIndex = _device.FindMemoryType(memReqs.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit)
        };

        if (_device.Vk.AllocateMemory(_device.Device, in allocInfo, null, out memory) != Result.Success)
            throw new Exception("Failed to allocate image memory!");

        _device.Vk.BindImageMemory(_device.Device, image, memory, 0);

        ImageViewCreateInfo viewInfo = new()
        {
            SType = StructureType.ImageViewCreateInfo,
            Image = image,
            ViewType = ImageViewType.Type2D,
            Format = format,
            SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1)
        };
        if (_device.Vk.CreateImageView(_device.Device, in viewInfo, null, out imageView) != Result.Success)
            throw new Exception("Failed to create image view!");
    }

    private void DestroyImageHelper(Image image, DeviceMemory memory, ImageView view)
    {
        if (view.Handle != 0) _device.Vk.DestroyImageView(_device.Device, view, null);
        if (image.Handle != 0) _device.Vk.DestroyImage(_device.Device, image, null);
        if (memory.Handle != 0) _device.Vk.FreeMemory(_device.Device, memory, null);
    }

    private void CreateStorageImage()
    {
        // 1. Create main output target image (high-res)
        CreateImageHelper((uint)_framebufferSize.X, (uint)_framebufferSize.Y, Format.R16G16B16A16Sfloat,
            ImageUsageFlags.StorageBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit,
            out _storageImage, out _storageImageMemory, out _storageImageView);

        // 2. Create G-buffers at render resolution
        CreateImageHelper((uint)_renderSize.X, (uint)_renderSize.Y, Format.R16G16B16A16Sfloat,
            ImageUsageFlags.StorageBit | ImageUsageFlags.SampledBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit,
            out _noisyColorImage, out _noisyColorImageMemory, out _noisyColorImageView);

        CreateImageHelper((uint)_renderSize.X, (uint)_renderSize.Y, Format.R16G16B16A16Sfloat,
            ImageUsageFlags.StorageBit | ImageUsageFlags.SampledBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit,
            out _normalImage, out _normalImageMemory, out _normalImageView);

        CreateImageHelper((uint)_renderSize.X, (uint)_renderSize.Y, Format.R8G8B8A8Unorm,
            ImageUsageFlags.StorageBit | ImageUsageFlags.SampledBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit,
            out _albedoImage, out _albedoImageMemory, out _albedoImageView);

        CreateImageHelper((uint)_renderSize.X, (uint)_renderSize.Y, Format.R8G8B8A8Unorm,
            ImageUsageFlags.StorageBit | ImageUsageFlags.SampledBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit,
            out _specularAlbedoImage, out _specularAlbedoImageMemory, out _specularAlbedoImageView);

        CreateImageHelper((uint)_renderSize.X, (uint)_renderSize.Y, Format.R16G16Sfloat,
            ImageUsageFlags.StorageBit | ImageUsageFlags.SampledBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit,
            out _motionVectorsImage, out _motionVectorsImageMemory, out _motionVectorsImageView);

        CreateImageHelper((uint)_renderSize.X, (uint)_renderSize.Y, Format.R32Sfloat,
            ImageUsageFlags.StorageBit | ImageUsageFlags.SampledBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit,
            out _depthImage, out _depthImageMemory, out _depthImageView);

        CreateImageHelper((uint)_renderSize.X, (uint)_renderSize.Y, Format.R32Sfloat,
            ImageUsageFlags.StorageBit | ImageUsageFlags.SampledBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit,
            out _linearDepthImage, out _linearDepthImageMemory, out _linearDepthImageView);

        CreateImageHelper((uint)_renderSize.X, (uint)_renderSize.Y, Format.R16G16B16A16Sfloat,
            ImageUsageFlags.StorageBit | ImageUsageFlags.SampledBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit,
            out _colorBeforeTransparencyImage, out _colorBeforeTransparencyImageMemory, out _colorBeforeTransparencyImageView);

        CreateImageHelper((uint)_renderSize.X, (uint)_renderSize.Y, Format.R16G16Sfloat,
            ImageUsageFlags.StorageBit | ImageUsageFlags.SampledBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit,
            out _specularMotionVectorsImage, out _specularMotionVectorsImageMemory, out _specularMotionVectorsImageView);

        CreateImageHelper(1, 1, Format.R32Sfloat,
            ImageUsageFlags.StorageBit | ImageUsageFlags.SampledBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit,
            out _exposureImage, out _exposureImageMemory, out _exposureImageView);

        CreateImageHelper((uint)_renderSize.X, (uint)_renderSize.Y, Format.R16Sfloat,
            ImageUsageFlags.StorageBit | ImageUsageFlags.SampledBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit,
            out _biasColorImage, out _biasColorImageMemory, out _biasColorImageView);

        CreateImageHelper((uint)_renderSize.X, (uint)_renderSize.Y, Format.R16Sfloat,
            ImageUsageFlags.StorageBit | ImageUsageFlags.SampledBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit,
            out _specularHitDistanceImage, out _specularHitDistanceImageMemory, out _specularHitDistanceImageView);

        CreateImageHelper((uint)_renderSize.X, (uint)_renderSize.Y, Format.R16G16B16A16Sfloat,
            ImageUsageFlags.StorageBit | ImageUsageFlags.SampledBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit,
            out _diffuseHitNoisyImage, out _diffuseHitNoisyImageMemory, out _diffuseHitNoisyImageView);

        CreateImageHelper((uint)_renderSize.X, (uint)_renderSize.Y, Format.R16G16B16A16Sfloat,
            ImageUsageFlags.StorageBit | ImageUsageFlags.SampledBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit,
            out _specularHitNoisyImage, out _specularHitNoisyImageMemory, out _specularHitNoisyImageView);

        _gBufferImages = [
            _noisyColorImage, _normalImage, _albedoImage, _specularAlbedoImage,
            _motionVectorsImage, _depthImage, _linearDepthImage, _colorBeforeTransparencyImage,
            _specularMotionVectorsImage, _specularHitDistanceImage, _diffuseHitNoisyImage, _specularHitNoisyImage
        ];
    }

    public IMesh CreateMesh<T>(List<T> vertices, List<ushort> indices, uint opaqueIndexCount = 0, List<ushort>? ommIndices = null) where T : unmanaged
        => _meshPool.Allocate(vertices, indices, opaqueIndexCount, ommIndices);

    public void DeleteMesh(IMesh mesh) => _pendingMeshesToDispose.Enqueue(mesh);

    public ITextureArray CreateTextureArray(int width, int height, byte[][] pixels)
    {
        _cachedPixels = pixels;
        _cachedTexWidth = width;
        _cachedTexHeight = height;
        TryInitOpacityMicromap();
        return new VulkanTextureArray(_device, width, height, pixels);
    }

    public void BindTextureArray(ITextureArray textureArray)
    {
        _currentTextureArray = textureArray;
        Array.Fill(_textureDirty, true);
    }

    public void BindMaterials(ReadOnlySpan<MaterialData> materials)
    {
        _cachedMaterials = materials.ToArray();
        TryInitOpacityMicromap();

        ulong size = (ulong)(materials.Length * sizeof(MaterialData));
        if (_materialBuffer == null || _materialBuffer.Size < size)
        {
            _materialBuffer?.Dispose();
            _materialBuffer = new VulkanBuffer(_device, size, BufferUsageFlags.StorageBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
        }
        var span = new Span<MaterialData>(_materialBuffer.MappedMemory, materials.Length);
        materials.CopyTo(span);
        Array.Fill(_materialsDirty, true);
    }

    private void TryInitOpacityMicromap()
    {
        if (_device.ExtOpacityMicromap == null || _ommManager != null)
            return;

        if (_cachedPixels != null && _cachedMaterials != null && _cachedPixels.Length > 0 && _cachedMaterials.Length > 0)
        {
            try
            {
                var mgr = new OpacityMicromapManager(_device, _cachedTexWidth, _cachedTexHeight, _cachedPixels, _cachedMaterials);
                if (mgr.IsValid)
                {
                    _ommManager = mgr;
                    if (_meshPool != null)
                    {
                        _meshPool.SetOpacityMicromapManager(_ommManager);
                    }
                    _logger.LogInformation("[OMM] Opacity Micromaps successfully built and bound to mesh pool.");
                }
                else
                {
                    mgr.Dispose();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OMM] Failed to initialize Opacity Micromap");
                _ommManager?.Dispose();
                _ommManager = null;
            }
        }
    }

    public void SubmitDraw(IMesh mesh, Vector3 position)
    {
        if (_drawCallCount >= _drawCalls.Length)
            Array.Resize(ref _drawCalls, _drawCalls.Length * 2);
        _drawCalls[_drawCallCount++] = new DrawCall { Mesh = mesh, Position = position };
    }

    public void ClearDraws() => _drawCallCount = 0;

    private void CreateCommandPool()
    {
        CommandPoolCreateInfo poolInfo = new() { SType = StructureType.CommandPoolCreateInfo, Flags = CommandPoolCreateFlags.ResetCommandBufferBit, QueueFamilyIndex = _device.GraphicsFamilyIndex };
        _device.Vk.CreateCommandPool(_device.Device, in poolInfo, null, out _commandPool);
    }

    private void CreateCommandBuffers()
    {
        CommandBufferAllocateInfo allocInfo = new() { SType = StructureType.CommandBufferAllocateInfo, CommandPool = _commandPool, Level = CommandBufferLevel.Primary, CommandBufferCount = MaxFramesInFlight };
        fixed (CommandBuffer* pCmds = _commandBuffers) _device.Vk.AllocateCommandBuffers(_device.Device, in allocInfo, pCmds);
    }

    private void CreateSyncObjects()
    {
        SemaphoreCreateInfo semaphoreInfo = new() { SType = StructureType.SemaphoreCreateInfo };
        FenceCreateInfo fenceInfo = new() { SType = StructureType.FenceCreateInfo, Flags = FenceCreateFlags.SignaledBit };

        for (int i = 0; i < MaxFramesInFlight; i++)
        {
            _device.Vk.CreateSemaphore(_device.Device, in semaphoreInfo, null, out _imageAvailableSemaphores[i]);
            _device.Vk.CreateFence(_device.Device, in fenceInfo, null, out _inFlightFences[i]);
        }

        _renderFinishedSemaphores = new Semaphore[_swapchain.Images.Length];
        for (int i = 0; i < _renderFinishedSemaphores.Length; i++)
        {
            _device.Vk.CreateSemaphore(_device.Device, in semaphoreInfo, null, out _renderFinishedSemaphores[i]);
        }
    }

    private void CreateDescriptorPoolAndSets()
    {
        DescriptorPoolSize[] poolSizes = [
            new() { Type = DescriptorType.AccelerationStructureKhr, DescriptorCount = MaxFramesInFlight },
            new() { Type = DescriptorType.StorageImage, DescriptorCount = MaxFramesInFlight * 16 },
            new() { Type = DescriptorType.UniformBuffer, DescriptorCount = MaxFramesInFlight },
            new() { Type = DescriptorType.CombinedImageSampler, DescriptorCount = MaxFramesInFlight * 2 },
            new() { Type = DescriptorType.StorageBuffer, DescriptorCount = MaxFramesInFlight * 2 }
        ];

        fixed (DescriptorPoolSize* pPoolSizes = poolSizes)
        {
            DescriptorPoolCreateInfo poolInfo = new() { SType = StructureType.DescriptorPoolCreateInfo, PoolSizeCount = 5, PPoolSizes = pPoolSizes, MaxSets = MaxFramesInFlight };
            _device.Vk.CreateDescriptorPool(_device.Device, in poolInfo, null, out _descriptorPool);
        }

        var layouts = stackalloc DescriptorSetLayout[MaxFramesInFlight];
        for (int i = 0; i < MaxFramesInFlight; i++) layouts[i] = _pipeline!.DescriptorSetLayout;

        DescriptorSetAllocateInfo allocInfo = new() { SType = StructureType.DescriptorSetAllocateInfo, DescriptorPool = _descriptorPool, DescriptorSetCount = MaxFramesInFlight, PSetLayouts = layouts };
        fixed (DescriptorSet* pSets = _descriptorSets) _device.Vk.AllocateDescriptorSets(_device.Device, in allocInfo, pSets);

        for (int i = 0; i < MaxFramesInFlight; i++)
        {
            _cameraBuffers[i] = new VulkanBuffer(_device, (ulong)sizeof(CameraData), BufferUsageFlags.UniformBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

            DescriptorBufferInfo bufferInfo = new() { Buffer = _cameraBuffers[i].Buffer, Offset = 0, Range = (ulong)sizeof(CameraData) };
            WriteDescriptorSet descriptorWrite = new() { SType = StructureType.WriteDescriptorSet, DstSet = _descriptorSets[i], DstBinding = 2, DstArrayElement = 0, DescriptorType = DescriptorType.UniformBuffer, DescriptorCount = 1, PBufferInfo = &bufferInfo };

            _device.Vk.UpdateDescriptorSets(_device.Device, 1, &descriptorWrite, 0, null);
        }
    }

    private void RecreateSwapchain()
    {
        _device.Vk.DeviceWaitIdle(_device.Device);

        foreach (var sem in _renderFinishedSemaphores)
        {
            if (sem.Handle != 0) _device.Vk.DestroySemaphore(_device.Device, sem, null);
        }

        DestroyImageHelper(_storageImage, _storageImageMemory, _storageImageView);

        DestroyImageHelper(_noisyColorImage, _noisyColorImageMemory, _noisyColorImageView);
        DestroyImageHelper(_normalImage, _normalImageMemory, _normalImageView);
        DestroyImageHelper(_albedoImage, _albedoImageMemory, _albedoImageView);
        DestroyImageHelper(_specularAlbedoImage, _specularAlbedoImageMemory, _specularAlbedoImageView);
        DestroyImageHelper(_motionVectorsImage, _motionVectorsImageMemory, _motionVectorsImageView);
        DestroyImageHelper(_depthImage, _depthImageMemory, _depthImageView);
        DestroyImageHelper(_linearDepthImage, _linearDepthImageMemory, _linearDepthImageView);
        DestroyImageHelper(_colorBeforeTransparencyImage, _colorBeforeTransparencyImageMemory, _colorBeforeTransparencyImageView);
        DestroyImageHelper(_specularMotionVectorsImage, _specularMotionVectorsImageMemory, _specularMotionVectorsImageView);
        DestroyImageHelper(_exposureImage, _exposureImageMemory, _exposureImageView);
        DestroyImageHelper(_biasColorImage, _biasColorImageMemory, _biasColorImageView);
        DestroyImageHelper(_specularHitDistanceImage, _specularHitDistanceImageMemory, _specularHitDistanceImageView);
        DestroyImageHelper(_diffuseHitNoisyImage, _diffuseHitNoisyImageMemory, _diffuseHitNoisyImageView);
        DestroyImageHelper(_specularHitNoisyImage, _specularHitNoisyImageMemory, _specularHitNoisyImageView);

        _swapchain.Dispose();
        _swapchain = new VulkanSwapchain(_device, _framebufferSize);
        _resetHistoryRequested = true;

        if (_useDLSS_RR)
        {
            var dlssdOptions = CreateDlssdOptions();

            if (DlssdAPI.GetOptimalSettings(in dlssdOptions, out var dlssdSettings) == SlResult.eOk)
            {
                _renderSize = new Vector2Int((int)dlssdSettings.OptimalRenderWidth, (int)dlssdSettings.OptimalRenderHeight);
            }
        }
        else if (_useDLSS_SR)
        {
            if (DlssAPI.IsLoaded)
            {
                var dlssOptions = CreateDlssOptions();
                var vp = _slViewport;
                DlssAPI.SetOptions(in vp, in dlssOptions);

                if (DlssAPI.GetOptimalSettings(in dlssOptions, out var dlssSettings) == SlResult.eOk)
                {
                    _renderSize = new Vector2Int((int)dlssSettings.OptimalRenderWidth, (int)dlssSettings.OptimalRenderHeight);
                }
            }
        }
        else
        {
            _renderSize = _framebufferSize;
        }

        SemaphoreCreateInfo semaphoreInfo = new() { SType = StructureType.SemaphoreCreateInfo };
        _renderFinishedSemaphores = new Semaphore[_swapchain.Images.Length];
        for (int i = 0; i < _renderFinishedSemaphores.Length; i++)
        {
            _device.Vk.CreateSemaphore(_device.Device, in semaphoreInfo, null, out _renderFinishedSemaphores[i]);
        }

        CreateStorageImage();
        InitStaticDescriptors();
        Array.Fill(_materialsDirty, true);
        Array.Fill(_textureDirty, true);
    }

    public void RenderFrame(in CameraData cameraData)
    {
        if (_pipeline == null) throw new Exception("Pipeline is not initialized.");

        StartFrame();
        SetSimulationStart();

        UpdateCameraBuffer(in cameraData);

        // WaitForFences and mesh disposal
        _device.Vk.WaitForFences(_device.Device, 1, ref _inFlightFences[_currentFrame], Vk.True, ulong.MaxValue);

        foreach (var mesh in _meshesToDispose[_currentFrame])
        {
            var alloc = (MeshAllocation)mesh;
            if (alloc.Blas.Handle != 0)
            {
                _device.KhrAccelerationStructure.DestroyAccelerationStructure(_device.Device, alloc.Blas, null);
                alloc.Blas = default;
            }
            _meshPool.Free(alloc);
            mesh.Dispose();
        }
        _meshesToDispose[_currentFrame].Clear();

        int pendingDisposeCount = _pendingMeshesToDispose.Count;
        for (int i = 0; i < pendingDisposeCount; i++)
        {
            if (_pendingMeshesToDispose.TryDequeue(out var mesh))
            {
                if (mesh.IsReady) _meshesToDispose[_currentFrame].Add(mesh);
                else _pendingMeshesToDispose.Enqueue(mesh);
            }
        }

        if (_framebufferSize.X == 0 || _framebufferSize.Y == 0)
        {
            _currentFrameToken = null;
            _prevFrameToken = null;
            return;
        }

        if (_framebufferResized)
        {
            _framebufferResized = false;
            RecreateSwapchain();
            return;
        }

        SetSimulationEnd();

        uint imageIndex;
        var result = _device.KhrSwapchain.AcquireNextImage(_device.Device, _swapchain.Swapchain, ulong.MaxValue, _imageAvailableSemaphores[_currentFrame], default, &imageIndex);

        if (result == Result.ErrorOutOfDateKhr)
        {
            RecreateSwapchain();
            return;
        }

        UpdateStreamlineFrameTokenAndReflex(in cameraData);

        _device.Vk.ResetFences(_device.Device, 1, ref _inFlightFences[_currentFrame]);

        CommandBuffer cmd = _commandBuffers[_currentFrame];
        _device.Vk.ResetCommandBuffer(cmd, 0);

        CommandBufferBeginInfo beginInfo = new() { SType = StructureType.CommandBufferBeginInfo };
        _device.Vk.BeginCommandBuffer(cmd, in beginInfo);

        RecordCommandBuffer(cmd, imageIndex, in cameraData);

        SubmitAndPresent(cmd, imageIndex);
    }

    private void UpdateCameraBuffer(in CameraData cameraData)
    {
        _frameCount++;

        var pMapped = (CameraData*)_cameraBuffers[_currentFrame].MappedMemory;
        if (pMapped != null)
        {
            *pMapped = cameraData;

            if (_lastViewProj != default && !_resetHistoryRequested)
            {
                if (cameraData.ChunkPosition != _lastChunkPos)
                {
                    var deltaChunk = cameraData.ChunkPosition - _lastChunkPos;
                    var offset = new Vector3(deltaChunk.X * 16.0f, deltaChunk.Y * 16.0f, deltaChunk.Z * 16.0f);
                    pMapped->PrevViewProjection = Matrix4x4.CreateTranslation(offset) * _lastViewProj;
                }
                else
                {
                    pMapped->PrevViewProjection = _lastViewProj;
                }
            }
            else
            {
                pMapped->PrevViewProjection = cameraData.ViewProjection;
            }

            _lastViewProj = cameraData.ViewProjection;
            _lastLocalPos = cameraData.LocalPosition;
            _lastChunkPos = cameraData.ChunkPosition;

            _seed = unchecked(_seed + 1664525 * _frameCount + 1013904223);
            pMapped->FrameCount = _frameCount;
            pMapped->Seed = _seed;

            UpdateJitter();
            pMapped->JitterX = _currentJitterX;
            pMapped->JitterY = _currentJitterY;
        }
    }

    private void UpdateStreamlineFrameTokenAndReflex(in CameraData cameraData)
    {
        if (_currentFrameToken == null)
        {
            StartFrame();
        }

        if (_useReflex && _currentFrameToken != null && ReflexAPI.SetCameraDataPtr != null)
        {
            var originalView = Matrix4x4.CreateLookAt(cameraData.LocalPosition, cameraData.LocalPosition + cameraData.CameraFwd, cameraData.CameraUp);

            float aspect = _framebufferSize.X / (float)Math.Max(1, _framebufferSize.Y);
            var originalProj = Matrix4x4.CreatePerspectiveFieldOfView(float.Pi / 2.5f, aspect, 0.1f, 3000f);
            originalProj.M33 = -originalProj.M33 - 1.0f;
            originalProj.M43 = -originalProj.M43;
            originalProj.M22 *= -1;

            var viewport = _slViewport;
            var reflexCam = ReflexCameraData.Create();
            reflexCam.WorldToViewMatrix = originalView;
            reflexCam.ViewToClipMatrix = originalProj;
            reflexCam.PrevRenderedWorldToViewMatrix = _prevWorldToView;
            reflexCam.PrevRenderedViewToClipMatrix = _prevViewToClip;

            // Save for next frame
            _prevWorldToView = originalView;
            _prevViewToClip = originalProj;

            var setCamRes = ReflexAPI.SetCameraData(in viewport, _currentFrameToken, in reflexCam);
            if (setCamRes != SlResult.eOk)
            {
                LogReflexCameraDataError(setCamRes);
            }
        }
    }

    private const int InitialTlasCapacity = 256;

    private void BuildTLAS(CommandBuffer cmd)
    {
        int requiredCapacity = Math.Max(InitialTlasCapacity, _drawCallCount);
        bool capacityExceeded = _tlasCapacities[_currentFrame] < requiredCapacity;
        bool needsRebuild = _tlasNeedsRebuild[_currentFrame] || capacityExceeded || _tlasHandles[_currentFrame].Handle == 0;

        if (capacityExceeded)
        {
            int newCapacity = Math.Max(requiredCapacity, Math.Max(_tlasCapacities[_currentFrame] + 128, (int)(_tlasCapacities[_currentFrame] * 1.5f)));

            _instancesBuffers[_currentFrame]?.Dispose();
            _instanceDataBuffers[_currentFrame]?.Dispose();

            _instancesBuffers[_currentFrame] = new VulkanBuffer(_device, (ulong)(newCapacity * sizeof(AccelerationStructureInstanceKHR)), BufferUsageFlags.ShaderDeviceAddressBit | BufferUsageFlags.AccelerationStructureBuildInputReadOnlyBitKhr, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);
            _instanceDataBuffers[_currentFrame] = new VulkanBuffer(_device, (ulong)(newCapacity * sizeof(InstanceData)), BufferUsageFlags.StorageBufferBit, MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit);

            _tlasCapacities[_currentFrame] = newCapacity;
        }

        var instSpan = new Span<AccelerationStructureInstanceKHR>(_instancesBuffers[_currentFrame].MappedMemory, _drawCallCount);
        var dataSpan = new Span<InstanceData>(_instanceDataBuffers[_currentFrame].MappedMemory, _drawCallCount);

        for (int i = 0; i < _drawCallCount; i++)
        {
            var alloc = (MeshAllocation)_drawCalls[i].Mesh;
            var pos = _drawCalls[i].Position;

            var tf = new TransformMatrixKHR();
            tf.Matrix[0] = 1; tf.Matrix[1] = 0; tf.Matrix[2] = 0; tf.Matrix[3] = pos.X;
            tf.Matrix[4] = 0; tf.Matrix[5] = 1; tf.Matrix[6] = 0; tf.Matrix[7] = pos.Y;
            tf.Matrix[8] = 0; tf.Matrix[9] = 0; tf.Matrix[10] = 1; tf.Matrix[11] = pos.Z;

            instSpan[i] = new AccelerationStructureInstanceKHR
            {
                Transform = tf,
                InstanceCustomIndex = (uint)i,
                Mask = 0xFF,
                InstanceShaderBindingTableRecordOffset = 0,
                Flags = GeometryInstanceFlagsKHR.None,
                AccelerationStructureReference = alloc.BlasDeviceAddress
            };

            dataSpan[i] = new InstanceData
            {
                VertexOffset = (uint)alloc.VertexOffset,
                IndexOffset = alloc.FirstIndex,
                OpaqueIndexCount = alloc.OpaqueIndexCount,
                VertexAddress = alloc.VertexAddress,
                IndexAddress = alloc.IndexAddress
            };
        }

        var instancesData = new AccelerationStructureGeometryInstancesDataKHR { SType = StructureType.AccelerationStructureGeometryInstancesDataKhr, ArrayOfPointers = Vk.False, Data = new DeviceOrHostAddressConstKHR { DeviceAddress = _instancesBuffers[_currentFrame].DeviceAddress } };
        var geometry = new AccelerationStructureGeometryKHR { SType = StructureType.AccelerationStructureGeometryKhr, GeometryType = GeometryTypeKHR.InstancesKhr, Geometry = new AccelerationStructureGeometryDataKHR { Instances = instancesData } };

        var buildFlags = BuildAccelerationStructureFlagsKHR.PreferFastTraceBitKhr | BuildAccelerationStructureFlagsKHR.AllowUpdateBitKhr;

        if (needsRebuild)
        {
            var buildInfoSize = new AccelerationStructureBuildGeometryInfoKHR
            {
                SType = StructureType.AccelerationStructureBuildGeometryInfoKhr,
                Type = AccelerationStructureTypeKHR.TopLevelKhr,
                Flags = buildFlags,
                GeometryCount = 1,
                PGeometries = &geometry
            };

            uint maxInstanceCount = (uint)_tlasCapacities[_currentFrame];
            _device.KhrAccelerationStructure.GetAccelerationStructureBuildSizes(_device.Device, AccelerationStructureBuildTypeKHR.DeviceKhr, in buildInfoSize, &maxInstanceCount, out var buildSizes);

            ulong requiredScratchSize = Math.Max(buildSizes.BuildScratchSize, buildSizes.UpdateScratchSize);

            if (_tlasScratchCapacities[_currentFrame] < requiredScratchSize)
            {
                _tlasScratchBuffers[_currentFrame]?.Dispose();
                ulong newCap = Math.Max(requiredScratchSize, _tlasScratchCapacities[_currentFrame] * 2);
                newCap = Math.Max(newCap, 1024 * 1024);
                _tlasScratchBuffers[_currentFrame] = new VulkanBuffer(_device, newCap, BufferUsageFlags.StorageBufferBit | BufferUsageFlags.ShaderDeviceAddressBit, MemoryPropertyFlags.DeviceLocalBit);
                _tlasScratchCapacities[_currentFrame] = newCap;
            }

            if (_tlasHandles[_currentFrame].Handle != 0)
                _device.KhrAccelerationStructure.DestroyAccelerationStructure(_device.Device, _tlasHandles[_currentFrame], null);

            _tlasBuffers[_currentFrame]?.Dispose();
            _tlasBuffers[_currentFrame] = new VulkanBuffer(_device, buildSizes.AccelerationStructureSize, BufferUsageFlags.AccelerationStructureStorageBitKhr, MemoryPropertyFlags.DeviceLocalBit);

            var createInfo = new AccelerationStructureCreateInfoKHR { SType = StructureType.AccelerationStructureCreateInfoKhr, Buffer = _tlasBuffers[_currentFrame].Buffer, Size = buildSizes.AccelerationStructureSize, Type = AccelerationStructureTypeKHR.TopLevelKhr };
            _device.KhrAccelerationStructure.CreateAccelerationStructure(_device.Device, in createInfo, null, out _tlasHandles[_currentFrame]);

            _tlasDescriptorDirty[_currentFrame] = true;
            _tlasNeedsRebuild[_currentFrame] = false;
        }

        var buildInfo = new AccelerationStructureBuildGeometryInfoKHR
        {
            SType = StructureType.AccelerationStructureBuildGeometryInfoKhr,
            Type = AccelerationStructureTypeKHR.TopLevelKhr,
            Flags = buildFlags,
            GeometryCount = 1,
            PGeometries = &geometry,
            Mode = BuildAccelerationStructureModeKHR.BuildKhr,
            SrcAccelerationStructure = default,
            DstAccelerationStructure = _tlasHandles[_currentFrame],
            ScratchData = new DeviceOrHostAddressKHR { DeviceAddress = _tlasScratchBuffers[_currentFrame].DeviceAddress }
        };

        var buildRange = new AccelerationStructureBuildRangeInfoKHR { PrimitiveCount = (uint)_drawCallCount, PrimitiveOffset = 0, FirstVertex = 0, TransformOffset = 0 };
        var pBuildRange = &buildRange;

        _device.KhrAccelerationStructure.CmdBuildAccelerationStructures(cmd, 1, in buildInfo, &pBuildRange);

        _tlasInstanceCounts[_currentFrame] = _drawCallCount;

        var buildBarrier = new MemoryBarrier2 { SType = StructureType.MemoryBarrier2, SrcStageMask = PipelineStageFlags2.AccelerationStructureBuildBitKhr, SrcAccessMask = AccessFlags2.AccelerationStructureWriteBitKhr, DstStageMask = PipelineStageFlags2.RayTracingShaderBitKhr, DstAccessMask = AccessFlags2.AccelerationStructureReadBitKhr };
        var depInfo1 = new DependencyInfo { SType = StructureType.DependencyInfo, MemoryBarrierCount = 1, PMemoryBarriers = &buildBarrier };
        _device.Vk.CmdPipelineBarrier2(cmd, in depInfo1);
    }

    private void InitStaticDescriptors()
    {
        var writes = stackalloc WriteDescriptorSet[12];
        for (int i = 0; i < MaxFramesInFlight; i++)
        {
            DescriptorImageInfo noisyImageInfo = new() { ImageLayout = ImageLayout.General, ImageView = _noisyColorImageView };
            writes[0] = new() { SType = StructureType.WriteDescriptorSet, DstSet = _descriptorSets[i], DstBinding = 6, DescriptorCount = 1, DescriptorType = DescriptorType.StorageImage, PImageInfo = &noisyImageInfo };

            DescriptorImageInfo normalImageInfo = new() { ImageLayout = ImageLayout.General, ImageView = _normalImageView };
            writes[1] = new() { SType = StructureType.WriteDescriptorSet, DstSet = _descriptorSets[i], DstBinding = 7, DescriptorCount = 1, DescriptorType = DescriptorType.StorageImage, PImageInfo = &normalImageInfo };

            DescriptorImageInfo albedoImageInfo = new() { ImageLayout = ImageLayout.General, ImageView = _albedoImageView };
            writes[2] = new() { SType = StructureType.WriteDescriptorSet, DstSet = _descriptorSets[i], DstBinding = 9, DescriptorCount = 1, DescriptorType = DescriptorType.StorageImage, PImageInfo = &albedoImageInfo };

            DescriptorImageInfo specAlbedoImageInfo = new() { ImageLayout = ImageLayout.General, ImageView = _specularAlbedoImageView };
            writes[3] = new() { SType = StructureType.WriteDescriptorSet, DstSet = _descriptorSets[i], DstBinding = 10, DescriptorCount = 1, DescriptorType = DescriptorType.StorageImage, PImageInfo = &specAlbedoImageInfo };

            DescriptorImageInfo mvecImageInfo = new() { ImageLayout = ImageLayout.General, ImageView = _motionVectorsImageView };
            writes[4] = new() { SType = StructureType.WriteDescriptorSet, DstSet = _descriptorSets[i], DstBinding = 11, DescriptorCount = 1, DescriptorType = DescriptorType.StorageImage, PImageInfo = &mvecImageInfo };

            DescriptorImageInfo depthImageInfo = new() { ImageLayout = ImageLayout.General, ImageView = _depthImageView };
            writes[5] = new() { SType = StructureType.WriteDescriptorSet, DstSet = _descriptorSets[i], DstBinding = 12, DescriptorCount = 1, DescriptorType = DescriptorType.StorageImage, PImageInfo = &depthImageInfo };

            DescriptorImageInfo specularMotionVectorsImageInfo = new() { ImageLayout = ImageLayout.General, ImageView = _specularMotionVectorsImageView };
            writes[6] = new() { SType = StructureType.WriteDescriptorSet, DstSet = _descriptorSets[i], DstBinding = 13, DescriptorCount = 1, DescriptorType = DescriptorType.StorageImage, PImageInfo = &specularMotionVectorsImageInfo };

            DescriptorImageInfo specHitDistanceImageInfo = new() { ImageLayout = ImageLayout.General, ImageView = _specularHitDistanceImageView };
            writes[7] = new() { SType = StructureType.WriteDescriptorSet, DstSet = _descriptorSets[i], DstBinding = 14, DescriptorCount = 1, DescriptorType = DescriptorType.StorageImage, PImageInfo = &specHitDistanceImageInfo };

            DescriptorImageInfo linearDepthImageInfo = new() { ImageLayout = ImageLayout.General, ImageView = _linearDepthImageView };
            writes[8] = new() { SType = StructureType.WriteDescriptorSet, DstSet = _descriptorSets[i], DstBinding = 15, DescriptorCount = 1, DescriptorType = DescriptorType.StorageImage, PImageInfo = &linearDepthImageInfo };

            DescriptorImageInfo colorBeforeTransInfo = new() { ImageLayout = ImageLayout.General, ImageView = _colorBeforeTransparencyImageView };
            writes[9] = new() { SType = StructureType.WriteDescriptorSet, DstSet = _descriptorSets[i], DstBinding = 16, DescriptorCount = 1, DescriptorType = DescriptorType.StorageImage, PImageInfo = &colorBeforeTransInfo };

            DescriptorImageInfo diffuseHitNoisyInfo = new() { ImageLayout = ImageLayout.General, ImageView = _diffuseHitNoisyImageView };
            writes[10] = new() { SType = StructureType.WriteDescriptorSet, DstSet = _descriptorSets[i], DstBinding = 17, DescriptorCount = 1, DescriptorType = DescriptorType.StorageImage, PImageInfo = &diffuseHitNoisyInfo };

            DescriptorImageInfo specularHitNoisyInfo = new() { ImageLayout = ImageLayout.General, ImageView = _specularHitNoisyImageView };
            writes[11] = new() { SType = StructureType.WriteDescriptorSet, DstSet = _descriptorSets[i], DstBinding = 18, DescriptorCount = 1, DescriptorType = DescriptorType.StorageImage, PImageInfo = &specularHitNoisyInfo };

            _device.Vk.UpdateDescriptorSets(_device.Device, 12, writes, 0, null);

            if (_pmj02bnTexture != null)
            {
                DescriptorImageInfo pmjInfo = new() { ImageLayout = ImageLayout.ShaderReadOnlyOptimal, ImageView = _pmj02bnTexture.ImageView, Sampler = _pmj02bnTexture.Sampler };
                WriteDescriptorSet writePmj = new() { SType = StructureType.WriteDescriptorSet, DstSet = _descriptorSets[i], DstBinding = 19, DescriptorCount = 1, DescriptorType = DescriptorType.CombinedImageSampler, PImageInfo = &pmjInfo };
                _device.Vk.UpdateDescriptorSets(_device.Device, 1, &writePmj, 0, null);
            }
        }
    }

    private void UpdateDescriptors()
    {
        if (_tlasDescriptorDirty[_currentFrame])
        {
            AccelerationStructureKHR tlasHandleForWrite = _tlasHandles[_currentFrame];

            WriteDescriptorSetAccelerationStructureKHR descriptorAS = new() { SType = StructureType.WriteDescriptorSetAccelerationStructureKhr, AccelerationStructureCount = 1, PAccelerationStructures = &tlasHandleForWrite };
            WriteDescriptorSet writeAS = new() { SType = StructureType.WriteDescriptorSet, PNext = &descriptorAS, DstSet = _descriptorSets[_currentFrame], DstBinding = 0, DescriptorCount = 1, DescriptorType = DescriptorType.AccelerationStructureKhr };

            DescriptorBufferInfo instanceDataInfo = new() { Buffer = _instanceDataBuffers[_currentFrame].Buffer, Offset = 0, Range = Vk.WholeSize };
            WriteDescriptorSet writeInstanceData = new() { SType = StructureType.WriteDescriptorSet, DstSet = _descriptorSets[_currentFrame], DstBinding = 4, DescriptorCount = 1, DescriptorType = DescriptorType.StorageBuffer, PBufferInfo = &instanceDataInfo };

            var writes = stackalloc WriteDescriptorSet[2] { writeAS, writeInstanceData };
            _device.Vk.UpdateDescriptorSets(_device.Device, 2, writes, 0, null);
            _tlasDescriptorDirty[_currentFrame] = false;
        }

        if (_materialBuffer != null && _materialsDirty[_currentFrame])
        {
            DescriptorBufferInfo matBufferInfo = new() { Buffer = _materialBuffer.Buffer, Offset = 0, Range = Vk.WholeSize };
            WriteDescriptorSet writeMatData = new() { SType = StructureType.WriteDescriptorSet, DstSet = _descriptorSets[_currentFrame], DstBinding = 5, DescriptorCount = 1, DescriptorType = DescriptorType.StorageBuffer, PBufferInfo = &matBufferInfo };
            _device.Vk.UpdateDescriptorSets(_device.Device, 1, &writeMatData, 0, null);
            _materialsDirty[_currentFrame] = false;
        }

        if (_currentTextureArray is VulkanTextureArray vkTexArray && _textureDirty[_currentFrame])
        {
            DescriptorImageInfo texArrayInfo = new() { ImageLayout = ImageLayout.ShaderReadOnlyOptimal, ImageView = vkTexArray.ImageView, Sampler = vkTexArray.Sampler };
            WriteDescriptorSet writeTex = new() { SType = StructureType.WriteDescriptorSet, DstSet = _descriptorSets[_currentFrame], DstBinding = 3, DescriptorCount = 1, DescriptorType = DescriptorType.CombinedImageSampler, PImageInfo = &texArrayInfo };
            _device.Vk.UpdateDescriptorSets(_device.Device, 1, &writeTex, 0, null);
            _textureDirty[_currentFrame] = false;
        }
    }

    private void RecordCommandBuffer(CommandBuffer cmd, uint imageIndex, in CameraData cameraData)
    {
        if (_currentFrameToken != null && PclAPI.SetMarkerPtr != null)
        {
            PclAPI.SetMarker(PCLMarker.eRenderSubmitStart, _currentFrameToken);
        }

        if (_drawCallCount == 0)
        {
            TransitionImageLayout(cmd, _swapchain.Images[imageIndex], ImageLayout.Undefined, ImageLayout.PresentSrcKhr, AccessFlags2.None, AccessFlags2.None, PipelineStageFlags2.TopOfPipeBit, PipelineStageFlags2.BottomOfPipeBit);
            return;
        }

        BuildTLAS(cmd);
        UpdateDescriptors();

        // Батчинг барьеров: переводим все 12 G-буферов из Undefined в General для записи Ray Tracing
        var preTraceBarriers = stackalloc ImageMemoryBarrier2[12];
        for (int i = 0; i < 12; i++)
        {
            preTraceBarriers[i] = new ImageMemoryBarrier2
            {
                SType = StructureType.ImageMemoryBarrier2,
                OldLayout = ImageLayout.Undefined,
                NewLayout = ImageLayout.General,
                SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
                DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
                Image = _gBufferImages[i],
                SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1),
                SrcAccessMask = AccessFlags2.None,
                DstAccessMask = AccessFlags2.ShaderWriteBit,
                SrcStageMask = PipelineStageFlags2.TopOfPipeBit,
                DstStageMask = PipelineStageFlags2.RayTracingShaderBitKhr
            };
        }
        var preTraceDepInfo = new DependencyInfo { SType = StructureType.DependencyInfo, ImageMemoryBarrierCount = 12, PImageMemoryBarriers = preTraceBarriers };
        _device.Vk.CmdPipelineBarrier2(cmd, in preTraceDepInfo);

        TransitionImageLayout(cmd, _exposureImage, ImageLayout.Undefined, ImageLayout.General, AccessFlags2.None, AccessFlags2.TransferWriteBit, PipelineStageFlags2.TopOfPipeBit, PipelineStageFlags2.TransferBit);
        ClearColorImage(cmd, _exposureImage, 1.0f, 1.0f, 1.0f, 1.0f);
        TransitionImageLayout(cmd, _exposureImage, ImageLayout.General, ImageLayout.General, AccessFlags2.TransferWriteBit, AccessFlags2.ShaderReadBit, PipelineStageFlags2.TransferBit, PipelineStageFlags2.ComputeShaderBit);

        float biasValue = _resetHistoryRequested ? 1.0f : 0.0f;
        TransitionImageLayout(cmd, _biasColorImage, ImageLayout.Undefined, ImageLayout.General, AccessFlags2.None, AccessFlags2.TransferWriteBit, PipelineStageFlags2.TopOfPipeBit, PipelineStageFlags2.TransferBit);
        ClearColorImage(cmd, _biasColorImage, biasValue, biasValue, biasValue, biasValue);
        TransitionImageLayout(cmd, _biasColorImage, ImageLayout.General, ImageLayout.General, AccessFlags2.TransferWriteBit, AccessFlags2.ShaderReadBit, PipelineStageFlags2.TransferBit, PipelineStageFlags2.ComputeShaderBit);

        _device.Vk.CmdBindPipeline(cmd, PipelineBindPoint.RayTracingKhr, _pipeline!.Pipeline);
        var descSet = _descriptorSets[_currentFrame];
        _device.Vk.CmdBindDescriptorSets(cmd, PipelineBindPoint.RayTracingKhr, _pipeline.PipelineLayout, 0, 1, &descSet, 0, null);

        var sbtProps = _pipeline.SbtProps;
        var raygenRegion = new StridedDeviceAddressRegionKHR { DeviceAddress = _pipeline.SbtBuffer.DeviceAddress, Stride = sbtProps.RegionAligned, Size = sbtProps.RegionAligned };
        var missRegion = new StridedDeviceAddressRegionKHR { DeviceAddress = _pipeline.SbtBuffer.DeviceAddress + sbtProps.RegionAligned, Stride = sbtProps.RegionAligned, Size = sbtProps.RegionAligned };
        var hitRegion = new StridedDeviceAddressRegionKHR { DeviceAddress = _pipeline.SbtBuffer.DeviceAddress + 2 * sbtProps.RegionAligned, Stride = sbtProps.RegionAligned, Size = sbtProps.RegionAligned };
        var callRegion = new StridedDeviceAddressRegionKHR { };

        _device.KhrRayTracingPipeline.CmdTraceRays(cmd, &raygenRegion, &missRegion, &hitRegion, &callRegion, (uint)_renderSize.X, (uint)_renderSize.Y, 1);

        if (_useDLSS_RR || _useDLSS_SR)
        {
            // Батчинг барьеров: синхронизация записи G-буферов из Ray Tracing для чтения в Compute (DLSS)
            var postTraceBarriers = stackalloc ImageMemoryBarrier2[12];
            for (int i = 0; i < 12; i++)
            {
                postTraceBarriers[i] = new ImageMemoryBarrier2
                {
                    SType = StructureType.ImageMemoryBarrier2,
                    OldLayout = ImageLayout.General,
                    NewLayout = ImageLayout.General,
                    SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
                    DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
                    Image = _gBufferImages[i],
                    SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1),
                    SrcAccessMask = AccessFlags2.ShaderWriteBit,
                    DstAccessMask = AccessFlags2.ShaderReadBit,
                    SrcStageMask = PipelineStageFlags2.RayTracingShaderBitKhr,
                    DstStageMask = PipelineStageFlags2.ComputeShaderBit
                };
            }
            var postTraceDepInfo = new DependencyInfo { SType = StructureType.DependencyInfo, ImageMemoryBarrierCount = 12, PImageMemoryBarriers = postTraceBarriers };
            _device.Vk.CmdPipelineBarrier2(cmd, in postTraceDepInfo);

            EvaluateStreamlineFeatures(cmd, in cameraData);
        }
        else
        {
            // Fallback blit: scale _noisyColorImage (_renderSize) up to _storageImage (_framebufferSize)
            TransitionImageLayout(cmd, _noisyColorImage, ImageLayout.General, ImageLayout.TransferSrcOptimal, AccessFlags2.ShaderWriteBit, AccessFlags2.TransferReadBit, PipelineStageFlags2.RayTracingShaderBitKhr, PipelineStageFlags2.TransferBit);
            TransitionImageLayout(cmd, _storageImage, ImageLayout.Undefined, ImageLayout.TransferDstOptimal, AccessFlags2.None, AccessFlags2.TransferWriteBit, PipelineStageFlags2.TopOfPipeBit, PipelineStageFlags2.TransferBit);

            ImageBlit fallbackBlit = new()
            {
                SrcSubresource = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, 0, 0, 1),
                DstSubresource = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, 0, 0, 1)
            };
            fallbackBlit.SrcOffsets[0] = new Offset3D(0, 0, 0);
            fallbackBlit.SrcOffsets[1] = new Offset3D((int)_renderSize.X, (int)_renderSize.Y, 1);
            fallbackBlit.DstOffsets[0] = new Offset3D(0, 0, 0);
            fallbackBlit.DstOffsets[1] = new Offset3D((int)_framebufferSize.X, (int)_framebufferSize.Y, 1);

            _device.Vk.CmdBlitImage(cmd, _noisyColorImage, ImageLayout.TransferSrcOptimal, _storageImage, ImageLayout.TransferDstOptimal, 1, &fallbackBlit, Filter.Linear);

            TransitionImageLayout(cmd, _storageImage, ImageLayout.TransferDstOptimal, ImageLayout.TransferSrcOptimal, AccessFlags2.TransferWriteBit, AccessFlags2.TransferReadBit, PipelineStageFlags2.TransferBit, PipelineStageFlags2.TransferBit);
        }

        TransitionImageLayout(cmd, _swapchain.Images[imageIndex], ImageLayout.Undefined, ImageLayout.TransferDstOptimal, AccessFlags2.None, AccessFlags2.TransferWriteBit, PipelineStageFlags2.TopOfPipeBit, PipelineStageFlags2.TransferBit);

        ImageBlit blit = new()
        {
            SrcSubresource = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, 0, 0, 1),
            DstSubresource = new ImageSubresourceLayers(ImageAspectFlags.ColorBit, 0, 0, 1)
        };
        blit.SrcOffsets[0] = new Offset3D(0, 0, 0);
        blit.SrcOffsets[1] = new Offset3D((int)_framebufferSize.X, (int)_framebufferSize.Y, 1);
        blit.DstOffsets[0] = new Offset3D(0, 0, 0);
        blit.DstOffsets[1] = new Offset3D((int)_framebufferSize.X, (int)_framebufferSize.Y, 1);

        _device.Vk.CmdBlitImage(cmd, _storageImage, ImageLayout.TransferSrcOptimal, _swapchain.Images[imageIndex], ImageLayout.TransferDstOptimal, 1, &blit, Filter.Linear);

        TransitionImageLayout(cmd, _swapchain.Images[imageIndex], ImageLayout.TransferDstOptimal, ImageLayout.PresentSrcKhr, AccessFlags2.TransferWriteBit, AccessFlags2.None, PipelineStageFlags2.TransferBit, PipelineStageFlags2.BottomOfPipeBit);
    }

    private void EvaluateStreamlineFeatures(CommandBuffer cmd, in CameraData cameraData)
    {
        // Streamline layout transitions are handled by Streamline, but output image _storageImage needs to be in General
        TransitionImageLayout(cmd, _storageImage, ImageLayout.Undefined, ImageLayout.General, AccessFlags2.None, AccessFlags2.ShaderWriteBit, PipelineStageFlags2.TopOfPipeBit, PipelineStageFlags2.ComputeShaderBit);

        FrameToken* frameToken = _currentFrameToken;
        var viewport = _slViewport;

        var originalView = Matrix4x4.CreateLookAt(cameraData.LocalPosition, cameraData.LocalPosition + cameraData.CameraFwd, cameraData.CameraUp);
        float aspect = _framebufferSize.X / (float)Math.Max(1, _framebufferSize.Y);
        var originalProj = Matrix4x4.CreatePerspectiveFieldOfView(float.Pi / 2.5f, aspect, 0.1f, 3000f);
        originalProj.M33 = -originalProj.M33 - 1.0f;
        originalProj.M43 = -originalProj.M43;
        originalProj.M22 *= -1;

        var view = originalView;
        var proj = originalProj;
        Matrix4x4.Invert(view, out var viewInverse);

        // Set Constants
        var consts = Constants.Create();
        consts.CameraViewToClip = proj;
        Matrix4x4.Invert(proj, out consts.ClipToCameraView);
        consts.ClipToPrevClip = cameraData.InverseViewProjection * cameraData.PrevViewProjection;
        Matrix4x4.Invert(consts.ClipToPrevClip, out consts.PrevClipToClip);

        consts.CameraPos = cameraData.LocalPosition;
        consts.CameraUp = cameraData.CameraUp;
        consts.CameraRight = cameraData.CameraRight;
        consts.CameraFwd = cameraData.CameraFwd;
        consts.CameraNear = 0.1f;
        consts.CameraFar = 3000.0f;
        consts.CameraFOV = float.Pi / 2.5f;
        consts.CameraAspectRatio = aspect;
        consts.JitterOffset = new Vector2(-_currentJitterX, -_currentJitterY);
        consts.MvecScale = new Vector2(1.0f, 1.0f);
        consts.DepthInverted = SlBoolean.eTrue;
        consts.CameraMotionIncluded = SlBoolean.eTrue;
        consts.MotionVectors3D = SlBoolean.eFalse;
        consts.MotionVectorsJittered = SlBoolean.eFalse;
        consts.Reset = _resetHistoryRequested ? SlBoolean.eTrue : SlBoolean.eFalse;
        consts.MinRelativeLinearDepthObjectSeparation = 40.0f;

        StreamlineAPI.slSetConstants(&consts, frameToken, &viewport);
        _resetHistoryRequested = false;

        // Setup resource tags
        var extentIn = new Extent((uint)_renderSize.X, (uint)_renderSize.Y);
        var extentOut = new Extent((uint)_framebufferSize.X, (uint)_framebufferSize.Y);
        var extentExposure = new Extent(1, 1);

        uint gBufferUsage = (uint)(ImageUsageFlags.StorageBit | ImageUsageFlags.SampledBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit);
        uint outUsage = (uint)(ImageUsageFlags.StorageBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit);

        var resNoisy = new Resource(ResourceType.eTex2d, (void*)_noisyColorImage.Handle, (void*)_noisyColorImageMemory.Handle, (void*)_noisyColorImageView.Handle, (uint)ImageLayout.General, (uint)_renderSize.X, (uint)_renderSize.Y, (uint)Format.R16G16B16A16Sfloat, gBufferUsage);
        var tagNoisy = new ResourceTag(&resNoisy, BufferType.kBufferTypeScalingInputColor, ResourceLifecycle.eValidUntilPresent, extentIn);
        var tagColor = new ResourceTag(&resNoisy, BufferType.kBufferTypeHUDLessColor, ResourceLifecycle.eValidUntilPresent, extentIn);

        var resAlbedo = new Resource(ResourceType.eTex2d, (void*)_albedoImage.Handle, (void*)_albedoImageMemory.Handle, (void*)_albedoImageView.Handle, (uint)ImageLayout.General, (uint)_renderSize.X, (uint)_renderSize.Y, (uint)Format.R8G8B8A8Unorm, gBufferUsage);
        var tagAlbedo = new ResourceTag(&resAlbedo, BufferType.kBufferTypeAlbedo, ResourceLifecycle.eValidUntilPresent, extentIn);

        var resSpecAlbedo = new Resource(ResourceType.eTex2d, (void*)_specularAlbedoImage.Handle, (void*)_specularAlbedoImageMemory.Handle, (void*)_specularAlbedoImageView.Handle, (uint)ImageLayout.General, (uint)_renderSize.X, (uint)_renderSize.Y, (uint)Format.R8G8B8A8Unorm, gBufferUsage);
        var tagSpecAlbedo = new ResourceTag(&resSpecAlbedo, BufferType.kBufferTypeSpecularAlbedo, ResourceLifecycle.eValidUntilPresent, extentIn);

        var resNormal = new Resource(ResourceType.eTex2d, (void*)_normalImage.Handle, (void*)_normalImageMemory.Handle, (void*)_normalImageView.Handle, (uint)ImageLayout.General, (uint)_renderSize.X, (uint)_renderSize.Y, (uint)Format.R16G16B16A16Sfloat, gBufferUsage);
        var tagNormalRoughness = new ResourceTag(&resNormal, BufferType.kBufferTypeNormalRoughness, ResourceLifecycle.eValidUntilPresent, extentIn);

        var resMvec = new Resource(ResourceType.eTex2d, (void*)_motionVectorsImage.Handle, (void*)_motionVectorsImageMemory.Handle, (void*)_motionVectorsImageView.Handle, (uint)ImageLayout.General, (uint)_renderSize.X, (uint)_renderSize.Y, (uint)Format.R16G16Sfloat, gBufferUsage);
        var tagMvec = new ResourceTag(&resMvec, BufferType.kBufferTypeMotionVectors, ResourceLifecycle.eValidUntilPresent, extentIn);

        var resDepth = new Resource(ResourceType.eTex2d, (void*)_depthImage.Handle, (void*)_depthImageMemory.Handle, (void*)_depthImageView.Handle, (uint)ImageLayout.General, (uint)_renderSize.X, (uint)_renderSize.Y, (uint)Format.R32Sfloat, gBufferUsage);
        var tagDepthStandard = new ResourceTag(&resDepth, BufferType.kBufferTypeDepth, ResourceLifecycle.eValidUntilPresent, extentIn);

        var resLinearDepth = new Resource(ResourceType.eTex2d, (void*)_linearDepthImage.Handle, (void*)_linearDepthImageMemory.Handle, (void*)_linearDepthImageView.Handle, (uint)ImageLayout.General, (uint)_renderSize.X, (uint)_renderSize.Y, (uint)Format.R32Sfloat, gBufferUsage);
        var tagDepthLinear = new ResourceTag(&resLinearDepth, BufferType.kBufferTypeLinearDepth, ResourceLifecycle.eValidUntilPresent, extentIn);

        var resColorBeforeTrans = new Resource(ResourceType.eTex2d, (void*)_colorBeforeTransparencyImage.Handle, (void*)_colorBeforeTransparencyImageMemory.Handle, (void*)_colorBeforeTransparencyImageView.Handle, (uint)ImageLayout.General, (uint)_renderSize.X, (uint)_renderSize.Y, (uint)Format.R16G16B16A16Sfloat, gBufferUsage);
        var tagColorBeforeTrans = new ResourceTag(&resColorBeforeTrans, BufferType.kBufferTypeColorBeforeTransparency, ResourceLifecycle.eValidUntilPresent, extentIn);

        var resOut = new Resource(ResourceType.eTex2d, (void*)_storageImage.Handle, (void*)_storageImageMemory.Handle, (void*)_storageImageView.Handle, (uint)ImageLayout.General, (uint)_framebufferSize.X, (uint)_framebufferSize.Y, (uint)Format.R16G16B16A16Sfloat, outUsage);
        var tagOut = new ResourceTag(&resOut, BufferType.kBufferTypeScalingOutputColor, ResourceLifecycle.eValidUntilPresent, extentOut);

        var resSpecularMvec = new Resource(ResourceType.eTex2d, (void*)_specularMotionVectorsImage.Handle, (void*)_specularMotionVectorsImageMemory.Handle, (void*)_specularMotionVectorsImageView.Handle, (uint)ImageLayout.General, (uint)_renderSize.X, (uint)_renderSize.Y, (uint)Format.R16G16Sfloat, gBufferUsage);
        var tagSpecularMvec = new ResourceTag(&resSpecularMvec, BufferType.kBufferTypeSpecularMotionVectors, ResourceLifecycle.eValidUntilPresent, extentIn);

        var resExposure = new Resource(ResourceType.eTex2d, (void*)_exposureImage.Handle, (void*)_exposureImageMemory.Handle, (void*)_exposureImageView.Handle, (uint)ImageLayout.General, 1, 1, (uint)Format.R32Sfloat, gBufferUsage);
        var tagExposure = new ResourceTag(&resExposure, BufferType.kBufferTypeExposure, ResourceLifecycle.eValidUntilPresent, extentExposure);

        var resBiasColor = new Resource(ResourceType.eTex2d, (void*)_biasColorImage.Handle, (void*)_biasColorImageMemory.Handle, (void*)_biasColorImageView.Handle, (uint)ImageLayout.General, (uint)_renderSize.X, (uint)_renderSize.Y, (uint)Format.R16Sfloat, gBufferUsage);
        var tagBiasColor = new ResourceTag(&resBiasColor, BufferType.kBufferTypeBiasCurrentColorHint, ResourceLifecycle.eValidUntilPresent, extentIn);

        var resSpecularHitDistance = new Resource(ResourceType.eTex2d, (void*)_specularHitDistanceImage.Handle, (void*)_specularHitDistanceImageMemory.Handle, (void*)_specularHitDistanceImageView.Handle, (uint)ImageLayout.General, (uint)_renderSize.X, (uint)_renderSize.Y, (uint)Format.R16Sfloat, gBufferUsage);
        var tagSpecularHitDistance = new ResourceTag(&resSpecularHitDistance, BufferType.kBufferTypeSpecularHitDistance, ResourceLifecycle.eValidUntilPresent, extentIn);

        var resDiffuseHitNoisy = new Resource(ResourceType.eTex2d, (void*)_diffuseHitNoisyImage.Handle, (void*)_diffuseHitNoisyImageMemory.Handle, (void*)_diffuseHitNoisyImageView.Handle, (uint)ImageLayout.General, (uint)_renderSize.X, (uint)_renderSize.Y, (uint)Format.R16G16B16A16Sfloat, gBufferUsage);
        var tagDiffuseHitNoisy = new ResourceTag(&resDiffuseHitNoisy, BufferType.kBufferTypeDiffuseHitNoisy, ResourceLifecycle.eValidUntilPresent, extentIn);

        var resSpecularHitNoisy = new Resource(ResourceType.eTex2d, (void*)_specularHitNoisyImage.Handle, (void*)_specularHitNoisyImageMemory.Handle, (void*)_specularHitNoisyImageView.Handle, (uint)ImageLayout.General, (uint)_renderSize.X, (uint)_renderSize.Y, (uint)Format.R16G16B16A16Sfloat, gBufferUsage);
        var tagSpecularHitNoisy = new ResourceTag(&resSpecularHitNoisy, BufferType.kBufferTypeSpecularHitNoisy, ResourceLifecycle.eValidUntilPresent, extentIn);

        if (_useDLSS_RR)
        {
            // Update options for RR
            var opt = CreateDlssdOptions(view, viewInverse);
            DlssdAPI.SetOptions(in viewport, in opt);

            ResourceTag* pTags = stackalloc ResourceTag[15];
            pTags[0] = tagNoisy;
            pTags[1] = tagColor;
            pTags[2] = tagAlbedo;
            pTags[3] = tagSpecAlbedo;
            pTags[4] = tagNormalRoughness;
            pTags[5] = tagMvec;
            pTags[6] = tagDepthLinear;
            pTags[7] = tagDepthStandard;
            pTags[8] = tagOut;
            pTags[9] = tagSpecularMvec;
            pTags[10] = tagExposure;
            pTags[11] = tagBiasColor;
            pTags[12] = tagColorBeforeTrans;
            pTags[13] = tagDiffuseHitNoisy;
            pTags[14] = tagSpecularHitNoisy;

            StreamlineAPI.slSetTagForFrame(frameToken, &viewport, pTags, 15, (void*)cmd.Handle);

            var evalRes = DlssdAPI.Evaluate(frameToken, in viewport, (void*)cmd.Handle);
            if (evalRes != SlResult.eOk)
            {
                LogDlssRrEvalError((int)evalRes);
            }
        }
        else if (_useDLSS_SR)
        {
            // Update options for SR
            var dlssOpt = CreateDlssOptions();
            DlssAPI.SetOptions(in viewport, in dlssOpt);

            ResourceTag* pTags = stackalloc ResourceTag[10];
            pTags[0] = tagNoisy;
            pTags[1] = tagColor;
            pTags[2] = tagMvec;
            pTags[3] = tagDepthStandard;
            pTags[4] = tagOut;
            pTags[5] = tagExposure;
            pTags[6] = tagBiasColor;
            pTags[7] = tagSpecularMvec;
            pTags[8] = tagSpecularHitDistance;
            pTags[9] = tagColorBeforeTrans;

            StreamlineAPI.slSetTagForFrame(frameToken, &viewport, pTags, 10, (void*)cmd.Handle);

            var evalRes = DlssAPI.Evaluate(frameToken, in viewport, (void*)cmd.Handle);
            if (evalRes != SlResult.eOk)
            {
                LogDlssSrEvalError((int)evalRes);
            }
        }

        TransitionImageLayout(cmd, _storageImage, ImageLayout.General, ImageLayout.TransferSrcOptimal, AccessFlags2.ShaderWriteBit, AccessFlags2.TransferReadBit, PipelineStageFlags2.RayTracingShaderBitKhr | PipelineStageFlags2.ComputeShaderBit, PipelineStageFlags2.TransferBit);
    }

    private void SubmitAndPresent(CommandBuffer cmd, uint imageIndex)
    {
        _device.Vk.EndCommandBuffer(cmd);

        var waitInfo = new SemaphoreSubmitInfo { SType = StructureType.SemaphoreSubmitInfo, Semaphore = _imageAvailableSemaphores[_currentFrame], StageMask = PipelineStageFlags2.ColorAttachmentOutputBit };
        var signalInfo = new SemaphoreSubmitInfo { SType = StructureType.SemaphoreSubmitInfo, Semaphore = _renderFinishedSemaphores[imageIndex], StageMask = PipelineStageFlags2.AllCommandsBit };
        var cmdInfo = new CommandBufferSubmitInfo { SType = StructureType.CommandBufferSubmitInfo, CommandBuffer = cmd };

        var submitInfo = new SubmitInfo2 { SType = StructureType.SubmitInfo2, WaitSemaphoreInfoCount = 1, PWaitSemaphoreInfos = &waitInfo, CommandBufferInfoCount = 1, PCommandBufferInfos = &cmdInfo, SignalSemaphoreInfoCount = 1, PSignalSemaphoreInfos = &signalInfo };

        if (_currentFrameToken != null && PclAPI.SetMarkerPtr != null)
        {
            PclAPI.SetMarker(PCLMarker.eRenderSubmitEnd, _currentFrameToken);
        }

        Result result;
        lock (_device.QueueLock)
        {
            _device.Vk.QueueSubmit2(_device.GraphicsQueue, 1, in submitInfo, _inFlightFences[_currentFrame]);

            var swapchains = stackalloc[] { _swapchain.Swapchain };
            PresentInfoKHR presentInfo = new() { SType = StructureType.PresentInfoKhr, WaitSemaphoreCount = 1, PWaitSemaphores = (Semaphore*)Unsafe.AsPointer(ref _renderFinishedSemaphores[imageIndex]), SwapchainCount = 1, PSwapchains = swapchains, PImageIndices = &imageIndex };

            if (_currentFrameToken != null && PclAPI.SetMarkerPtr != null)
            {
                PclAPI.SetMarker(PCLMarker.ePresentStart, _currentFrameToken);
            }

            if (_useDLSS_RR || _useDLSS_SR)
            {
                result = (Result)StreamlineAPI.vkQueuePresentKHR((void*)_device.PresentQueue.Handle, &presentInfo);
            }
            else
            {
                result = _device.KhrSwapchain.QueuePresent(_device.PresentQueue, in presentInfo);
            }
        }

        if (_currentFrameToken != null && PclAPI.SetMarkerPtr != null)
        {
            PclAPI.SetMarker(PCLMarker.ePresentEnd, _currentFrameToken);
        }

        if (result == Result.ErrorDeviceLost) throw new Exception("Критическая ошибка: Vulkan Device Lost (видеокарта перестала отвечать)!");
        if (result is Result.ErrorOutOfDateKhr or Result.SuboptimalKhr) _framebufferResized = true;

        _currentFrame = (_currentFrame + 1) % MaxFramesInFlight;
        _prevFrameToken = _currentFrameToken;
        _currentFrameToken = null;
    }

    private void ClearColorImage(CommandBuffer cmd, Image image, float r, float g, float b, float a)
    {
        var clearColor = new ClearColorValue();
        unsafe
        {
            float* pFloat = (float*)&clearColor;
            pFloat[0] = r;
            pFloat[1] = g;
            pFloat[2] = b;
            pFloat[3] = a;
        }
        var range = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1);
        _device.Vk.CmdClearColorImage(cmd, image, ImageLayout.General, in clearColor, 1, in range);
    }

    private void TransitionImageLayout(CommandBuffer cmd, Image image, ImageLayout oldLayout, ImageLayout newLayout, AccessFlags2 srcAccess, AccessFlags2 dstAccess, PipelineStageFlags2 srcStage, PipelineStageFlags2 dstStage)
    {
        var barrier = new ImageMemoryBarrier2 { SType = StructureType.ImageMemoryBarrier2, OldLayout = oldLayout, NewLayout = newLayout, SrcQueueFamilyIndex = Vk.QueueFamilyIgnored, DstQueueFamilyIndex = Vk.QueueFamilyIgnored, Image = image, SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ColorBit, 0, 1, 0, 1), SrcAccessMask = srcAccess, DstAccessMask = dstAccess, SrcStageMask = srcStage, DstStageMask = dstStage };
        var depInfo = new DependencyInfo { SType = StructureType.DependencyInfo, ImageMemoryBarrierCount = 1, PImageMemoryBarriers = &barrier };
        _device.Vk.CmdPipelineBarrier2(cmd, in depInfo);
    }

    public void OnFramebufferResize(Vector2Int newSize)
    {
        _framebufferSize = newSize;
        _framebufferResized = true;
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;

        _device.Vk.DeviceWaitIdle(_device.Device);

        try
        {
            StreamlineAPI.slShutdown();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Streamline] Error during slShutdown");
        }

        _materialBuffer?.Dispose();
        _materialBuffer = null;

        foreach (var list in _meshesToDispose)
        {
            foreach (var mesh in list)
            {
                var a = (MeshAllocation)mesh;
                if (a.Blas.Handle != 0)
                {
                    _device.KhrAccelerationStructure.DestroyAccelerationStructure(_device.Device, a.Blas, null);
                    a.Blas = default;
                }
                mesh.Dispose();
            }
            list.Clear();
        }

        while (_pendingMeshesToDispose.TryDequeue(out var mesh))
        {
            var a = (MeshAllocation)mesh;
            if (a.Blas.Handle != 0)
            {
                _device.KhrAccelerationStructure.DestroyAccelerationStructure(_device.Device, a.Blas, null);
                a.Blas = default;
            }
            mesh.Dispose();
        }

        for (int i = 0; i < MaxFramesInFlight; i++)
        {
            if (_tlasHandles[i].Handle != 0)
            {
                _device.KhrAccelerationStructure.DestroyAccelerationStructure(_device.Device, _tlasHandles[i], null);
                _tlasHandles[i] = default;
            }

            _tlasBuffers[i]?.Dispose();
            _tlasBuffers[i] = null!;

            _tlasScratchBuffers[i]?.Dispose();
            _tlasScratchBuffers[i] = null!;

            _instancesBuffers[i]?.Dispose();
            _instancesBuffers[i] = null!;

            _instanceDataBuffers[i]?.Dispose();
            _instanceDataBuffers[i] = null!;
        }

        if (_descriptorPool.Handle != 0)
        {
            _device.Vk.DestroyDescriptorPool(_device.Device, _descriptorPool, null);
            _descriptorPool = default;
        }

        for (int i = 0; i < _cameraBuffers.Length; i++)
        {
            _cameraBuffers[i]?.Dispose();
            _cameraBuffers[i] = null!;
        }

        _meshPool?.Dispose();
        _meshPool = null!;

        _ommManager?.Dispose();
        _ommManager = null;

        DestroyImageHelper(_storageImage, _storageImageMemory, _storageImageView);

        DestroyImageHelper(_noisyColorImage, _noisyColorImageMemory, _noisyColorImageView);
        DestroyImageHelper(_normalImage, _normalImageMemory, _normalImageView);
        DestroyImageHelper(_albedoImage, _albedoImageMemory, _albedoImageView);
        DestroyImageHelper(_specularAlbedoImage, _specularAlbedoImageMemory, _specularAlbedoImageView);
        DestroyImageHelper(_motionVectorsImage, _motionVectorsImageMemory, _motionVectorsImageView);
        DestroyImageHelper(_depthImage, _depthImageMemory, _depthImageView);
        DestroyImageHelper(_linearDepthImage, _linearDepthImageMemory, _linearDepthImageView);
        DestroyImageHelper(_colorBeforeTransparencyImage, _colorBeforeTransparencyImageMemory, _colorBeforeTransparencyImageView);
        DestroyImageHelper(_specularMotionVectorsImage, _specularMotionVectorsImageMemory, _specularMotionVectorsImageView);
        DestroyImageHelper(_exposureImage, _exposureImageMemory, _exposureImageView);
        DestroyImageHelper(_biasColorImage, _biasColorImageMemory, _biasColorImageView);
        DestroyImageHelper(_specularHitDistanceImage, _specularHitDistanceImageMemory, _specularHitDistanceImageView);
        DestroyImageHelper(_diffuseHitNoisyImage, _diffuseHitNoisyImageMemory, _diffuseHitNoisyImageView);
        DestroyImageHelper(_specularHitNoisyImage, _specularHitNoisyImageMemory, _specularHitNoisyImageView);

        _pmj02bnTexture?.Dispose();
        _pmj02bnTexture = null;

        for (int i = 0; i < MaxFramesInFlight; i++)
        {
            if (_imageAvailableSemaphores[i].Handle != 0)
            {
                _device.Vk.DestroySemaphore(_device.Device, _imageAvailableSemaphores[i], null);
                _imageAvailableSemaphores[i] = default;
            }
            if (_inFlightFences[i].Handle != 0)
            {
                _device.Vk.DestroyFence(_device.Device, _inFlightFences[i], null);
                _inFlightFences[i] = default;
            }
        }

        foreach (var sem in _renderFinishedSemaphores)
        {
            if (sem.Handle != 0) _device.Vk.DestroySemaphore(_device.Device, sem, null);
        }
        Array.Clear(_renderFinishedSemaphores);

        if (_commandPool.Handle != 0)
        {
            _device.Vk.DestroyCommandPool(_device.Device, _commandPool, null);
            _commandPool = default;
        }

        _pipeline?.Dispose();
        _pipeline = null;

        _swapchain?.Dispose();
        _swapchain = null!;

        _device.Dispose();
    }

    private void UpdateJitter()
    {
        if (_useDLSS_RR || _useDLSS_SR)
        {
            var jitter = LowDiscrepancy.HaltonJitter(_slFrameIndex, 16);
            _currentJitterX = jitter.X;
            _currentJitterY = jitter.Y;
        }
        else
        {
            _currentJitterX = 0f;
            _currentJitterY = 0f;
        }
    }

    public void StartFrame()
    {
        if (_currentFrameToken != null) return;

        if (_useDLSS_RR || _useDLSS_SR || _useReflex)
        {
            FrameToken* frameToken = null;
            _slFrameIndex++;
            uint localFrameIndex = _slFrameIndex;
            StreamlineAPI.slGetNewFrameToken(&frameToken, &localFrameIndex);
            _slFrameIndex = localFrameIndex;
            _currentFrameToken = frameToken;
        }

        if (_useReflex && _reflexMode != ReflexMode.eOff && _currentFrameToken != null && ReflexAPI.SleepPtr != null)
        {
            ReflexAPI.Sleep(_currentFrameToken);
        }
    }

    public void CycleReflexMode()
    {
        if (!_useReflex) return;

        _reflexMode = _reflexMode switch
        {
            ReflexMode.eLowLatencyWithBoost => ReflexMode.eLowLatency,
            ReflexMode.eLowLatency => ReflexMode.eOff,
            _ => ReflexMode.eLowLatencyWithBoost
        };

        var reflexOpt = ReflexOptions.Create();
        reflexOpt.Mode = (uint)_reflexMode;
        reflexOpt.UseMarkersToOptimize = 0;
        ReflexAPI.SetOptions(in reflexOpt);

        _logger.LogInformation("[Reflex] Режим переключен на: {Mode}", _reflexMode);
    }

    /// <inheritdoc/>
    public void RequestHistoryReset()
    {
        _resetHistoryRequested = true;
    }

    public bool GetPredictedCamera(out Matrix4x4 view, out Matrix4x4 proj)
    {
        view = Matrix4x4.Identity;
        proj = Matrix4x4.Identity;
        return false;
    }

    public void SetSimulationStart()
    {
        if (_currentFrameToken != null && PclAPI.SetMarkerPtr != null)
        {
            PclAPI.SetMarker(PCLMarker.eSimulationStart, _currentFrameToken);
        }
    }

    public void SetSimulationEnd()
    {
        if (_currentFrameToken != null && PclAPI.SetMarkerPtr != null)
        {
            PclAPI.SetMarker(PCLMarker.eSimulationEnd, _currentFrameToken);
        }
    }

}