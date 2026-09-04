using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Уникальный 128-битный идентификатор типа структуры Streamline (аналог GUID/UUID).
/// Позволяет плагинам безопасно идентифицировать тип данных в цепочке связанных структур <see cref="BaseStructure"/>.
/// Соответствует <c>sl::StructType</c> из заголовочного файла <c>sl_struct.h</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct StructType
{
    /// <summary>
    /// Первая 32-битная часть идентификатора GUID.
    /// </summary>
    public uint Data1;

    /// <summary>
    /// Вторая 16-битная часть идентификатора GUID.
    /// </summary>
    public ushort Data2;

    /// <summary>
    /// Третья 16-битная часть идентификатора GUID.
    /// </summary>
    public ushort Data3;

    /// <summary>
    /// Четвертая 8-байтовая часть идентификатора GUID (8 отдельных байт).
    /// </summary>
    public fixed byte Data4[8];

    /// <summary>
    /// Инициализирует идентификатор типа структуры всеми компонентами GUID.
    /// </summary>
    /// <param name="d1">Первая 32-битная компонента.</param>
    /// <param name="d2">Вторая 16-битная компонента.</param>
    /// <param name="d3">Третья 16-битная компонента.</param>
    /// <param name="b0">Байт 0 массива Data4.</param>
    /// <param name="b1">Байт 1 массива Data4.</param>
    /// <param name="b2">Байт 2 массива Data4.</param>
    /// <param name="b3">Байт 3 массива Data4.</param>
    /// <param name="b4">Байт 4 массива Data4.</param>
    /// <param name="b5">Байт 5 массива Data4.</param>
    /// <param name="b6">Байт 6 массива Data4.</param>
    /// <param name="b7">Байт 7 массива Data4.</param>
    public StructType(uint d1, ushort d2, ushort d3, byte b0, byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7)
    {
        Data1 = d1;
        Data2 = d2;
        Data3 = d3;
        fixed (byte* p = Data4)
        {
            p[0] = b0; p[1] = b1; p[2] = b2; p[3] = b3;
            p[4] = b4; p[5] = b5; p[6] = b6; p[7] = b7;
        }
    }

    /// <summary>
    /// Инициализирует идентификатор типа структуры из компонентов GUID и диапазона байт.
    /// </summary>
    /// <param name="d1">Первая 32-битная компонента.</param>
    /// <param name="d2">Вторая 16-битная компонента.</param>
    /// <param name="d3">Третья 16-битная компонента.</param>
    /// <param name="d4">Диапазон байт (размером 8 байт) для Data4.</param>
    public StructType(uint d1, ushort d2, ushort d3, ReadOnlySpan<byte> d4)
    {
        Data1 = d1;
        Data2 = d2;
        Data3 = d3;
        fixed (byte* p = Data4)
        {
            for (int i = 0; i < 8 && i < d4.Length; i++) p[i] = d4[i];
        }
    }
}
