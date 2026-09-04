using System.Numerics;

using MinecraftPT.Engine.Abstractions.Graphics;
using MinecraftPT.Utils.Math;

namespace MinecraftPT.Engine.Abstractions;

public interface IRenderPipeline : IDisposable
{
    void Initialize(ReadOnlySpan<VertexElement> layout, uint stride);
    IMesh CreateMesh<T>(List<T> vertices, List<ushort> indices, uint opaqueIndexCount = 0, List<ushort>? ommIndices = null) where T : unmanaged;
    void DeleteMesh(IMesh mesh);
    ITextureArray CreateTextureArray(int width, int height, byte[][] pixels);
    void BindTextureArray(ITextureArray textureArray);
    void BindMaterials(ReadOnlySpan<MaterialData> materials);
    void SubmitDraw(IMesh mesh, Vector3 position);
    void ClearDraws();
    void RenderFrame(in CameraData cameraData);
    void OnFramebufferResize(Vector2Int newSize);
    void StartFrame();
    bool GetPredictedCamera(out Matrix4x4 view, out Matrix4x4 proj);
    void SetSimulationStart();
    void SetSimulationEnd();
    void CycleReflexMode();

    /// <summary>
    /// Запрашивает сброс временной истории денойзера/масштабирования (DLSS-RR/SR) на текущем кадре
    /// при структурных изменениях геометрии мира (установка, разрушение блоков или обновление мешей).
    /// </summary>
    void RequestHistoryReset();
}