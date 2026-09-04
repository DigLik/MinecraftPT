using Xunit;
using MinecraftPT.Graphics.Vulkan;
using MinecraftPT.Streamline;

namespace MinecraftPT.Tests.Streamline;

public class DlssConfigurationTests
{
    [Fact]
    public void DlssConfiguration_DefaultValues_AreConfiguredForDlaaAndGen2()
    {
        // Убеждаемся, что по умолчанию выставлен DLAA с пресетом K (Transformer для максимального качества)
        Assert.Equal(DLSSMode.eDLAA, DlssConfiguration.SrMode);
        Assert.Equal(DLSSPreset.ePresetK, DlssConfiguration.SrPreset);

        // Убеждаемся, что для DLSS Ray Reconstruction выставлен DLAA и пресет E (Gen 2 Transformer)
        Assert.Equal(DLSSMode.eDLAA, DlssConfiguration.RrMode);
        Assert.Equal(DLSSDPreset.ePresetE, DlssConfiguration.RrPreset);
        Assert.Equal(DLSSDNormalRoughnessMode.ePacked, DlssConfiguration.RrNormalRoughnessMode);
    }

    [Fact]
    public void DlssConfiguration_CanBeModifiedCentrally()
    {
        var prevSrMode = DlssConfiguration.SrMode;
        var prevSrPreset = DlssConfiguration.SrPreset;

        try
        {
            // Проверяем возможность смены на Performance + Preset M
            DlssConfiguration.SrMode = DLSSMode.eMaxPerformance;
            DlssConfiguration.SrPreset = DLSSPreset.ePresetM;

            Assert.Equal(DLSSMode.eMaxPerformance, DlssConfiguration.SrMode);
            Assert.Equal(DLSSPreset.ePresetM, DlssConfiguration.SrPreset);
        }
        finally
        {
            // Восстанавливаем дефолтное состояние
            DlssConfiguration.SrMode = prevSrMode;
            DlssConfiguration.SrPreset = prevSrPreset;
        }
    }

    [Fact]
    public void RenderPipelineInterface_HasRequestHistoryResetContract()
    {
        var method = typeof(MinecraftPT.Engine.Abstractions.IRenderPipeline).GetMethod("RequestHistoryReset");
        Assert.NotNull(method);
        Assert.Equal(typeof(void), method.ReturnType);
        Assert.Empty(method.GetParameters());
    }
}
