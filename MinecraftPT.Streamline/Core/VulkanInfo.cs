using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Информация о контексте Vulkan API (устройство, инстанс, физическое устройство и параметры очередей команд).
/// Передается в метод <see cref="StreamlineAPI.slSetVulkanInfo"/> сразу после создания устройства Vulkan.
/// Требуется при ручной интеграции и явном создании очередей без использования прокси-библиотек Streamline.
/// Соответствует структуре <c>sl::VulkanInfo</c> (GUID: <c>{0EED6FD5-82CD-43A9-BDB5-47A5BA2F45D6}</c>, версия 3) из заголовочного файла <c>sl_helpers_vk.h</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct VulkanInfo
{
    private static readonly StructType VulkanInfoTypeId = new(0xeed6fd5, 0x82cd, 0x43a9, 0xbd, 0xb5, 0x47, 0xa5, 0xba, 0x2f, 0x45, 0xd6);

    /// <summary>
    /// Базовый заголовок структуры Streamline (версия 3).
    /// </summary>
    public BaseStructure Base;

    /// <summary>
    /// Логическое устройство Vulkan (<c>VkDevice</c>).
    /// </summary>
    public void* Device;

    /// <summary>
    /// Экземпляр Vulkan (<c>VkInstance</c>).
    /// </summary>
    public void* Instance;

    /// <summary>
    /// Физическое графическое устройство Vulkan (<c>VkPhysicalDevice</c>).
    /// </summary>
    public void* PhysicalDevice;

    /// <summary>
    /// Начальный индекс вычислительной очереди (Compute Queue), с которого Streamline создает свои внутренние очереди.
    /// </summary>
    public uint ComputeQueueIndex;

    /// <summary>
    /// Индекс семейства вычислительной очереди (Compute Queue Family Index).
    /// </summary>
    public uint ComputeQueueFamily;

    /// <summary>
    /// Начальный индекс графической очереди (Graphics Queue).
    /// </summary>
    public uint GraphicsQueueIndex;

    /// <summary>
    /// Индекс семейства графической очереди (Graphics Queue Family Index).
    /// </summary>
    public uint GraphicsQueueFamily;

    /// <summary>
    /// Начальный индекс аппаратной очереди оптического потока (Optical Flow Queue) для DLSS Frame Generation.
    /// </summary>
    public uint OpticalFlowQueueIndex;

    /// <summary>
    /// Индекс семейства очереди оптического потока (Optical Flow Queue Family Index).
    /// </summary>
    public uint OpticalFlowQueueFamily;

    /// <summary>
    /// Использовать ли нативный режим оптического потока (C++ <c>bool</c>, 1 байт).
    /// </summary>
    public byte UseNativeOpticalFlowMode;

    /// <summary>
    /// Байты выравнивания для 4-байтовой границы полей флагов.
    /// </summary>
    private byte pad0, pad1, pad2;

    /// <summary>
    /// Флаги создания вычислительной очереди (<c>VkDeviceQueueCreateFlags</c>).
    /// </summary>
    public uint ComputeQueueCreateFlags;

    /// <summary>
    /// Флаги создания графической очереди (<c>VkDeviceQueueCreateFlags</c>).
    /// </summary>
    public uint GraphicsQueueCreateFlags;

    /// <summary>
    /// Флаги создания очереди оптического потока (<c>VkDeviceQueueCreateFlags</c>).
    /// </summary>
    public uint OpticalFlowQueueCreateFlags;

    /// <summary>
    /// Создает инициализированный экземпляр структуры <see cref="VulkanInfo"/> версии 3 с нулевыми полями по умолчанию.
    /// </summary>
    /// <returns>Новая структура <see cref="VulkanInfo"/> версии 3.</returns>
    public static VulkanInfo Create()
    {
        var info = new VulkanInfo();
        info.Base = new BaseStructure(VulkanInfoTypeId, 3);
        info.Device = null;
        info.Instance = null;
        info.PhysicalDevice = null;
        info.ComputeQueueIndex = 0;
        info.ComputeQueueFamily = 0;
        info.GraphicsQueueIndex = 0;
        info.GraphicsQueueFamily = 0;
        info.OpticalFlowQueueIndex = 0;
        info.OpticalFlowQueueFamily = 0;
        info.UseNativeOpticalFlowMode = 0;
        info.ComputeQueueCreateFlags = 0;
        info.GraphicsQueueCreateFlags = 0;
        info.OpticalFlowQueueCreateFlags = 0;
        return info;
    }
}
