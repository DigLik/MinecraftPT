using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Уникальный маркёр/дескриптор кадра (Frame Token) для покадрового отслеживания ресурсов и вызовов технологий Streamline.
/// Получается через вызов метода <see cref="StreamlineAPI.slGetNewFrameToken"/>.
/// Соответствует <c>sl::FrameToken</c> (GUID: <c>{830A0F35-DB84-4171-A804-59B206499B18}</c>) из заголовочного файла <c>sl_core_types.h</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct FrameToken
{
    /// <summary>
    /// Базовый заголовок структуры Streamline (версия 1).
    /// </summary>
    public BaseStructure Base;
}
