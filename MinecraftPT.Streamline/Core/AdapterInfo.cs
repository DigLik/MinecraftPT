using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Информация о графическом адаптере (адаптер DXGI или физическое устройство Vulkan VkPhysicalDevice).
/// Передается в метод <see cref="StreamlineAPI.slIsFeatureSupported"/> для проверки совместимости технологий с конкретным GPU.
/// Соответствует структуре <c>sl::AdapterInfo</c> (GUID: <c>{0677315F-A746-4492-9F42-CB6142C9C3D4}</c>) из заголовочного файла <c>sl_core_types.h</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct AdapterInfo
{
    private static readonly StructType AdapterInfoTypeId = new(
        0x0677315f, 0xa746, 0x4492,
        0x9f, 0x42, 0xcb, 0x61, 0x42, 0xc9, 0xc3, 0xd4);

    /// <summary>
    /// Базовый заголовок расширяемой структуры Streamline (тип структуры и версия 1).
    /// </summary>
    public BaseStructure Base;

    /// <summary>
    /// Указатель на локально уникальный идентификатор устройства LUID (для DirectX 11 / DirectX 12 / DXGI).
    /// </summary>
    public byte* DeviceLUID;

    /// <summary>
    /// Размер буфера идентификатора LUID в байтах.
    /// </summary>
    public uint DeviceLUIDSizeInBytes;

    /// <summary>
    /// Явное выравнивание для соблюдения 8-байтовой границы указателя Vulkan PhysicalDevice.
    /// </summary>
    private uint _pad0;

    /// <summary>
    /// Дескриптор/указатель на физическое устройство Vulkan (<c>VkPhysicalDevice</c>).
    /// При указании ненулевого значения поле <see cref="DeviceLUID"/> игнорируется.
    /// </summary>
    public void* VkPhysicalDevice;

    /// <summary>
    /// Инициализирует структуру <see cref="AdapterInfo"/> для физического устройства Vulkan.
    /// </summary>
    /// <param name="vkPhysicalDevice">Указатель или дескриптор нативного объекта <c>VkPhysicalDevice</c>.</param>
    public AdapterInfo(void* vkPhysicalDevice)
    {
        Base = new BaseStructure(AdapterInfoTypeId, 1);
        DeviceLUID = null;
        DeviceLUIDSizeInBytes = 0;
        _pad0 = 0;
        VkPhysicalDevice = vkPhysicalDevice;
    }

    /// <summary>
    /// Инициализирует структуру <see cref="AdapterInfo"/> для адаптера DXGI / D3D12 по LUID.
    /// </summary>
    /// <param name="deviceLUID">Указатель на байтовый массив идентификатора LUID устройства.</param>
    /// <param name="deviceLUIDSizeInBytes">Размер буфера LUID в байтах.</param>
    /// <param name="vkPhysicalDevice">Опциональный дескриптор физического устройства Vulkan.</param>
    public AdapterInfo(byte* deviceLUID, uint deviceLUIDSizeInBytes, void* vkPhysicalDevice = null)
    {
        Base = new BaseStructure(AdapterInfoTypeId, 1);
        DeviceLUID = deviceLUID;
        DeviceLUIDSizeInBytes = deviceLUIDSizeInBytes;
        _pad0 = 0;
        VkPhysicalDevice = vkPhysicalDevice;
    }
}
