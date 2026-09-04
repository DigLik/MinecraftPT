using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Уникальный дескриптор видового экрана (Viewport Handle) для раздельной настройки и вычисления плагинов Streamline.
/// Позволяет разделять состояние рендеринга нескольких камер или viewport'ов в рамках одного кадра.
/// Соответствует структуре <c>sl::ViewportHandle</c> (GUID: <c>{171B6435-9B3C-4FC8-9994-FBE52569AAA4}</c>, версия 1) из заголовочного файла <c>sl_core_types.h</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct ViewportHandle
{
    private static readonly StructType ViewportHandleTypeId = new(0x171b6435, 0x9b3c, 0x4fc8, 0x99, 0x94, 0xfb, 0xe5, 0x25, 0x69, 0xaa, 0xa4);

    /// <summary>
    /// Базовый заголовок структуры Streamline (версия 1).
    /// </summary>
    public BaseStructure Base;

    /// <summary>
    /// Числовой целочисленный идентификатор видового экрана (обычно 0 или 1 для основного видового экрана).
    /// </summary>
    public uint Value;

    /// <summary>
    /// Создает новый дескриптор видового экрана с заданным числовым значением.
    /// </summary>
    /// <param name="val">Целочисленный идентификатор видового экрана.</param>
    public ViewportHandle(uint val)
    {
        Base = new BaseStructure(ViewportHandleTypeId, 1);
        Value = val;
    }
}
