using System.Buffers;
using System.Buffers.Binary;

using MinecraftPT.Utils.Math;

using ZstdSharp;

namespace MinecraftPT.Game.World.Environment;

public readonly record struct PooledChunkData(byte[] Buffer, int Length);

public class Bucket
{
    private const uint ZstdMagicNumber = 0xFD2FB528;

    private readonly string _filePath;
    private readonly string _legacyFilePath;
    private readonly Lock _ioLock = new();
    private readonly Lock _dataLock = new();

    private readonly Dictionary<Vector3Int, PooledChunkData> _chunkData = [];

    public bool IsDirty { get; private set; }
    public long LastAccessTick;

    public Bucket(string directory, Vector3Int bucketPos)
    {
        _filePath = Path.Combine(directory, $"bucket.{bucketPos.X}.{bucketPos.Y}.{bucketPos.Z}.zbin");
        _legacyFilePath = Path.Combine(directory, $"bucket.{bucketPos.X}.{bucketPos.Y}.{bucketPos.Z}.bin");
        Load();
    }

    private void Load()
    {
        string? targetPath = null;
        if (File.Exists(_filePath)) targetPath = _filePath;
        else if (File.Exists(_legacyFilePath)) targetPath = _legacyFilePath;

        if (targetPath == null) return;

        lock (_ioLock)
        {
            try
            {
                byte[] fileBytes = File.ReadAllBytes(targetPath);
                if (fileBytes.Length < 4) return;

                uint magic = BinaryPrimitives.ReadUInt32LittleEndian(fileBytes.AsSpan(0, 4));
                Stream dataStream;
                MemoryStream? decompressedStream = null;

                if (magic == ZstdMagicNumber)
                {
                    using var decompressor = new Decompressor();
                    byte[] uncompressed = decompressor.Unwrap(fileBytes).ToArray();
                    decompressedStream = new MemoryStream(uncompressed);
                    dataStream = decompressedStream;
                }
                else
                {
                    dataStream = new MemoryStream(fileBytes);
                }

                using (decompressedStream)
                using (dataStream)
                using (var reader = new BinaryReader(dataStream))
                {
                    int chunkCount = reader.ReadInt32();
                    lock (_dataLock)
                    {
                        for (int i = 0; i < chunkCount; i++)
                        {
                            int x = reader.ReadInt32();
                            int y = reader.ReadInt32();
                            int z = reader.ReadInt32();

                            int dataLength = reader.ReadInt32();
                            byte[] buffer = ArrayPool<byte>.Shared.Rent(dataLength);
                            reader.Read(buffer, 0, dataLength);
                            _chunkData[new Vector3Int(x, y, z)] = new PooledChunkData(buffer, dataLength);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading bucket {targetPath}: {ex.Message}");
            }
        }
    }

    public void Save()
    {
        if (!IsDirty) return;
        lock (_ioLock)
        {
            try
            {
                using var ms = new MemoryStream();
                using var writer = new BinaryWriter(ms);

                lock (_dataLock)
                {
                    writer.Write(_chunkData.Count);
                    foreach (var kvp in _chunkData)
                    {
                        writer.Write(kvp.Key.X);
                        writer.Write(kvp.Key.Y);
                        writer.Write(kvp.Key.Z);

                        writer.Write(kvp.Value.Length);
                        writer.Write(kvp.Value.Buffer, 0, kvp.Value.Length);
                    }
                    IsDirty = false;
                }

                ReadOnlySpan<byte> uncompressedSpan = ms.GetBuffer().AsSpan(0, (int)ms.Length);
                using var compressor = new Compressor(3);
                Span<byte> compressedData = compressor.Wrap(uncompressedSpan);

                using (var fs = File.Create(_filePath))
                {
                    fs.Write(compressedData);
                }

                if (File.Exists(_legacyFilePath))
                {
                    try
                    {
                        File.Delete(_legacyFilePath);
                    }
                    catch
                    {
                        // Игнорируем ошибку удаления устаревшего файла при блокировке
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving bucket {_filePath}: {ex.Message}");
            }
        }
    }

    public ReadOnlySpan<byte> GetChunkData(Vector3Int localChunkPos)
    {
        lock (_dataLock)
        {
            if (_chunkData.TryGetValue(localChunkPos, out var data))
                return new ReadOnlySpan<byte>(data.Buffer, 0, data.Length);
            return default;
        }
    }

    public void SetChunkData(Vector3Int localChunkPos, PooledChunkData data)
    {
        lock (_dataLock)
        {
            if (_chunkData.TryGetValue(localChunkPos, out var oldData))
                ArrayPool<byte>.Shared.Return(oldData.Buffer);

            _chunkData[localChunkPos] = data;
            IsDirty = true;
        }
    }

    public void FreeMemory()
    {
        lock (_dataLock)
        {
            foreach (var kvp in _chunkData)
                ArrayPool<byte>.Shared.Return(kvp.Value.Buffer);
            _chunkData.Clear();
        }
    }
}