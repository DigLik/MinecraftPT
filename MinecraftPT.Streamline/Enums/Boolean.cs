namespace MinecraftPT.Streamline;

/// <summary>
/// Троичное логическое значение Streamline SDK для параметров, не имеющих однозначного значения по умолчанию.
/// Соответствует перечислению <c>sl::Boolean</c> (базовый тип <c>char</c>) из заголовочного файла <c>sl_consts.h</c> NVIDIA Streamline SDK.
/// </summary>
public enum Boolean : byte
{
    /// <summary>
    /// Логическая ложь (<c>false</c> / 0).
    /// </summary>
    eFalse = 0,

    /// <summary>
    /// Логическая истина (<c>true</c> / 1).
    /// </summary>
    eTrue = 1,

    /// <summary>
    /// Недействительное/неопределённое состояние (<c>invalid</c> / 2). Используется, когда хост-приложение не предоставляет значение.
    /// </summary>
    eInvalid = 2
}
