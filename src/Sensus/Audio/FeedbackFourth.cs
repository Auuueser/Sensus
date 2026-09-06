using System.Collections.Generic;
using UnityEngine;
using Sensus.Captions;

namespace Sensus.Audio;

internal static class FeedbackFourth
{
    private static readonly Dictionary<VehicleController,float> vehicleBorn=new();
    private static readonly List<VehicleController> retired=new();
    [System.ThreadStatic] internal static bool LocalSplash;
    internal static void Register(Component component)
    {
        if(component is VehicleController vehicle && !vehicleBorn.ContainsKey(vehicle) && vehicleBorn.Count<128)
            vehicleBorn[vehicle]=Time.unscaledTime;
    }
    internal static bool StartupVehicle(AudioSource source,AudioClip clip)
    {
        if(clip.name is not ("Cruiser_HeadlightsOn" or "Cruiser_Turbulence")) return false;
        var vehicle=AudioRegistry.Owner(source) as VehicleController ?? source.GetComponentInParent<VehicleController>();
        // Initial serialized turbulence volume decays before the controller stops its loop.
        // This does not suppress collision one-shots on that same source.
        return vehicle!=null && (clip==vehicle.headlightsToggleSFX || source==vehicle.turbulenceAudio) && vehicleBorn.TryGetValue(vehicle,out float born) && Time.unscaledTime-born<5;
    }
    internal static bool Resolve(AudioSource source,AudioClip clip,out Cue cue)
    {
        cue=Cue.Creature;
        var bloom=AudioRegistry.Owner(source) as CadaverBloomAI;
        if(bloom!=null && source==bloom.creatureSFX && clip.name=="WalkQuickly") { cue=Cue.Running; return true; }
        if(clip.name=="Boombox6QuestionMark")
        {
            for(var t=source.transform;t!=null;t=t.parent)
                if(t.name=="DiscoBallContainer" || t.name=="DiscoBallContainer(Clone)") { cue=Cue.DiscoMusic; return true; }
            // The same asset is also in the boombox playlist. Do not leak the
            // furniture classification through the shared clip registry.
            cue=Cue.ItemMusic; return true;
        }
        return false;
    }
    internal static bool DeviceDirection(Cue cue) => cue is Cue.CruiserHood or Cue.CruiserDoor or Cue.CruiserRearDoor or Cue.BreakerDoor or Cue.BreakerSwitch or Cue.CabinetDoor or Cue.Cabinet or Cue.CarHood;
    internal static void BeforeSplash(bool syncToServer, ref bool __state)
    { __state=LocalSplash; LocalSplash=syncToServer; }
    internal static System.Exception? AfterSplash(System.Exception? __exception,bool __state)
    { LocalSplash=__state; return __exception; }
    internal static void Prune()
    {
        retired.Clear();
        foreach(var entry in vehicleBorn) if(entry.Key==null) retired.Add(entry.Key!);
        foreach(var vehicle in retired) vehicleBorn.Remove(vehicle);
        retired.Clear();
    }
    internal static void Clear() { vehicleBorn.Clear(); retired.Clear(); LocalSplash=false; }
}
