using System.Numerics;
using System.Runtime.InteropServices;

namespace MinecraftPT.Streamline;

/// <summary>
/// Общие константы и параметры камеры текущего кадра для всех плагинов NVIDIA Streamline SDK.
/// Передаются каждый кадр через вызов <see cref="StreamlineAPI.slSetConstants"/>.
/// Соответствует структуре <c>sl::Constants</c> (GUID: <c>{DCD35AD7-4E4A-4BAD-A90C-E0C49EB23AFE}</c>, версия 2) из заголовочного файла <c>sl_consts.h</c>.
/// </summary>
/// <remarks>
/// <b>Важно:</b> Все матрицы должны быть представлены в строчном формате (row-major) и <b>не должны</b> содержать
/// субпиксельного смещения сглаживания (TAA/DLSS jitter). Величина сдвига передается отдельно через поле <see cref="JitterOffset"/>.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct Constants
{
    private static readonly StructType ConstantsTypeId = new(0xdcd35ad7, 0x4e4a, 0x4bad, 0xa9, 0x0c, 0xe0, 0xc4, 0x9e, 0xb2, 0x3a, 0xfe);

    /// <summary>
    /// Значение недействительного числа с плавающей запятой (<c>INVALID_FLOAT</c> = 3.402823466e38f / <see cref="float.MaxValue"/>).
    /// Используется для обязательных параметров, не имеющих адекватного дефолтного значения.
    /// </summary>
    public const float InvalidFloat = float.MaxValue;

    /// <summary>
    /// Значение недействительного беззнакового 32-битного целого (<c>INVALID_UINT</c> = 0xffffffff / <see cref="uint.MaxValue"/>).
    /// </summary>
    public const uint InvalidUInt = uint.MaxValue;

    /// <summary>
    /// Максимальное количество кадров в обработке GPU/CPU (<c>MAX_FRAMES_IN_FLIGHT</c> = 6).
    /// Обычно приложение использует не более 2-3 кадров, но для NVIDIA Reflex и сбора маркеров задержек
    /// количество параллельно отслеживаемых кадров может достигать 6.
    /// </summary>
    public const uint MaxFramesInFlight = 6;

    /// <summary>
    /// Базовый заголовок структуры Streamline (версия 2).
    /// </summary>
    public BaseStructure Base;

    /// <summary>
    /// Матрица преобразования из пространства вида камеры в пространство отсечения (View to Clip). Без субпиксельного сдвига (jitter).
    /// </summary>
    public Matrix4x4 CameraViewToClip;

    /// <summary>
    /// Матрица преобразования из пространства отсечения в пространство вида камеры (Clip to View).
    /// </summary>
    public Matrix4x4 ClipToCameraView;

    /// <summary>
    /// Опционально: Матрица искажения линзы в пространстве отсечения (Clip to Lens Clip).
    /// </summary>
    public Matrix4x4 ClipToLensClip;

    /// <summary>
    /// Матрица преобразования из текущего пространства отсечения в пространство отсечения предыдущего кадра (Clip to Previous Clip).
    /// Вычисляется как: <c>clipToPrevClip = clipToView * viewToViewPrev * viewToClipPrev</c>.
    /// </summary>
    public Matrix4x4 ClipToPrevClip;

    /// <summary>
    /// Обратная матрица преобразования из пространства отсечения предыдущего кадра в текущее (Previous Clip to Clip).
    /// </summary>
    public Matrix4x4 PrevClipToClip;

    /// <summary>
    /// Субпиксельное фазовое смещение выборки (Jitter offset) в пиксельном пространстве для временного сглаживания.
    /// </summary>
    public Vector2 JitterOffset;

    /// <summary>
    /// Масштабные коэффициенты для нормализации векторов движения в диапазон [-1, 1].
    /// </summary>
    public Vector2 MvecScale;

    /// <summary>
    /// Опционально: Смещение точечной диафрагмы камеры (Camera Pinhole Offset), если используется.
    /// </summary>
    public Vector2 CameraPinholeOffset;

    /// <summary>
    /// Мировая позиция камеры в трехмерном пространстве (Camera Position).
    /// </summary>
    public Vector3 CameraPos;

    /// <summary>
    /// Вектор направления «вверх» камеры в мировом пространстве (Camera Up).
    /// </summary>
    public Vector3 CameraUp;

    /// <summary>
    /// Вектор направления «вправо» камеры в мировом пространстве (Camera Right).
    /// </summary>
    public Vector3 CameraRight;

    /// <summary>
    /// Вектор направления взгляда «вперед» камеры в мировом пространстве (Camera Forward).
    /// </summary>
    public Vector3 CameraFwd;

    /// <summary>
    /// Расстояние до ближней плоскости отсечения камеры (Camera Near Plane).
    /// </summary>
    public float CameraNear;

    /// <summary>
    /// Расстояние до дальней плоскости отсечения камеры (Camera Far Plane).
    /// </summary>
    public float CameraFar;

    /// <summary>
    /// Вертикальное поле зрения камеры в радианах (Field of View / FOV).
    /// </summary>
    public float CameraFOV;

    /// <summary>
    /// Соотношение сторон видового экрана камеры (Aspect Ratio = ширина / высота).
    /// </summary>
    public float CameraAspectRatio;

    /// <summary>
    /// Значение, кодирующее недействительный/неинициализированный вектор в буфере векторов движения.
    /// Требуется, если <see cref="CameraMotionIncluded"/> равно <see cref="Boolean.eFalse"/> и Streamline должен сам рассчитать движение камеры.
    /// </summary>
    public float MotionVectorsInvalidValue;

    /// <summary>
    /// Флаг инвертированной глубины (Reversed-Z: ближняя плоскость 1.0, дальняя 0.0).
    /// </summary>
    public Boolean DepthInverted;

    /// <summary>
    /// Включено ли движение камеры в буфер векторов движения (MVec buffer).
    /// </summary>
    public Boolean CameraMotionIncluded;

    /// <summary>
    /// Являются ли векторы движения трехмерными (3D Motion Vectors).
    /// </summary>
    public Boolean MotionVectors3D;

    /// <summary>
    /// Флаг сброса истории (Reset history): установите <see cref="Boolean.eTrue"/> при смене сцены, телепортации или резком разрыве кадра.
    /// </summary>
    public Boolean Reset;

    /// <summary>
    /// Используется ли ортографическая проекция вместо перспективной.
    /// </summary>
    public Boolean OrthographicProjection;

    /// <summary>
    /// Предварительно дилатированы ли (расширены) векторы движения хост-приложением.
    /// </summary>
    public Boolean MotionVectorsDilated;

    /// <summary>
    /// Содержат ли векторы движения субпиксельный сдвиг (jittered motion vectors).
    /// </summary>
    public Boolean MotionVectorsJittered;

    /// <summary>
    /// Байт выравнивания для 4-байтовой границы.
    /// </summary>
    private byte pad0;

    /// <summary>
    /// Эвристика минимальной разницы глубин между объектами в экранном пространстве (в единицах линейной глубины).
    /// По умолчанию 40.0f. Используется для разделения перекрывающихся силуэтов.
    /// </summary>
    public float MinRelativeLinearDepthObjectSeparation;

    /// <summary>
    /// Создает экземпляр структуры <see cref="Constants"/> со стандартными значениями по умолчанию.
    /// </summary>
    /// <returns>Инициализированная структура <see cref="Constants"/> версии 2.</returns>
    public static Constants Create()
    {
        var c = new Constants();
        c.Base = new BaseStructure(ConstantsTypeId, 2);
        c.CameraViewToClip = Matrix4x4.Identity;
        c.ClipToCameraView = Matrix4x4.Identity;
        c.ClipToLensClip = Matrix4x4.Identity;
        c.ClipToPrevClip = Matrix4x4.Identity;
        c.PrevClipToClip = Matrix4x4.Identity;
        c.JitterOffset = Vector2.Zero;
        c.MvecScale = Vector2.One;
        c.CameraPinholeOffset = Vector2.Zero;
        c.CameraPos = Vector3.Zero;
        c.CameraUp = Vector3.Zero;
        c.CameraRight = Vector3.Zero;
        c.CameraFwd = Vector3.Zero;
        c.CameraNear = InvalidFloat;
        c.CameraFar = InvalidFloat;
        c.CameraFOV = InvalidFloat;
        c.CameraAspectRatio = InvalidFloat;
        c.MotionVectorsInvalidValue = InvalidFloat;
        c.DepthInverted = Boolean.eInvalid;
        c.CameraMotionIncluded = Boolean.eInvalid;
        c.MotionVectors3D = Boolean.eInvalid;
        c.Reset = Boolean.eInvalid;
        c.OrthographicProjection = Boolean.eFalse;
        c.MotionVectorsDilated = Boolean.eFalse;
        c.MotionVectorsJittered = Boolean.eFalse;
        c.MinRelativeLinearDepthObjectSeparation = 40.0f;
        return c;
    }
}
