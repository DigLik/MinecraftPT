using System.Numerics;
using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Экстраполированные (предсказанные) матричные данные положения камеры для минимизации воспринимаемой задержки обзора.
/// Возвращаются методом <see cref="ReflexAPI.GetPredictedCameraData"/>.
/// Соответствует структуре <c>sl::ReflexPredictedCameraData</c> (GUID: <c>{8B960090-A807-4C85-B02F-1069950D066C}</c>, версия 1) из заголовочного файла <c>sl_reflex.h</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct ReflexPredictedCameraData
{
    private static readonly StructType ReflexPredictedCameraDataTypeId = new(0x8b960090, 0xa807, 0x4c85, 0xb0, 0x2f, 0x10, 0x69, 0x95, 0x0d, 0x06, 0x6c);

    /// <summary>
    /// Базовый заголовок структуры Streamline (версия 1).
    /// </summary>
    public BaseStructure Base;

    /// <summary>
    /// Предсказанная матрица преобразования из мирового пространства в пространство вида камеры на момент вывода на экран.
    /// </summary>
    public Matrix4x4 PredictedWorldToViewMatrix;

    /// <summary>
    /// Предсказанная матрица проекции камеры (View to Clip Matrix).
    /// </summary>
    public Matrix4x4 PredictedViewToClipMatrix;

    /// <summary>
    /// Создает инициализированную структуру <see cref="ReflexPredictedCameraData"/> версии 1.
    /// </summary>
    /// <returns>Новая структура <see cref="ReflexPredictedCameraData"/>.</returns>
    public static ReflexPredictedCameraData Create()
    {
        var d = new ReflexPredictedCameraData();
        d.Base = new BaseStructure(ReflexPredictedCameraDataTypeId, 1);
        d.PredictedWorldToViewMatrix = Matrix4x4.Identity;
        d.PredictedViewToClipMatrix = Matrix4x4.Identity;
        return d;
    }
}
