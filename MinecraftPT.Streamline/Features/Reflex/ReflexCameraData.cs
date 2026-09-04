using System.Numerics;
using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Матричные данные положения и проекции камеры для алгоритмов прогнозирования Reflex.
/// Передаются через метод <see cref="ReflexAPI.SetCameraData"/>.
/// Соответствует структуре <c>sl::ReflexCameraData</c> (GUID: <c>{C83CBB02-B4E2-4260-9CA2-D0C3DE3A9684}</c>, версия 1) из заголовочного файла <c>sl_reflex.h</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct ReflexCameraData
{
    private static readonly StructType ReflexCameraDataTypeId = new(0xc83cbb02, 0xb4e2, 0x4260, 0x9c, 0xa2, 0xd0, 0xc3, 0xde, 0x3a, 0x96, 0x84);

    /// <summary>
    /// Базовый заголовок структуры Streamline (версия 1).
    /// </summary>
    public BaseStructure Base;

    /// <summary>
    /// Матрица преобразования из мирового пространства в пространство вида камеры (World to View Matrix).
    /// </summary>
    public Matrix4x4 WorldToViewMatrix;

    /// <summary>
    /// Матрица преобразования из пространства вида в пространство отсечения (View to Clip Matrix).
    /// </summary>
    public Matrix4x4 ViewToClipMatrix;

    /// <summary>
    /// Матрица преобразования из мирового пространства в пространство вида предыдущего отрендеренного кадра.
    /// </summary>
    public Matrix4x4 PrevRenderedWorldToViewMatrix;

    /// <summary>
    /// Матрица проекции (View to Clip) предыдущего отрендеренного кадра.
    /// </summary>
    public Matrix4x4 PrevRenderedViewToClipMatrix;

    /// <summary>
    /// Создает инициализированную структуру <see cref="ReflexCameraData"/> версии 1 со стандартными единичными матрицами.
    /// </summary>
    /// <returns>Новая структура <see cref="ReflexCameraData"/>.</returns>
    public static ReflexCameraData Create()
    {
        var d = new ReflexCameraData();
        d.Base = new BaseStructure(ReflexCameraDataTypeId, 1);
        d.WorldToViewMatrix = Matrix4x4.Identity;
        d.ViewToClipMatrix = Matrix4x4.Identity;
        d.PrevRenderedWorldToViewMatrix = Matrix4x4.Identity;
        d.PrevRenderedViewToClipMatrix = Matrix4x4.Identity;
        return d;
    }
}
