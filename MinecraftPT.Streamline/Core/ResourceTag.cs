using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Тег привязки графического ресурса GPU к семантическому типу буфера Streamline SDK.
/// Передается в метод <see cref="StreamlineAPI.slSetTagForFrame"/> для уведомления плагинов (DLSS SR, DLSS-RR)
/// о назначении текстуры (глубина, векторы движения, цвет, альбедо и др.).
/// Соответствует структуре <c>sl::ResourceTag</c> (GUID: <c>{4C6A5AAD-B445-496C-87FF-1AF3845BE653}</c>, версия 1) из заголовочного файла <c>sl_core_types.h</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct ResourceTag
{
    private static readonly StructType ResourceTagTypeId = new(0x4c6a5aad, 0xb445, 0x496c, 0x87, 0xff, 0x1a, 0xf3, 0x84, 0x5b, 0xe6, 0x53);

    /// <summary>
    /// Базовый заголовок структуры Streamline (версия 1).
    /// </summary>
    public BaseStructure Base;

    /// <summary>
    /// Указатель на структуру описания нативного ресурса (<see cref="Resource"/>).
    /// </summary>
    public Resource* Resource;

    /// <summary>
    /// Семантический тип буфера Streamline (<see cref="BufferType"/>), например буфер глубины или векторов движения.
    /// </summary>
    public BufferType Type;

    /// <summary>
    /// Жизненный цикл тегированного ресурса (<see cref="ResourceLifecycle"/>). Рекомендуется <see cref="ResourceLifecycle.eValidUntilPresent"/>.
    /// </summary>
    public ResourceLifecycle Lifecycle;

    /// <summary>
    /// Прямоугольная область (экстент) на ресурсе, подлежащая обработке.
    /// </summary>
    public Extent Extent;

    /// <summary>
    /// Инициализирует тег ресурса с указанием ресурса, типа буфера, жизненного цикла и экстента.
    /// </summary>
    /// <param name="r">Указатель на структуру описания графического ресурса.</param>
    /// <param name="t">Семантический тип буфера.</param>
    /// <param name="l">Жизненный цикл ресурса в конвейере кадра.</param>
    /// <param name="e">Используемая прямоугольная область на ресурсе.</param>
    public ResourceTag(Resource* r, BufferType t, ResourceLifecycle l, Extent e)
    {
        Base = new BaseStructure(ResourceTagTypeId, 1);
        Resource = r;
        Type = t;
        Lifecycle = l;
        Extent = e;
    }
}
