namespace MinecraftPT.Streamline;

/// <summary>
/// Типы нативных графических ресурсов, передаваемых во фреймворк NVIDIA Streamline SDK.
/// Соответствует перечислению <c>sl::ResourceType</c> (базовый тип <c>char</c>) из заголовочного файла <c>sl_core_types.h</c>.
/// </summary>
public enum ResourceType : sbyte
{
    /// <summary>
    /// Двумерная текстура/изображение (<c>VkImage</c> / <c>ID3D12Resource</c> / <c>ID3D11Texture2D</c>).
    /// </summary>
    eTex2d = 0,

    /// <summary>
    /// Линейный буфер GPU (<c>VkBuffer</c> / <c>ID3D12Resource</c> / <c>ID3D11Buffer</c>).
    /// </summary>
    eBuffer,

    /// <summary>
    /// Очередь команд (<c>VkQueue</c> / <c>ID3D12CommandQueue</c>).
    /// </summary>
    eCommandQueue,

    /// <summary>
    /// Буфер или список команд (<c>VkCommandBuffer</c> / <c>ID3D12GraphicsCommandList</c>).
    /// </summary>
    eCommandBuffer,

    /// <summary>
    /// Пул командных буферов (<c>VkCommandPool</c> / <c>ID3D12CommandAllocator</c>).
    /// </summary>
    eCommandPool,

    /// <summary>
    /// Объект синхронизации/фенс GPU (<c>VkFence</c> / <c>ID3D12Fence</c>).
    /// </summary>
    eFence,

    /// <summary>
    /// Цепочка буферов отображения (Swapchain: <c>VkSwapchainKHR</c> / <c>IDXGISwapChain</c>).
    /// </summary>
    eSwapchain,

    /// <summary>
    /// Фенс хоста для синхронизации CPU-GPU.
    /// </summary>
    eHostFence,

    /// <summary>
    /// Неизвестный ресурс (известно только приведение к базовому интерфейсу IUnknown).
    /// </summary>
    eUnknown,

    /// <summary>
    /// Служебное значение количества типов ресурсов.
    /// </summary>
    eCount
}
