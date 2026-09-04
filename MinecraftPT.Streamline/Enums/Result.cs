namespace MinecraftPT.Streamline;

/// <summary>
/// Коды результатов и ошибок выполнения вызовов API NVIDIA Streamline SDK.
/// Соответствует перечислению <c>sl::Result</c> из заголовочного файла <c>sl_result.h</c>.
/// </summary>
public enum Result : int
{
    /// <summary>
    /// Операция выполнена успешно.
    /// </summary>
    eOk = 0,

    /// <summary>
    /// Ошибка ввода-вывода (не удалось прочитать файл конфигурации или плагин).
    /// </summary>
    eErrorIO,

    /// <summary>
    /// Установленная версия графического драйвера устарела и не поддерживает требуемые функции.
    /// </summary>
    eErrorDriverOutOfDate,

    /// <summary>
    /// Версия операционной системы Windows устарела и не поддерживает требуемые подсистемы.
    /// </summary>
    eErrorOSOutOfDate,

    /// <summary>
    /// Аппаратное планирование GPU (Hardware-accelerated GPU Scheduling / HWS) отключено в настройках ОС.
    /// </summary>
    eErrorOSDisabledHWS,

    /// <summary>
    /// Графическое устройство (Device) еще не создано на момент вызова метода.
    /// </summary>
    eErrorDeviceNotCreated,

    /// <summary>
    /// В системе не обнаружено поддерживаемых графических адаптеров NVIDIA.
    /// </summary>
    eErrorNoSupportedAdapterFound,

    /// <summary>
    /// Указанный графический адаптер не поддерживается выбранной технологией.
    /// </summary>
    eErrorAdapterNotSupported,

    /// <summary>
    /// Не найдены файлы плагинов Streamline в указанных путях поиска.
    /// </summary>
    eErrorNoPlugins,

    /// <summary>
    /// Внутренняя ошибка подсистемы Vulkan API.
    /// </summary>
    eErrorVulkanAPI,

    /// <summary>
    /// Внутренняя ошибка подсистемы DXGI API.
    /// </summary>
    eErrorDXGIAPI,

    /// <summary>
    /// Внутренняя ошибка подсистемы Direct3D API.
    /// </summary>
    eErrorD3DAPI,

    /// <summary>
    /// Ошибка NRD API (модуль NRD удален в актуальных версиях SDK).
    /// </summary>
    eErrorNRDAPI,

    /// <summary>
    /// Внутренняя ошибка вызова библиотеки NVAPI.
    /// </summary>
    eErrorNVAPI,

    /// <summary>
    /// Внутренняя ошибка подсистемы NVIDIA Reflex API.
    /// </summary>
    eErrorReflexAPI,

    /// <summary>
    /// Ошибка инициализации или исполнения ядра NVIDIA NGX (Next Generation Image Processing).
    /// </summary>
    eErrorNGXFailed,

    /// <summary>
    /// Ошибка разбора JSON-манифеста конфигурации плагина.
    /// </summary>
    eErrorJSONParsing,

    /// <summary>
    /// Отсутствует перехватывающий прокси-интерфейс Streamline.
    /// </summary>
    eErrorMissingProxy,

    /// <summary>
    /// Не указано требуемое начальное состояние графического ресурса (<c>VkImageLayout</c> или <c>D3D12_RESOURCE_STATES</c>).
    /// </summary>
    eErrorMissingResourceState,

    /// <summary>
    /// Обнаружена некорректная последовательность интеграции или несовместимые флаги.
    /// </summary>
    eErrorInvalidIntegration,

    /// <summary>
    /// Отсутствует обязательный входной параметр в цепочке структур.
    /// </summary>
    eErrorMissingInputParameter,

    /// <summary>
    /// Подсистема или метод плагина не инициализированы (не загружены функциональные указатели).
    /// </summary>
    eErrorNotInitialized,

    /// <summary>
    /// Сбой выполнения вычислительного шейдера/задачи на GPU.
    /// </summary>
    eErrorComputeFailed,

    /// <summary>
    /// Метод <c>slInit</c> не был вызван перед обращением к функциям Streamline.
    /// </summary>
    eErrorInitNotCalled,

    /// <summary>
    /// Внутреннее исключение в обработчике Streamline.
    /// </summary>
    eErrorExceptionHandler,

    /// <summary>
    /// Передан неверный аргумент (null-указатель, некорректный идентификатор или размер).
    /// </summary>
    eErrorInvalidParameter,

    /// <summary>
    /// Не переданы обязательные константы для технологии.
    /// </summary>
    eErrorMissingConstants,

    /// <summary>
    /// Обнаружено дублирование констант в цепочке структур.
    /// </summary>
    eErrorDuplicatedConstants,

    /// <summary>
    /// Отсутствует или не поддерживается требуемый графический API.
    /// </summary>
    eErrorMissingOrInvalidAPI,

    /// <summary>
    /// Отсутствуют общие константы кадра (<see cref="Constants"/>), необходимые для работы плагинов.
    /// </summary>
    eErrorCommonConstantsMissing,

    /// <summary>
    /// Передан неподдерживаемый интерфейс для перехвата/обновления.
    /// </summary>
    eErrorUnsupportedInterface,

    /// <summary>
    /// Требуемая технология или плагин отсутствует в списке загруженных.
    /// </summary>
    eErrorFeatureMissing,

    /// <summary>
    /// Технология не поддерживается на данном оборудовании или платформе.
    /// </summary>
    eErrorFeatureNotSupported,

    /// <summary>
    /// Отсутствуют необходимые хуки графического API.
    /// </summary>
    eErrorFeatureMissingHooks,

    /// <summary>
    /// Не удалось загрузить динамическую библиотеку плагина технологии.
    /// </summary>
    eErrorFeatureFailedToLoad,

    /// <summary>
    /// Неверный приоритет выполнения функции в конвейере.
    /// </summary>
    eErrorFeatureWrongPriority,

    /// <summary>
    /// Отсутствует обязательная зависимость плагина (например, зависимость от базового модуля или драйвера).
    /// </summary>
    eErrorFeatureMissingDependency,

    /// <summary>
    /// Недопустимое состояние менеджера технологий Streamline.
    /// </summary>
    eErrorFeatureManagerInvalidState,

    /// <summary>
    /// Недопустимое общее внутреннее состояние фреймворка.
    /// </summary>
    eErrorInvalidState,

    /// <summary>
    /// Предупреждение: недостаточно свободной видеопамяти (VRAM) для выделения промежуточных буферов.
    /// </summary>
    eWarnOutOfVRAM
}
