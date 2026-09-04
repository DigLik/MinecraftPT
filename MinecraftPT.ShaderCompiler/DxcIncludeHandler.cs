using System.Runtime.InteropServices;

using Silk.NET.Direct3D.Compilers;

namespace MinecraftPT.ShaderCompiler;

/// <summary>
/// Unmanaged COM implementation of IDxcIncludeHandler and IDxcBlobEncoding for DXC compiler.
/// </summary>
public unsafe class DxcIncludeHandler : IDisposable
{
    private static readonly Guid IID_IUnknown = new("00000000-0000-0000-C000-000000000046");
    private static readonly Guid IID_IDxcIncludeHandler = new("7FC41582-D611-4457-B0A6-1BD680FB1390");
    private static readonly Guid IID_IDxcBlob = new("8BA5FB08-5195-40E2-AC58-0D989C3A0102");
    private static readonly Guid IID_IDxcBlobEncoding = new("7241D424-2646-4191-97C0-98E96E42FC68");
    private static readonly Guid IID_IDxcBlobUtf8 = new("3DA636C9-BA71-4024-A301-30CBF125305B");

    private const int S_OK = 0;
    private const int E_NOINTERFACE = unchecked((int)0x80004002);
    private const int E_POINTER = unchecked((int)0x80004003);
    private const int ERROR_FILE_NOT_FOUND = unchecked((int)0x80070002);

    // Static vtables for COM objects
    private static readonly void** s_handlerVTable;
    private static readonly void** s_blobVTable;

    static DxcIncludeHandler()
    {
        // IDxcIncludeHandler vtable (4 entries)
        s_handlerVTable = (void**)NativeMemory.Alloc((nuint)(sizeof(void*) * 4));
        s_handlerVTable[0] = (delegate* unmanaged<void*, Guid*, void**, int>)&HandlerQueryInterface;
        s_handlerVTable[1] = (delegate* unmanaged<void*, uint>)&HandlerAddRef;
        s_handlerVTable[2] = (delegate* unmanaged<void*, uint>)&HandlerRelease;
        s_handlerVTable[3] = (delegate* unmanaged<void*, char*, void**, int>)&HandlerLoadSource;

        // IDxcBlobEncoding / IDxcBlobUtf8 vtable (8 entries)
        s_blobVTable = (void**)NativeMemory.Alloc((nuint)(sizeof(void*) * 8));
        s_blobVTable[0] = (delegate* unmanaged<void*, Guid*, void**, int>)&BlobQueryInterface;
        s_blobVTable[1] = (delegate* unmanaged<void*, uint>)&BlobAddRef;
        s_blobVTable[2] = (delegate* unmanaged<void*, uint>)&BlobRelease;
        s_blobVTable[3] = (delegate* unmanaged<void*, void*>)&BlobGetBufferPointer;
        s_blobVTable[4] = (delegate* unmanaged<void*, nuint>)&BlobGetBufferSize;
        s_blobVTable[5] = (delegate* unmanaged<void*, int*, uint*, int>)&BlobGetEncoding;
        s_blobVTable[6] = (delegate* unmanaged<void*, byte*>)&BlobGetStringPointer;
        s_blobVTable[7] = (delegate* unmanaged<void*, nuint>)&BlobGetStringLength;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct HandlerInstance
    {
        public void** VTable;
        public int RefCount;
        public IntPtr GcHandle;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BlobInstance
    {
        public void** VTable;
        public int RefCount;
        public byte* Buffer;
        public nuint Size;
    }

    private readonly List<string> _searchDirectories;
    private GCHandle _gcHandle;
    private HandlerInstance* _nativeInstance;

    public DxcIncludeHandler(IEnumerable<string> searchDirectories)
    {
        _searchDirectories = new List<string>(searchDirectories);
        _gcHandle = GCHandle.Alloc(this);

        _nativeInstance = (HandlerInstance*)NativeMemory.Alloc((nuint)sizeof(HandlerInstance));
        _nativeInstance->VTable = s_handlerVTable;
        _nativeInstance->RefCount = 1;
        _nativeInstance->GcHandle = GCHandle.ToIntPtr(_gcHandle);
    }

    public IDxcIncludeHandler* NativePointer => (IDxcIncludeHandler*)_nativeInstance;

    public void AddSearchDirectory(string directory)
    {
        if (!string.IsNullOrWhiteSpace(directory) && !_searchDirectories.Contains(directory))
        {
            _searchDirectories.Add(directory);
        }
    }

    private static uint InternalHandlerAddRef(void* thisPtr)
    {
        var inst = (HandlerInstance*)thisPtr;
        return (uint)Interlocked.Increment(ref inst->RefCount);
    }

    private static uint InternalHandlerRelease(void* thisPtr)
    {
        var inst = (HandlerInstance*)thisPtr;
        int count = Interlocked.Decrement(ref inst->RefCount);
        if (count == 0)
        {
            NativeMemory.Free(inst);
            return 0;
        }
        return (uint)count;
    }

    private static uint InternalBlobAddRef(void* thisPtr)
    {
        var blob = (BlobInstance*)thisPtr;
        return (uint)Interlocked.Increment(ref blob->RefCount);
    }

    private static uint InternalBlobRelease(void* thisPtr)
    {
        var blob = (BlobInstance*)thisPtr;
        int count = Interlocked.Decrement(ref blob->RefCount);
        if (count == 0)
        {
            if (blob->Buffer != null)
            {
                NativeMemory.Free(blob->Buffer);
                blob->Buffer = null;
            }
            NativeMemory.Free(blob);
            return 0;
        }
        return (uint)count;
    }

    [UnmanagedCallersOnly]
    private static int HandlerQueryInterface(void* thisPtr, Guid* riid, void** ppvObject)
    {
        if (ppvObject == null) return E_POINTER;
        if (riid == null) return E_POINTER;

        if (*riid == IID_IUnknown || *riid == IID_IDxcIncludeHandler)
        {
            *ppvObject = thisPtr;
            InternalHandlerAddRef(thisPtr);
            return S_OK;
        }

        *ppvObject = null;
        return E_NOINTERFACE;
    }

    [UnmanagedCallersOnly]
    private static uint HandlerAddRef(void* thisPtr)
    {
        return InternalHandlerAddRef(thisPtr);
    }

    [UnmanagedCallersOnly]
    private static uint HandlerRelease(void* thisPtr)
    {
        return InternalHandlerRelease(thisPtr);
    }

    [UnmanagedCallersOnly]
    private static int HandlerLoadSource(void* thisPtr, char* pFilename, void** ppIncludeSource)
    {
        if (ppIncludeSource == null) return E_POINTER;
        *ppIncludeSource = null;
        if (pFilename == null) return E_POINTER;

        var inst = (HandlerInstance*)thisPtr;
        var handler = (DxcIncludeHandler?)GCHandle.FromIntPtr(inst->GcHandle).Target;
        if (handler == null) return E_NOINTERFACE;

        string filename = Marshal.PtrToStringUni((IntPtr)pFilename) ?? string.Empty;
        string? resolvedPath = handler.ResolvePath(filename);

        if (resolvedPath != null && File.Exists(resolvedPath))
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(resolvedPath);
                *ppIncludeSource = CreateBlob(bytes);
                return S_OK;
            }
            catch
            {
                return ERROR_FILE_NOT_FOUND;
            }
        }

        return ERROR_FILE_NOT_FOUND;
    }

    private string? ResolvePath(string filename)
    {
        if (Path.IsPathRooted(filename) && File.Exists(filename))
        {
            return Path.GetFullPath(filename);
        }

        if (File.Exists(filename))
        {
            return Path.GetFullPath(filename);
        }

        foreach (var dir in _searchDirectories)
        {
            string candidate = Path.Combine(dir, filename);
            if (File.Exists(candidate))
            {
                return Path.GetFullPath(candidate);
            }
        }

        return null;
    }

    private static BlobInstance* CreateBlob(byte[] data)
    {
        var blob = (BlobInstance*)NativeMemory.Alloc((nuint)sizeof(BlobInstance));
        blob->VTable = s_blobVTable;
        blob->RefCount = 1;
        blob->Buffer = (byte*)NativeMemory.Alloc((nuint)data.Length);
        blob->Size = (nuint)data.Length;

        fixed (byte* pData = data)
        {
            System.Buffer.MemoryCopy(pData, blob->Buffer, data.Length, data.Length);
        }

        return blob;
    }

    [UnmanagedCallersOnly]
    private static int BlobQueryInterface(void* thisPtr, Guid* riid, void** ppvObject)
    {
        if (ppvObject == null) return E_POINTER;
        if (riid == null) return E_POINTER;

        if (*riid == IID_IUnknown || *riid == IID_IDxcBlob || *riid == IID_IDxcBlobEncoding || *riid == IID_IDxcBlobUtf8)
        {
            *ppvObject = thisPtr;
            InternalBlobAddRef(thisPtr);
            return S_OK;
        }

        *ppvObject = null;
        return E_NOINTERFACE;
    }

    [UnmanagedCallersOnly]
    private static uint BlobAddRef(void* thisPtr)
    {
        return InternalBlobAddRef(thisPtr);
    }

    [UnmanagedCallersOnly]
    private static uint BlobRelease(void* thisPtr)
    {
        return InternalBlobRelease(thisPtr);
    }

    [UnmanagedCallersOnly]
    private static void* BlobGetBufferPointer(void* thisPtr)
    {
        var blob = (BlobInstance*)thisPtr;
        return blob->Buffer;
    }

    [UnmanagedCallersOnly]
    private static nuint BlobGetBufferSize(void* thisPtr)
    {
        var blob = (BlobInstance*)thisPtr;
        return blob->Size;
    }

    [UnmanagedCallersOnly]
    private static int BlobGetEncoding(void* thisPtr, int* pKnown, uint* pCodePage)
    {
        if (pKnown != null) *pKnown = 1;
        if (pCodePage != null) *pCodePage = 65001; // CP_UTF8
        return S_OK;
    }

    [UnmanagedCallersOnly]
    private static byte* BlobGetStringPointer(void* thisPtr)
    {
        var blob = (BlobInstance*)thisPtr;
        return blob->Buffer;
    }

    [UnmanagedCallersOnly]
    private static nuint BlobGetStringLength(void* thisPtr)
    {
        var blob = (BlobInstance*)thisPtr;
        return blob->Size;
    }

    public void Dispose()
    {
        if (_nativeInstance != null)
        {
            InternalHandlerRelease(_nativeInstance);
            _nativeInstance = null;
        }

        if (_gcHandle.IsAllocated)
        {
            _gcHandle.Free();
        }

        GC.SuppressFinalize(this);
    }

    ~DxcIncludeHandler()
    {
        Dispose();
    }
}
