using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Описание нативного графического ресурса GPU (текстуры, буфера или представления) для NVIDIA Streamline SDK.
/// Передается в теги ресурсов (<see cref="ResourceTag"/>).
/// Соответствует структуре <c>sl::Resource</c> (GUID: <c>{3A9D70CF-2418-4B72-8391-13F8721C7261}</c>, версия 1) из заголовочного файла <c>sl_core_types.h</c>.
/// </summary>
/// <remarks>
/// <b>Обязательные поля:</b>
/// <list type="bullet">
///   <item><description><see cref="Type"/> и <see cref="Native"/> — обязательны всегда для всех графических API.</description></item>
///   <item><description><see cref="State"/> — обязательно всегда (состояние ресурса <c>VkImageLayout</c> или <c>D3D12_RESOURCE_STATES</c> в момент использования).</description></item>
///   <item><description><see cref="Memory"/>, <see cref="View"/>, <see cref="NativeFormat"/>, <see cref="Usage"/> — обязательны при использовании Vulkan API.</description></item>
/// </list>
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct Resource
{
    private static readonly StructType ResourceTypeId = new(0x3a9d70cf, 0x2418, 0x4b72, 0x83, 0x91, 0x13, 0xf8, 0x72, 0x1c, 0x72, 0x61);

    /// <summary>
    /// Базовый заголовок структуры Streamline (версия 1).
    /// </summary>
    public BaseStructure Base;

    /// <summary>
    /// Тип графического ресурса (<see cref="ResourceType"/>), например <see cref="ResourceType.eTex2d"/> или <see cref="ResourceType.eBuffer"/>.
    /// </summary>
    public ResourceType Type;

    /// <summary>
    /// Указатель на нативный объект ресурса (<c>VkImage</c>, <c>VkBuffer</c>, <c>ID3D12Resource*</c> или <c>ID3D11Resource*</c>).
    /// </summary>
    public void* Native;

    /// <summary>
    /// Указатель на выделенную память устройства (<c>VkDeviceMemory</c>) для Vulkan (или <see langword="null"/> для D3D).
    /// </summary>
    public void* Memory;

    /// <summary>
    /// Указатель на представление ресурса (<c>VkImageView</c> / <c>VkBufferView</c>) для Vulkan.
    /// </summary>
    public void* View;

    /// <summary>
    /// Текущее состояние ресурса при передаче в очередь (<c>VkImageLayout</c> для Vulkan или <c>D3D12_RESOURCE_STATES</c> для D3D12).
    /// </summary>
    public uint State;

    /// <summary>
    /// Ширина текстуры или размер буфера в пикселях/байтах.
    /// </summary>
    public uint Width;

    /// <summary>
    /// Высота текстуры в пикселях (или 1 для линейного буфера).
    /// </summary>
    public uint Height;

    /// <summary>
    /// Нативный формат пикселей ресурса (<c>VkFormat</c> или <c>DXGI_FORMAT</c>).
    /// </summary>
    public uint NativeFormat;

    /// <summary>
    /// Количество уровней детализации мипмапов (mip-map levels).
    /// </summary>
    public uint MipLevels;

    /// <summary>
    /// Количество слоев массива текстур (array layers).
    /// </summary>
    public uint ArrayLayers;

    /// <summary>
    /// Виртуальный адрес ресурса в памяти GPU (при наличии).
    /// </summary>
    public ulong GpuVirtualAddress;

    /// <summary>
    /// Флаги создания ресурса (<c>VkImageCreateFlags</c>).
    /// </summary>
    public uint Flags;

    /// <summary>
    /// Флаги назначения/использования ресурса (<c>VkImageUsageFlags</c>).
    /// </summary>
    public uint Usage;

    /// <summary>
    /// Зарезервировано для внутреннего использования Streamline.
    /// </summary>
    public uint Reserved;

    /// <summary>
    /// Инициализирует описание ресурса Vulkan для текстуры или буфера.
    /// </summary>
    /// <param name="type">Тип ресурса.</param>
    /// <param name="native">Указатель на нативный объект ресурса (<c>VkImage</c> или <c>VkBuffer</c>).</param>
    /// <param name="mem">Указатель на выделенную память (<c>VkDeviceMemory</c>).</param>
    /// <param name="view">Указатель на представление (<c>VkImageView</c>).</param>
    /// <param name="state">Текущий лэйаут изображения (<c>VkImageLayout</c>).</param>
    /// <param name="width">Ширина в пикселях.</param>
    /// <param name="height">Высота в пикселях.</param>
    /// <param name="format">Нативный формат (<c>VkFormat</c>).</param>
    /// <param name="usage">Флаги использования (<c>VkImageUsageFlags</c>).</param>
    public Resource(ResourceType type, void* native, void* mem, void* view, uint state, uint width, uint height, uint format, uint usage)
    {
        Base = new BaseStructure(ResourceTypeId, 1);
        Type = type;
        Native = native;
        Memory = mem;
        View = view;
        State = state;
        Width = width;
        Height = height;
        NativeFormat = format;
        MipLevels = 1;
        ArrayLayers = 1;
        GpuVirtualAddress = 0;
        Flags = 0;
        Usage = usage;
        Reserved = 0;
    }
}
