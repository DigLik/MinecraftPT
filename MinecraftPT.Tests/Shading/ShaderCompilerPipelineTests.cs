using System.Diagnostics;
using System.Runtime.InteropServices;
using MinecraftPT.Engine.Abstractions.Graphics;
using MinecraftPT.Game.World.Meshing;
using MinecraftPT.Graphics.Vulkan;
using MinecraftPT.ShaderCompiler;
using Silk.NET.Direct3D.Compilers;
using Xunit;

using DxcBuffer = Silk.NET.Direct3D.Compilers.Buffer;

namespace MinecraftPT.Tests.Shading;

public unsafe class ShaderCompilerPipelineTests
{
    [Fact]
    public void StructMemoryLayout_MatchesHlslByteForByte()
    {
        // 8.1 CameraData vs Camera (structs.hlsli / ConstantBuffer<Camera>)
        int cameraSize = sizeof(CameraData);
        Assert.Equal(288, cameraSize);

        CameraData dummyCam = default;
        byte* pCam = (byte*)&dummyCam;
        Assert.Equal(0, (int)((byte*)&dummyCam.ViewProjection - pCam));
        Assert.Equal(64, (int)((byte*)&dummyCam.InverseViewProjection - pCam));
        Assert.Equal(128, (int)((byte*)&dummyCam.PrevViewProjection - pCam));
        Assert.Equal(192, (int)((byte*)&dummyCam.ChunkPosition - pCam));
        Assert.Equal(204, (int)((byte*)&dummyCam.FrameCount - pCam));
        Assert.Equal(208, (int)((byte*)&dummyCam.LocalPosition - pCam));
        Assert.Equal(220, (int)((byte*)&dummyCam.SamplesPerPixel - pCam));
        Assert.Equal(224, (int)((byte*)&dummyCam.SunDirection - pCam));
        Assert.Equal(240, (int)((byte*)&dummyCam.CameraUp - pCam));
        Assert.Equal(252, (int)((byte*)&dummyCam.Seed - pCam));
        Assert.Equal(256, (int)((byte*)&dummyCam.CameraRight - pCam));
        Assert.Equal(268, (int)((byte*)&dummyCam.JitterX - pCam));
        Assert.Equal(272, (int)((byte*)&dummyCam.CameraFwd - pCam));
        Assert.Equal(284, (int)((byte*)&dummyCam.JitterY - pCam));

        // 8.2 ChunkVertex vs ChunkVertex (structs.hlsli)
        int chunkVertexSize = sizeof(ChunkVertex);
        Assert.Equal(16, chunkVertexSize);
        Assert.Equal(0, (int)Marshal.OffsetOf<ChunkVertex>("<X>k__BackingField"));
        Assert.Equal(4, (int)Marshal.OffsetOf<ChunkVertex>("<Y>k__BackingField"));
        Assert.Equal(8, (int)Marshal.OffsetOf<ChunkVertex>("<Z>k__BackingField"));
        Assert.Equal(12, (int)Marshal.OffsetOf<ChunkVertex>("<PackedData>k__BackingField"));

        // 8.3 InstanceData vs InstanceData (structs.hlsli)
        int instanceDataSize = sizeof(VulkanRenderPipeline.InstanceData);
        Assert.Equal(32, instanceDataSize);
        VulkanRenderPipeline.InstanceData dummyInst = default;
        byte* pInst = (byte*)&dummyInst;
        Assert.Equal(0, (int)((byte*)&dummyInst.VertexOffset - pInst));
        Assert.Equal(4, (int)((byte*)&dummyInst.IndexOffset - pInst));
        Assert.Equal(8, (int)((byte*)&dummyInst.OpaqueIndexCount - pInst));
        Assert.Equal(12, (int)((byte*)&dummyInst.Pad2 - pInst));
        Assert.Equal(16, (int)((byte*)&dummyInst.VertexAddress - pInst));
        Assert.Equal(24, (int)((byte*)&dummyInst.IndexAddress - pInst));

        // 8.4 MaterialData vs MaterialData (structs.hlsli)
        int materialDataSize = sizeof(MaterialData);
        Assert.Equal(32, materialDataSize);
        MaterialData dummyMat = default;
        byte* pMat = (byte*)&dummyMat;
        Assert.Equal(0, (int)((byte*)&dummyMat.Roughness - pMat));
        Assert.Equal(4, (int)((byte*)&dummyMat.Metallic - pMat));
        Assert.Equal(8, (int)((byte*)&dummyMat.Emission - pMat));
        Assert.Equal(12, (int)((byte*)&dummyMat.Opacity - pMat));
        Assert.Equal(16, (int)((byte*)&dummyMat.Type - pMat));
        Assert.Equal(20, (int)((byte*)&dummyMat.Ior - pMat));
        Assert.Equal(24, (int)((byte*)&dummyMat.Absorption - pMat));
        Assert.Equal(28, (int)((byte*)&dummyMat.Pad - pMat));
    }

    [Fact]
    public void Shaders_CompileCleanlyToSpirv_And_MatchReflectionOffsets()
    {
        string shadersDir = FindShadersDirectory();
        Assert.True(Directory.Exists(shadersDir), $"Shaders directory exists at {shadersDir}");

        string[] shaderFiles = ["raygen.hlsl", "chit.hlsl", "ahit.hlsl", "miss.hlsl"];
        var compiledShaders = new Dictionary<string, byte[]>();

        foreach (var shaderFile in shaderFiles)
        {
            string fullPath = Path.Combine(shadersDir, shaderFile);
            Assert.True(File.Exists(fullPath), $"Shader file {shaderFile} exists");

            string hlslCode = File.ReadAllText(fullPath);
            var (spvBytes, errors, status) = CompileHlslInProcess(hlslCode, fullPath, [shadersDir]);

            Assert.True(status == 0 && spvBytes != null && spvBytes.Length > 0, $"Failed compiling {shaderFile}: {errors}");
            compiledShaders[shaderFile] = spvBytes;

            // Validate SPIR-V Header (Magic: 0x07230203)
            fixed (byte* p = spvBytes)
            {
                uint* words = (uint*)p;
                uint magic = words[0];
                uint version = words[1];
                uint bound = words[3];

                Assert.Equal(0x07230203u, magic);
                Assert.True(version >= 0x00010400u, $"SPIR-V version should be >= 1.4, actual: 0x{version:X8}");
                Assert.True(bound > 0);
            }

            RunSpirvValOnBinary(spvBytes, shaderFile);
        }

        // Validate SPIR-V member decorations from raygen
        if (compiledShaders.TryGetValue("raygen.hlsl", out var raygenSpv))
        {
            var decorations = ParseSpirvMemberOffsets(raygenSpv);
            string? cameraKey = decorations.Keys.FirstOrDefault(k => k.Contains("Camera"));
            Assert.NotNull(cameraKey);

            if (cameraKey != null && decorations.TryGetValue(cameraKey, out var offsets))
            {
                int[] expected = [0, 64, 128, 192, 204, 208, 220, 224, 240, 252, 256, 268, 272, 284];
                Assert.Equal(expected.Length, offsets.Count);
                Assert.True(offsets.SequenceEqual(expected));
            }
        }

        // Validate SPIR-V member decorations from chit
        if (compiledShaders.TryGetValue("chit.hlsl", out var chitSpv))
        {
            var decorations = ParseSpirvMemberOffsets(chitSpv);

            if (decorations.TryGetValue("InstanceData", out var instOffsets))
            {
                int[] expected = [0, 4, 8, 12, 16, 24];
                Assert.True(instOffsets.SequenceEqual(expected));
            }

            if (decorations.TryGetValue("MaterialData", out var matOffsets))
            {
                int[] expected = [0, 4, 8, 12, 16, 20, 24, 28];
                Assert.True(matOffsets.SequenceEqual(expected));
            }

            if (decorations.TryGetValue("ChunkVertex", out var vertOffsets))
            {
                int[] expected = [0, 4, 8, 12];
                Assert.True(vertOffsets.SequenceEqual(expected));
            }
        }
    }

    [Fact]
    public void DxcIncludeHandler_QueryInterface_And_LoadSource()
    {
        string shadersDir = FindShadersDirectory();

        using var handler = new DxcIncludeHandler([shadersDir]);
        Assert.True(handler.NativePointer != null);

        void** vtable = *(void***)handler.NativePointer;
        var qiFunc = (delegate* unmanaged<void*, Guid*, void**, int>)vtable[0];
        var loadSourceFunc = (delegate* unmanaged<void*, char*, void**, int>)vtable[3];

        Guid iidUnknown = new("00000000-0000-0000-C000-000000000046");
        Guid iidIncludeHandler = new("7FC41582-D611-4457-B0A6-1BD680FB1390");
        Guid iidBlobUtf8 = new("3DA636C9-BA71-4024-A301-30CBF125305B");
        Guid iidRandom = Guid.NewGuid();

        void* ppv = null;
        int hr = qiFunc(handler.NativePointer, &iidIncludeHandler, &ppv);
        Assert.Equal(0, hr);
        Assert.Equal((nint)handler.NativePointer, (nint)ppv);

        hr = qiFunc(handler.NativePointer, &iidUnknown, &ppv);
        Assert.Equal(0, hr);
        Assert.Equal((nint)handler.NativePointer, (nint)ppv);

        hr = qiFunc(handler.NativePointer, &iidRandom, &ppv);
        Assert.NotEqual(0, hr);
        Assert.True(ppv == null);

        // LoadSource on existing file
        fixed (char* pFilename = "structs.hlsli")
        {
            void* pBlob = null;
            hr = loadSourceFunc(handler.NativePointer, pFilename, &pBlob);
            Assert.Equal(0, hr);
            Assert.True(pBlob != null);

            if (pBlob != null)
            {
                void** blobVtable = *(void***)pBlob;
                var blobQiFunc = (delegate* unmanaged<void*, Guid*, void**, int>)blobVtable[0];
                var blobReleaseFunc = (delegate* unmanaged<void*, uint>)blobVtable[2];
                var getBufPtrFunc = (delegate* unmanaged<void*, void*>)blobVtable[3];
                var getBufSizeFunc = (delegate* unmanaged<void*, nuint>)blobVtable[4];
                var getEncodingFunc = (delegate* unmanaged<void*, int*, uint*, int>)blobVtable[5];
                var getStringPtrFunc = (delegate* unmanaged<void*, byte*>)blobVtable[6];
                var getStringLenFunc = (delegate* unmanaged<void*, nuint>)blobVtable[7];

                nuint blobSize = getBufSizeFunc(pBlob);
                void* bufPtr = getBufPtrFunc(pBlob);
                Assert.True(blobSize > 0 && bufPtr != null);

                int known = 0;
                uint codePage = 0;
                hr = getEncodingFunc(pBlob, &known, &codePage);
                Assert.True(hr == 0 && known == 1 && codePage == 65001);

                void* pBlobQi = null;
                hr = blobQiFunc(pBlob, &iidBlobUtf8, &pBlobQi);
                Assert.True(hr == 0 && pBlobQi == pBlob);

                byte* strPtr = getStringPtrFunc(pBlob);
                nuint strLen = getStringLenFunc(pBlob);
                Assert.True(strPtr == bufPtr && strLen == blobSize);

                blobReleaseFunc(pBlobQi);
                uint refCount = blobReleaseFunc(pBlob);
                Assert.Equal(0u, refCount);
            }
        }

        // LoadSource on non-existing file
        fixed (char* pFilename = "non_existent_header_12345.hlsli")
        {
            void* pBlob = null;
            hr = loadSourceFunc(handler.NativePointer, pFilename, &pBlob);
            Assert.NotEqual(0, hr);
            Assert.True(pBlob == null);
        }
    }

    [Fact]
    public void Dxc_NestedInclude_And_IncludeGuard_Tests()
    {
        string shadersDir = FindShadersDirectory();

        // Nested Include
        string testNestedHlsl = @"
#include ""common.hlsli""

[shader(""miss"")]
void main(inout Payload payload) {
    payload = InitPayload();
    payload.hitDistance = -1.0;
}
";
        var (nestedSpv, _, nestedStatus) = CompileHlslInProcess(testNestedHlsl, "test_nested.hlsl", [shadersDir]);
        Assert.True(nestedStatus == 0 && nestedSpv != null && nestedSpv.Length > 0);

        // Missing Include
        string testMissingHlsl = @"
#include ""does_not_exist.hlsli""

[shader(""miss"")]
void main(inout Payload payload) {
}
";
        var (missingSpv, _, missingStatus) = CompileHlslInProcess(testMissingHlsl, "test_missing.hlsl", [shadersDir]);
        Assert.True(missingStatus != 0 && missingSpv == null);

        // Invalid Syntax
        string testBadSyntaxHlsl = @"
#include ""structs.hlsli""

[shader(""miss"")]
void main(inout Payload payload) {
    this_is_an_invalid_statement_syntax_error @@##;
}
";
        var (badSpv, badErrors, badStatus) = CompileHlslInProcess(testBadSyntaxHlsl, "test_bad.hlsl", [shadersDir]);
        Assert.True(badStatus != 0 && badSpv == null && !string.IsNullOrWhiteSpace(badErrors));

        // Include Guard Idempotency
        string testIncludeGuardHlsl = @"
#include ""structs.hlsli""
#include ""sampling.hlsli""
#include ""common.hlsli""
#include ""structs.hlsli""

[shader(""miss"")]
void main(inout Payload payload) {
    payload = InitPayload();
}
";
        var (guardSpv, _, guardStatus) = CompileHlslInProcess(testIncludeGuardHlsl, "test_guard.hlsl", [shadersDir]);
        Assert.True(guardStatus == 0 && guardSpv != null);
    }

    private static string FindShadersDirectory()
    {
        string current = Directory.GetCurrentDirectory();
        for (int i = 0; i < 5; i++)
        {
            string candidate = Path.Combine(current, "MinecraftPT", "Assets", "Shaders");
            if (Directory.Exists(candidate)) return Path.GetFullPath(candidate);

            candidate = Path.Combine(current, "Assets", "Shaders");
            if (Directory.Exists(candidate)) return Path.GetFullPath(candidate);

            string? parent = Path.GetDirectoryName(current);
            if (parent == null || parent == current) break;
            current = parent;
        }

        return Path.GetFullPath(@"c:\Projects\C#\MinecraftPT\MinecraftPT\Assets\Shaders");
    }

    private static (byte[]? SpvBytes, string? Errors, int Status) CompileHlslInProcess(string sourceCode, string sourcePath, List<string> includeDirs, string profile = "lib_6_3")
    {
        try
        {
            var dxc = DXC.GetApi();
            Guid clsidCompiler = new Guid("73E22D93-E6CE-47F3-B5BF-F0664F39C1B0");
            Guid iidCompiler3 = typeof(IDxcCompiler3).GUID;

            void* pCompiler = null;
            int hr = dxc.CreateInstance(ref clsidCompiler, ref iidCompiler3, ref pCompiler);
            if (hr < 0) return (null, $"DXC CreateInstance failed with HR 0x{hr:X8}", hr);

            IDxcCompiler3* compiler = (IDxcCompiler3*)pCompiler;
            byte[] sourceBytes = System.Text.Encoding.UTF8.GetBytes(sourceCode);

            var compilerArgs = new List<string>
            {
                sourcePath,
                "-E", "main",
                "-T", profile,
                "-spirv",
                "-fspv-target-env=vulkan1.3",
                "-fvk-use-dx-layout",
                "-enable-16bit-types"
            };

            foreach (var inc in includeDirs)
            {
                compilerArgs.Add("-I");
                compilerArgs.Add(inc);
            }

            using var includeHandler = new DxcIncludeHandler(includeDirs);

            fixed (byte* pSource = sourceBytes)
            {
                DxcBuffer dxcBuffer = new DxcBuffer
                {
                    Ptr = pSource,
                    Size = (nuint)sourceBytes.Length,
                    Encoding = 65001
                };

                var argsPtrs = compilerArgs.Select(arg => Marshal.StringToHGlobalUni(arg)).ToArray();
                fixed (nint* pArgs = argsPtrs)
                {
                    Guid riidResult = typeof(IDxcResult).GUID;
                    void* pResult = null;

                    hr = compiler->Compile(
                        in dxcBuffer,
                        (char**)pArgs,
                        (uint)compilerArgs.Count,
                        includeHandler.NativePointer,
                        ref riidResult,
                        ref pResult
                    );

                    foreach (var ptr in argsPtrs)
                    {
                        Marshal.FreeHGlobal(ptr);
                    }

                    if (hr < 0)
                    {
                        compiler->Release();
                        return (null, $"Compile call failed with HR 0x{hr:X8}", hr);
                    }

                    IDxcResult* compileResult = (IDxcResult*)pResult;
                    string? errorStr = null;
                    IDxcBlobEncoding* errorBlob = null;
                    compileResult->GetErrorBuffer(ref errorBlob);
                    if (errorBlob != null)
                    {
                        nuint errorSize = errorBlob->GetBufferSize();
                        if (errorSize > 0)
                        {
                            errorStr = Marshal.PtrToStringAnsi((IntPtr)errorBlob->GetBufferPointer(), (int)errorSize);
                        }
                        errorBlob->Release();
                    }

                    int status = 0;
                    compileResult->GetStatus(ref status);
                    byte[]? spvBytes = null;
                    if (status >= 0)
                    {
                        IDxcBlob* resultBlob = null;
                        compileResult->GetResult(ref resultBlob);
                        if (resultBlob != null)
                        {
                            nuint bufSize = resultBlob->GetBufferSize();
                            spvBytes = new byte[(int)bufSize];
                            Marshal.Copy((IntPtr)resultBlob->GetBufferPointer(), spvBytes, 0, spvBytes.Length);
                            resultBlob->Release();
                        }
                    }

                    compileResult->Release();
                    compiler->Release();
                    return (spvBytes, errorStr, status);
                }
            }
        }
        catch (Exception ex)
        {
            return (null, ex.ToString(), -1);
        }
    }

    private static void RunSpirvValOnBinary(byte[] spvBytes, string shaderName)
    {
        try
        {
            string tempSpv = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}_{shaderName}.spv");
            File.WriteAllBytes(tempSpv, spvBytes);

            var psi = new ProcessStartInfo
            {
                FileName = "spirv-val.exe",
                Arguments = $"--target-env vulkan1.3 \"{tempSpv}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process != null)
            {
                process.WaitForExit(5000);
                string stderr = process.StandardError.ReadToEnd();
                bool passed = process.ExitCode == 0;
                Assert.True(passed, $"Khronos spirv-val: {shaderName} failed: {stderr}");
            }

            if (File.Exists(tempSpv)) File.Delete(tempSpv);
        }
        catch
        {
            // spirv-val may not be in PATH on all environments, skip if missing
        }
    }

    private static Dictionary<string, List<int>> ParseSpirvMemberOffsets(byte[] spvBytes)
    {
        var result = new Dictionary<string, List<int>>();
        var typeNames = new Dictionary<uint, string>();
        var memberOffsets = new Dictionary<uint, SortedDictionary<uint, int>>();

        if (spvBytes.Length < 20) return result;

        fixed (byte* p = spvBytes)
        {
            uint* words = (uint*)p;
            int totalWords = spvBytes.Length / 4;
            int i = 5;

            while (i < totalWords)
            {
                uint word = words[i];
                uint opCode = word & 0xFFFF;
                uint wordCount = (word >> 16) & 0xFFFF;
                if (wordCount == 0 || i + wordCount > totalWords) break;

                if (opCode == 5 && wordCount >= 3)
                {
                    uint targetId = words[i + 1];
                    var nameChars = new List<byte>();
                    for (int w = 2; w < wordCount; w++)
                    {
                        uint charWord = words[i + w];
                        for (int b = 0; b < 4; b++)
                        {
                            byte c = (byte)((charWord >> (b * 8)) & 0xFF);
                            if (c == 0) goto DoneName;
                            nameChars.Add(c);
                        }
                    }
                DoneName:
                    string name = System.Text.Encoding.UTF8.GetString(nameChars.ToArray());
                    typeNames[targetId] = name;
                }
                else if (opCode == 72 && wordCount >= 5)
                {
                    uint structType = words[i + 1];
                    uint memberIndex = words[i + 2];
                    uint decoration = words[i + 3];
                    if (decoration == 35)
                    {
                        int offset = (int)words[i + 4];
                        if (!memberOffsets.TryGetValue(structType, out var dict))
                        {
                            dict = new SortedDictionary<uint, int>();
                            memberOffsets[structType] = dict;
                        }
                        dict[memberIndex] = offset;
                    }
                }

                i += (int)wordCount;
            }
        }

        foreach (var (structId, offsets) in memberOffsets)
        {
            string name = typeNames.TryGetValue(structId, out var n) ? n : $"Type_{structId}";
            result[name] = offsets.Values.ToList();
        }

        return result;
    }
}
