using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Прямоугольная область (экстент) на ресурсе или видовом экране в пикселях.
/// Используется в тегировании ресурсов (<see cref="ResourceTag"/>) для ограничения используемого региона текстуры.
/// Соответствует структуре <c>sl::Extent</c> из заголовочного файла <c>sl_consts.h</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct Extent
{
    /// <summary>
    /// Координата верхней границы области в пикселях (Top offset).
    /// </summary>
    public uint Top;

    /// <summary>
    /// Координата левой границы области в пикселях (Left offset).
    /// </summary>
    public uint Left;

    /// <summary>
    /// Ширина прямоугольной области в пикселях (Width).
    /// </summary>
    public uint Width;

    /// <summary>
    /// Высота прямоугольной области в пикселях (Height).
    /// </summary>
    public uint Height;

    /// <summary>
    /// Инициализирует экстент с нулевыми координатами левого верхнего угла (0, 0) и заданными размерами.
    /// </summary>
    /// <param name="w">Ширина в пикселях.</param>
    /// <param name="h">Высота в пикселях.</param>
    public Extent(uint w, uint h)
    {
        Top = 0;
        Left = 0;
        Width = w;
        Height = h;
    }

    /// <summary>
    /// Инициализирует экстент с явным указанием положения и габаритов прямоугольника.
    /// </summary>
    /// <param name="top">Верхняя координата смещения.</param>
    /// <param name="left">Левая координата смещения.</param>
    /// <param name="width">Ширина в пикселях.</param>
    /// <param name="height">Высота в пикселях.</param>
    public Extent(uint top, uint left, uint width, uint height)
    {
        Top = top;
        Left = left;
        Width = width;
        Height = height;
    }
}
