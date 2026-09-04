using System.Runtime.InteropServices;

using Silk.NET.Direct3D.Compilers;

using DxcBuffer = Silk.NET.Direct3D.Compilers.Buffer;

namespace MinecraftPT.ShaderCompiler;

internal unsafe class Program
{
    private static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.WriteLine("Usage: MinecraftPT.ShaderCompiler <input_hlsl_path> <output_spv_path> [-I <include_dir>]...");
            return 1;
        }

        string inputPath = Path.GetFullPath(args[0].Trim('"', '\''));
        string outputPath = Path.GetFullPath(args[1].Trim('"', '\''));

        if (!File.Exists(inputPath))
        {
            Console.Error.WriteLine($"Error: Input file '{inputPath}' does not exist.");
            return 1;
        }

        var includeDirs = new List<string>();
        string? inputDir = Path.GetDirectoryName(inputPath);
        if (!string.IsNullOrEmpty(inputDir))
        {
            includeDirs.Add(inputDir);
        }

        for (int i = 2; i < args.Length; i++)
        {
            if (args[i] == "-I" && i + 1 < args.Length)
            {
                string rawDir = args[++i].Trim('"', '\'');
                if (!string.IsNullOrWhiteSpace(rawDir))
                {
                    string dir = Path.GetFullPath(rawDir);
                    if (!includeDirs.Contains(dir))
                    {
                        includeDirs.Add(dir);
                    }
                }
            }
        }

        Console.WriteLine($"Compiling: {inputPath} -> {outputPath}");
        Console.WriteLine($"Include directories: {string.Join(", ", includeDirs)}");

        try
        {
            // 1. Initialize DXC API
            var dxc = DXC.GetApi();

            // 2. Create compiler instance IDxcCompiler3
            Guid clsidCompiler = new Guid("73E22D93-E6CE-47F3-B5BF-F0664F39C1B0");
            Guid iidCompiler3 = typeof(IDxcCompiler3).GUID;

            void* pCompiler = null;
            int hr = dxc.CreateInstance(ref clsidCompiler, ref iidCompiler3, ref pCompiler);
            if (hr < 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }

            IDxcCompiler3* compiler = (IDxcCompiler3*)pCompiler;

            // 3. Read source code
            byte[] sourceBytes = File.ReadAllBytes(inputPath);

            // 4. Configure arguments
            string profile = "lib_6_3";
            var compilerArgs = new List<string>
            {
                inputPath,
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

            Console.WriteLine($"Compiler profile: {profile}, args: {string.Join(" ", compilerArgs)}");

            using var includeHandler = new DxcIncludeHandler(includeDirs);

            // 5. Compile
            fixed (byte* pSource = sourceBytes)
            {
                DxcBuffer dxcBuffer = new DxcBuffer
                {
                    Ptr = pSource,
                    Size = (nuint)sourceBytes.Length,
                    Encoding = 65001 // DXC_CP_UTF8
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

                    // Free memory for arguments immediately
                    foreach (var ptr in argsPtrs)
                    {
                        Marshal.FreeHGlobal(ptr);
                    }

                    if (hr < 0)
                    {
                        Marshal.ThrowExceptionForHR(hr);
                    }

                    IDxcResult* compileResult = (IDxcResult*)pResult;

                    // Check compiler errors/warnings
                    IDxcBlobEncoding* errorBlob = null;
                    compileResult->GetErrorBuffer(ref errorBlob);
                    if (errorBlob != null)
                    {
                        nuint errorSize = errorBlob->GetBufferSize();
                        if (errorSize > 0)
                        {
                            string errors =
                                Marshal.PtrToStringAnsi((IntPtr)errorBlob->GetBufferPointer(), (int)errorSize);
                            if (!string.IsNullOrWhiteSpace(errors))
                            {
                                Console.Error.WriteLine("Compiler Output/Errors:");
                                Console.Error.WriteLine(errors);
                            }
                        }

                        errorBlob->Release();
                    }

                    int status = 0;
                    compileResult->GetStatus(ref status);
                    if (status < 0)
                    {
                        Console.Error.WriteLine($"Compilation failed with HRESULT {status:X}");
                        compileResult->Release();
                        compiler->Release();
                        return 1;
                    }

                    // Get results
                    IDxcBlob* resultBlob = null;
                    compileResult->GetResult(ref resultBlob);
                    if (resultBlob != null)
                    {
                        nuint bufferSize = resultBlob->GetBufferSize();
                        byte[] compiledBytes = new byte[(int)bufferSize];
                        Marshal.Copy((IntPtr)resultBlob->GetBufferPointer(), compiledBytes, 0, compiledBytes.Length);

                        // Write output file
                        string? directory = Path.GetDirectoryName(outputPath);
                        if (!string.IsNullOrEmpty(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        File.WriteAllBytes(outputPath, compiledBytes);
                        Console.WriteLine($"Successfully compiled to '{outputPath}' ({bufferSize} bytes)");

                        resultBlob->Release();
                    }

                    compileResult->Release();
                }
            }

            compiler->Release();
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error compiling shader: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }
}
