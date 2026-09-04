using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using MinecraftPT.Streamline;

namespace MinecraftPT.Tests.Streamline;

public static unsafe class StreamlineMockHelper
{
    public static volatile bool LogTimeline = true;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int MockSleep(FrameToken* frame) => 0;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int MockSetOptionsReflex(ReflexOptions* options) => 0;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int MockGetStateReflex(ReflexState* state) => 0;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int MockSetCameraDataReflex(ViewportHandle* vp, FrameToken* frame, ReflexCameraData* cam) => 0;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int MockSetMarker(PCLMarker marker, FrameToken* frame)
    {
        if (LogTimeline)
        {
            lock (TimelineEvents)
            {
                TimelineEvents.Add((marker, (IntPtr)frame, Stopwatch.GetTimestamp()));
            }
        }
        return 0;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int MockSetOptionsPcl(PCLOptions* options) => 0;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int MockGetStatePcl(PCLState* state) => 0;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int MockDlssGetOptimalSettings(DLSSOptions* opt, DLSSOptimalSettings* set) => 0;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int MockDlssGetState(ViewportHandle* vp, DLSSState* state) => 0;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int MockDlssSetOptions(ViewportHandle* vp, DLSSOptions* opt) => 0;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int MockDlssdGetOptimalSettings(DLSSDOptions* opt, DLSSDOptimalSettings* set) => 0;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int MockDlssdGetState(ViewportHandle* vp, DLSSDState* state) => 0;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int MockDlssdSetOptions(ViewportHandle* vp, DLSSDOptions* opt) => 0;

    public static readonly List<(PCLMarker marker, IntPtr frameToken, long timestamp)> TimelineEvents = new();

    private static void SetInternalProp(Type type, string name, void* ptr)
    {
        var field = type.GetField($"<{name}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static);
        if (field == null)
            throw new Exception($"Cannot find backing field for {name} on {type.FullName}");

        var dm = new DynamicMethod($"Set_{name}", null, [typeof(IntPtr)], type.Module, true);
        var il = dm.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Stsfld, field);
        il.Emit(OpCodes.Ret);
        var setter = (Action<IntPtr>)dm.CreateDelegate(typeof(Action<IntPtr>));
        setter((IntPtr)ptr);
    }

    private static bool _initialized = false;
    private static readonly Lock _lock = new();

    public static void EnsureMockFunctionPointers()
    {
        lock (_lock)
        {
            if (_initialized) return;
            _initialized = true;

            SetInternalProp(typeof(ReflexAPI), nameof(ReflexAPI.SleepPtr), (void*)(delegate* unmanaged[Cdecl]<FrameToken*, int>)&MockSleep);
            SetInternalProp(typeof(ReflexAPI), nameof(ReflexAPI.SetOptionsPtr), (void*)(delegate* unmanaged[Cdecl]<ReflexOptions*, int>)&MockSetOptionsReflex);
            SetInternalProp(typeof(ReflexAPI), nameof(ReflexAPI.GetStatePtr), (void*)(delegate* unmanaged[Cdecl]<ReflexState*, int>)&MockGetStateReflex);
            SetInternalProp(typeof(ReflexAPI), nameof(ReflexAPI.SetCameraDataPtr), (void*)(delegate* unmanaged[Cdecl]<ViewportHandle*, FrameToken*, ReflexCameraData*, int>)&MockSetCameraDataReflex);

            SetInternalProp(typeof(PclAPI), nameof(PclAPI.SetMarkerPtr), (void*)(delegate* unmanaged[Cdecl]<PCLMarker, FrameToken*, int>)&MockSetMarker);
            SetInternalProp(typeof(PclAPI), nameof(PclAPI.SetOptionsPtr), (void*)(delegate* unmanaged[Cdecl]<PCLOptions*, int>)&MockSetOptionsPcl);
            SetInternalProp(typeof(PclAPI), nameof(PclAPI.GetStatePtr), (void*)(delegate* unmanaged[Cdecl]<PCLState*, int>)&MockGetStatePcl);

            SetInternalProp(typeof(DlssAPI), nameof(DlssAPI.GetOptimalSettingsPtr), (void*)(delegate* unmanaged[Cdecl]<DLSSOptions*, DLSSOptimalSettings*, int>)&MockDlssGetOptimalSettings);
            SetInternalProp(typeof(DlssAPI), nameof(DlssAPI.GetStatePtr), (void*)(delegate* unmanaged[Cdecl]<ViewportHandle*, DLSSState*, int>)&MockDlssGetState);
            SetInternalProp(typeof(DlssAPI), nameof(DlssAPI.SetOptionsPtr), (void*)(delegate* unmanaged[Cdecl]<ViewportHandle*, DLSSOptions*, int>)&MockDlssSetOptions);

            SetInternalProp(typeof(DlssdAPI), nameof(DlssdAPI.GetOptimalSettingsPtr), (void*)(delegate* unmanaged[Cdecl]<DLSSDOptions*, DLSSDOptimalSettings*, int>)&MockDlssdGetOptimalSettings);
            SetInternalProp(typeof(DlssdAPI), nameof(DlssdAPI.GetStatePtr), (void*)(delegate* unmanaged[Cdecl]<ViewportHandle*, DLSSDState*, int>)&MockDlssdGetState);
            SetInternalProp(typeof(DlssdAPI), nameof(DlssdAPI.SetOptionsPtr), (void*)(delegate* unmanaged[Cdecl]<ViewportHandle*, DLSSDOptions*, int>)&MockDlssdSetOptions);
        }
    }
}
