using StbImageSharp;

using ZstdSharp;

if (args.Length < 2)
{
    Console.WriteLine("Usage: TextureCompiler <input.png> <output.ztex>");
    return;
}

string inputPath = args[0];
string outputPath = args[1];

using var stream = File.OpenRead(inputPath);
var image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);

using var compressor = new Compressor(3);
Span<byte> compressedData = compressor.Wrap(image.Data);

File.WriteAllBytes(outputPath, compressedData);