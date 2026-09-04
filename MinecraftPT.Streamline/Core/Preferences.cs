using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Глобальные параметры инициализации библиотеки NVIDIA Streamline SDK.
/// Передаются в метод <see cref="StreamlineAPI.EarlyInitStreamline"/> / <see cref="StreamlineNative.slInit"/> при старте приложения.
/// Соответствует структуре <c>sl::Preferences</c> (GUID: <c>{1CA10965-BF8E-432B-8DA1-6716D879FB14}</c>, версия 1) из заголовочного файла <c>sl_core_types.h</c>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct Preferences
{
    private static readonly StructType PreferencesTypeId = new(0x1ca10965, 0xbf8e, 0x432b, 0x8d, 0xa1, 0x67, 0x16, 0xd8, 0x79, 0xfb, 0x14);

    /// <summary>
    /// Базовый заголовок структуры Streamline (версия 1).
    /// </summary>
    public BaseStructure Base;

    /// <summary>
    /// Показывать ли системное консольное окно отладки в нерелизных сборках (0 — скрыть, 1 — показать).
    /// </summary>
    public byte ShowConsole;

    /// <summary>
    /// Выравнивание для соблюдения границы полей.
    /// </summary>
    private byte pad0, pad1, pad2;

    /// <summary>
    /// Уровень системного логирования Streamline (<see cref="StreamlineLogLevel"/>).
    /// </summary>
    public uint LogLevel;

    /// <summary>
    /// Массив строковых путей (UTF-16 <c>wchar_t*</c>) к директориям поиска плагинов Streamline (первый путь имеет наивысший приоритет).
    /// </summary>
    public char** PathsToPlugins;

    /// <summary>
    /// Количество путей в массиве <see cref="PathsToPlugins"/>.
    /// </summary>
    public uint NumPathsToPlugins;

    /// <summary>
    /// Байт выравнивания для 64-битного указателя.
    /// </summary>
    private uint pad3;

    /// <summary>
    /// Абсолютный путь к каталогу хранения логов и дампов данных (UTF-16 <c>wchar_t*</c>). Передайте <see langword="null"/> для отключения записи логов на диск.
    /// </summary>
    public char* PathToLogsAndData;

    /// <summary>
    /// Указатель на функцию обратного вызова аллокации ресурсов хоста (опционально).
    /// </summary>
    public void* AllocateCallback;

    /// <summary>
    /// Указатель на функцию обратного вызова освобождения ресурсов хоста (опционально).
    /// </summary>
    public void* ReleaseCallback;

    /// <summary>
    /// Указатель на функцию обратного вызова для перехвата сообщений лога Streamline (<c>delegate* unmanaged[Cdecl]&lt;uint, byte*, void&gt;</c>).
    /// </summary>
    public void* LogMessageCallback;

    /// <summary>
    /// Битовая маска флагов конфигурации Streamline (<c>PreferenceFlags</c>):
    /// <list type="bullet">
    ///   <item><description><c>0x01</c> (<c>eDisableCLStateTracking</c>) — отключает внутреннее отслеживание состояний командного списка GPU.</description></item>
    ///   <item><description><c>0x02</c> (<c>eDisableDebugText</c>) — отключает вывод отладочного текста на экран.</description></item>
    ///   <item><description><c>0x04</c> (<c>eUseManualHooking</c>) — режим ручной интеграции хуков.</description></item>
    ///   <item><description><c>0x08</c> (<c>eAllowOTA</c>) — разрешает поиск онлайн-обновлений моделей OTA.</description></item>
    ///   <item><description><c>0x10</c> (<c>eBypassOSVersionCheck</c>) — пропуск проверки версии ОС.</description></item>
    ///   <item><description><c>0x20</c> (<c>eUseDXGIFactoryProxy</c>) — использование DXGI Factory Proxy.</description></item>
    ///   <item><description><c>0x40</c> (<c>eLoadDownloadedPlugins</c>) — загрузка загруженных OTA-плагинов.</description></item>
    ///   <item><description><c>0x80</c> (<c>eUseFrameBasedResourceTagging</c>) — использование покадрового тегирования ресурсов (<c>slSetTagForFrame</c>).</description></item>
    /// </list>
    /// </summary>
    public ulong Flags;

    /// <summary>
    /// Указатель на массив идентификаторов технологий (<see cref="Feature"/>), подлежащих автоматической загрузке.
    /// </summary>
    public uint* FeaturesToLoad;

    /// <summary>
    /// Количество технологий в массиве <see cref="FeaturesToLoad"/>.
    /// </summary>
    public uint NumFeaturesToLoad;

    /// <summary>
    /// Уникальный идентификатор приложения (Application ID), выданный NVIDIA.
    /// </summary>
    public uint ApplicationId;

    /// <summary>
    /// Тип используемого игрового движка (0 = <c>eCustom</c>, 1 = <c>eUnreal</c>, 2 = <c>eUnity</c>).
    /// </summary>
    public uint Engine;

    /// <summary>
    /// Версия игрового движка в формате строки ANSI (опционально).
    /// </summary>
    public byte* EngineVersion;

    /// <summary>
    /// Идентификатор проекта в формате GUID ANSI (опционально).
    /// </summary>
    public byte* ProjectId;

    /// <summary>
    /// Используемый графический API (0 = <c>eD3D11</c>, 1 = <c>eD3D12</c>, 2 = <c>eVulkan</c>).
    /// </summary>
    public uint RenderAPI;

    /// <summary>
    /// Байт выравнивания.
    /// </summary>
    private uint pad4;

    /// <summary>
    /// Создает инициализированную структуру <see cref="Preferences"/> с рекомендуемыми настройками для Vulkan.
    /// </summary>
    /// <returns>Экземпляр <see cref="Preferences"/> с заполненным базовым заголовком и флагами по умолчанию.</returns>
    public static Preferences Create()
    {
        var p = new Preferences();
        p.Base = new BaseStructure(PreferencesTypeId, 1);
        p.ShowConsole = 0;
        p.LogLevel = (uint)StreamlineLogLevel.eDefault;
        p.PathsToPlugins = null;
        p.NumPathsToPlugins = 0;
        p.PathToLogsAndData = null;
        p.AllocateCallback = null;
        p.ReleaseCallback = null;
        p.LogMessageCallback = null;
        p.Flags = 0x01 | 0x08 | 0x40 | 0x80; // eDisableCLStateTracking | eAllowOTA | eLoadDownloadedPlugins | eUseFrameBasedResourceTagging
        p.FeaturesToLoad = null;
        p.NumFeaturesToLoad = 0;
        p.ApplicationId = 0;
        p.Engine = 0; // eCustom
        p.EngineVersion = null;
        p.ProjectId = null;
        p.RenderAPI = 2; // eVulkan
        return p;
    }
}
