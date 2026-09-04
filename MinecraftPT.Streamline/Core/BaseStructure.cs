using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Базовый заголовок всех расширяемых структур NVIDIA Streamline SDK.
/// Реализует механизм связного списка (chaining) структур для расширения API без нарушения обратной бинарной совместимости (ABI).
/// Соответствует <c>sl::BaseStructure</c> из заголовочного файла <c>sl_struct.h</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct BaseStructure
{
    /// <summary>
    /// Опциональный указатель на следующую структуру в цепочке связанных структур Streamline (или <see langword="null"/>).
    /// </summary>
    public BaseStructure* Next;

    /// <summary>
    /// Обязательный уникальный идентификатор типа структуры (<c>StructType</c> / GUID).
    /// </summary>
    public StructType StructType;

    /// <summary>
    /// Обязательный номер версии структуры (<c>Version</c>), определяющий набор доступных полей.
    /// </summary>
    public nuint StructVersion;

    /// <summary>
    /// Инициализирует базовый заголовок структуры Streamline заданным типом и версией.
    /// </summary>
    /// <param name="t">Уникальный идентификатор типа структуры.</param>
    /// <param name="v">Версия структуры (например, 1, 2 или 3).</param>
    public BaseStructure(StructType t, uint v)
    {
        Next = null;
        StructType = t;
        StructVersion = (nuint)v;
    }
}
