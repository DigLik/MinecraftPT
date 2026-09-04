using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Низкоуровневые P/Invoke импорты C-API функций динамической библиотеки <c>sl.interposer.dll</c>.
/// Соответствует сигнатурам функций ядра Streamline из заголовочных файлов <c>sl_core_api.h</c> и <c>sl_helpers_vk.h</c>.
/// </summary>
public static unsafe partial class StreamlineNative
{
    private const string DllName = "sl.interposer.dll";

    /// <summary>
    /// Инициализирует модуль Streamline SDK. Должен вызываться при старте приложения до создания графических устройств.
    /// Метод <b>не является</b> потокобезопасным.
    /// Соответствует <c>slInit</c> из <c>sl_core_api.h</c>.
    /// </summary>
    /// <param name="pref">Указатель на структуру предпочтений хоста (<see cref="Preferences"/>).</param>
    /// <param name="sdkVersion">64-битная версия Streamline SDK (<c>kSDKVersion</c>).</param>
    /// <returns>Код результата <see cref="Result"/> (<c>eOk</c> при успехе).</returns>
    [LibraryImport(DllName)]
    public static partial int slInit(Preferences* pref, ulong sdkVersion);

    /// <summary>
    /// Завершает работу модуля Streamline SDK и выгружает все загруженные плагины.
    /// Метод <b>не является</b> потокобезопасным.
    /// Соответствует <c>slShutdown</c> из <c>sl_core_api.h</c>.
    /// </summary>
    /// <returns>Код результата <see cref="Result"/> (<c>eOk</c> при успехе).</returns>
    [LibraryImport(DllName)]
    public static partial int slShutdown();

    /// <summary>
    /// Передает в Streamline контекст Vulkan API (устройство, инстанс, очереди).
    /// Должен вызываться сразу после создания устройства Vulkan при ручном перехвате.
    /// Соответствует <c>slSetVulkanInfo</c> из <c>sl_helpers_vk.h</c>.
    /// </summary>
    /// <param name="info">Указатель на структуру <see cref="VulkanInfo"/>.</param>
    /// <returns>Код результата <see cref="Result"/> (<c>eOk</c> при успехе).</returns>
    [LibraryImport(DllName)]
    public static partial int slSetVulkanInfo(VulkanInfo* info);

    /// <summary>
    /// Проверяет, поддерживается ли указанная технология на данном графическом адаптере.
    /// Соответствует <c>slIsFeatureSupported</c> из <c>sl_core_api.h</c>.
    /// </summary>
    /// <param name="feature">Идентификатор проверяемой технологии (<see cref="Feature"/>).</param>
    /// <param name="adapterInfo">Указатель на информацию об адаптере (<see cref="AdapterInfo"/>).</param>
    /// <returns>Код результата <see cref="Result"/> (<c>eOk</c> если поддерживается).</returns>
    [LibraryImport(DllName)]
    public static partial int slIsFeatureSupported(uint feature, AdapterInfo* adapterInfo);

    /// <summary>
    /// Регистрирует семантические теги графических ресурсов для указанного кадра и видового экрана.
    /// Потокобезопасный метод. Соответствует <c>slSetTagForFrame</c> из <c>sl_core_api.h</c>.
    /// </summary>
    /// <param name="frame">Указатель на маркёр текущего кадра (<see cref="FrameToken"/>).</param>
    /// <param name="viewport">Указатель на дескриптор видового экрана (<see cref="ViewportHandle"/>).</param>
    /// <param name="tags">Указатель на массив тегов ресурсов (<see cref="ResourceTag"/>).</param>
    /// <param name="numTags">Количество тегов в массиве.</param>
    /// <param name="cmdBuffer">Командный буфер GPU (или <see langword="null"/>, если ресурсы <see cref="ResourceLifecycle.eValidUntilPresent"/>).</param>
    /// <returns>Код результата <see cref="Result"/> (<c>eOk</c> при успехе).</returns>
    [LibraryImport(DllName)]
    public static partial int slSetTagForFrame(FrameToken* frame, ViewportHandle* viewport, ResourceTag* tags, uint numTags, void* cmdBuffer);

    /// <summary>
    /// Устанавливает общие константы и параметры камеры для текущего кадра и видового экрана.
    /// Потокобезопасный метод. Соответствует <c>slSetConstants</c> из <c>sl_core_api.h</c>.
    /// </summary>
    /// <param name="values">Указатель на структуру общих констант (<see cref="Constants"/>).</param>
    /// <param name="frame">Указатель на маркёр кадра (<see cref="FrameToken"/>).</param>
    /// <param name="viewport">Указатель на дескриптор видового экрана (<see cref="ViewportHandle"/>).</param>
    /// <returns>Код результата <see cref="Result"/> (<c>eOk</c> при успехе).</returns>
    [LibraryImport(DllName)]
    public static partial int slSetConstants(Constants* values, FrameToken* frame, ViewportHandle* viewport);

    /// <summary>
    /// Внедряет выполнение вычислительных команд технологии в конвейер рендеринга.
    /// Соответствует <c>slEvaluateFeature</c> из <c>sl_core_api.h</c>.
    /// </summary>
    /// <param name="feature">Идентификатор вычисляемой технологии (<see cref="Feature"/>).</param>
    /// <param name="frame">Указатель на маркёр текущего кадра (<see cref="FrameToken"/>).</param>
    /// <param name="inputs">Массив указателей на входные структуры (дескриптор вьюпорта, константы и т.д.).</param>
    /// <param name="numInputs">Количество структур во входном массиве.</param>
    /// <param name="cmdBuffer">Нативный командный буфер GPU (<c>VkCommandBuffer</c>).</param>
    /// <returns>Код результата <see cref="Result"/> (<c>eOk</c> при успехе).</returns>
    [LibraryImport(DllName)]
    public static partial int slEvaluateFeature(uint feature, FrameToken* frame, void** inputs, uint numInputs, void* cmdBuffer);

    /// <summary>
    /// Явно выделяет внутренние ресурсы для указанной технологии и видового экрана.
    /// Соответствует <c>slAllocateResources</c> из <c>sl_core_api.h</c>.
    /// </summary>
    /// <param name="cmdBuffer">Командный буфер GPU.</param>
    /// <param name="feature">Идентификатор технологии (<see cref="Feature"/>).</param>
    /// <param name="viewport">Указатель на дескриптор видового экрана (<see cref="ViewportHandle"/>).</param>
    /// <returns>Код результата <see cref="Result"/> (<c>eOk</c> при успехе).</returns>
    [LibraryImport(DllName)]
    public static partial int slAllocateResources(void* cmdBuffer, uint feature, ViewportHandle* viewport);

    /// <summary>
    /// Явно освобождает внутренние ресурсы указанной технологии и видового экрана.
    /// Соответствует <c>slFreeResources</c> из <c>sl_core_api.h</c>.
    /// </summary>
    /// <param name="feature">Идентификатор технологии (<see cref="Feature"/>).</param>
    /// <param name="viewport">Указатель на дескриптор видового экрана (<see cref="ViewportHandle"/>).</param>
    /// <returns>Код результата <see cref="Result"/> (<c>eOk</c> при успехе).</returns>
    [LibraryImport(DllName)]
    public static partial int slFreeResources(uint feature, ViewportHandle* viewport);

    /// <summary>
    /// Получает новый уникальный токен кадра (Frame Token) для идентификации текущего кадра во всех вызовах Streamline.
    /// Потокобезопасный метод. Соответствует <c>slGetNewFrameToken</c> из <c>sl_core_api.h</c>.
    /// </summary>
    /// <param name="token">Возвращаемый указатель на выделенный токен кадра (<see cref="FrameToken"/>).</param>
    /// <param name="frameIndex">Опциональный указатель на номер кадра (если <see langword="null"/>, используется внутренний счетчик).</param>
    /// <returns>Код результата <see cref="Result"/> (<c>eOk</c> при успехе).</returns>
    [LibraryImport(DllName)]
    public static partial int slGetNewFrameToken(FrameToken** token, uint* frameIndex);

    /// <summary>
    /// Получает функциональный указатель на специфичную для технологии API-функцию по её имени.
    /// Должен вызываться после установки графического устройства.
    /// Потокобезопасный метод. Соответствует <c>slGetFeatureFunction</c> из <c>sl_core_api.h</c>.
    /// </summary>
    /// <param name="feature">Идентификатор технологии (<see cref="Feature"/>).</param>
    /// <param name="functionName">Имя запрашиваемой C-функции в формате строки UTF-8 с завершающим нулём.</param>
    /// <param name="function">Возвращаемый указатель на функцию.</param>
    /// <returns>Код результата <see cref="Result"/> (<c>eOk</c> при успехе).</returns>
    [LibraryImport(DllName)]
    public static partial int slGetFeatureFunction(uint feature, byte* functionName, void** function);

    /// <summary>
    /// Перехватываемый Streamline вызов презентации кадра очереди Vulkan <c>vkQueuePresentKHR</c>.
    /// Выполняет завершающие операции технологий (например, DLSS Frame Generation) перед выводом на экран.
    /// </summary>
    /// <param name="queue">Очередь вывода Vulkan (<c>VkQueue</c>).</param>
    /// <param name="presentInfo">Указатель на структуру параметров презентации <c>VkPresentInfoKHR</c>.</param>
    /// <returns>Результат выполнения Vulkan (<c>VkResult</c>).</returns>
    [LibraryImport(DllName, EntryPoint = "vkQueuePresentKHR")]
    public static partial int vkQueuePresentKHR(void* queue, void* presentInfo);
}
