using System.Collections.Generic;
using UnityEngine;
using Sensus.Captions;

namespace Sensus.Audio;

internal static class InteractionAudio
{
    private static readonly Dictionary<GrabbableObject,bool> drops=new();
    private static readonly Dictionary<GrabbableObject,float> dropTimes=new();
    private static readonly List<GrabbableObject> retired=new();
    private static readonly HashSet<AudioClip> localMaskClips=new();
    internal static bool LocalMaskClip(AudioClip clip) => localMaskClips.Contains(clip);
    internal static void BeforeDiscard(GrabbableObject __instance)
    {
        // Capture the actor while the original method still has playerHeldBy.
        if(__instance.playerHeldBy!=null) Remember(__instance,__instance.playerHeldBy==GameNetworkManager.Instance?.localPlayerController);
    }
    internal static void EnemyDiscard(GrabbableObject __instance) => Remember(__instance,false);
    internal static void PickedUp(GrabbableObject __instance) { drops.Remove(__instance); dropTimes.Remove(__instance); }
    internal static void PlayerDrop(GameNetcodeStuff.PlayerControllerB __instance, GrabbableObject dropObject) => Remember(dropObject,__instance==GameNetworkManager.Instance?.localPlayerController);
    internal static void ForcedDrop(GameNetcodeStuff.PlayerControllerB __instance, GrabbableObject dropItem, bool itemsFall)
    { if(itemsFall) Remember(dropItem,__instance==GameNetworkManager.Instance?.localPlayerController); }
    internal static void PlayerPlace(GameNetcodeStuff.PlayerControllerB __instance, GrabbableObject placeObject) => Remember(placeObject,__instance==GameNetworkManager.Instance?.localPlayerController);
    private static void Remember(GrabbableObject item,bool local)
    {
        if(!drops.ContainsKey(item) && drops.Count>=1024) { Prune(); if(!drops.ContainsKey(item) && drops.Count>=1024) return; }
        drops[item]=local; dropTimes[item]=Time.unscaledTime;
    }
    internal static bool RecentDrop(GrabbableObject? item) => item!=null && dropTimes.TryGetValue(item,out float at) && Time.unscaledTime-at<=30;
    internal static bool LocalDrop(GrabbableObject? item) => item!=null && drops.TryGetValue(item,out bool local) && local;
    internal static void Register(Component component)
    {
        if(component is HauntedMaskItem mask && mask.maskAttachAudioLocal!=null) localMaskClips.Add(mask.maskAttachAudioLocal);
        if(component is GrabbableObject item && item.itemProperties!=null)
        {
            AudioRegistry.Clips(item.itemProperties.dropSFX,Cue.ItemDrop);
            if(item is HauntedMaskItem && item.itemProperties.clinkAudios!=null)
            {
                foreach(var clip in item.itemProperties.clinkAudios)
                {
                    if(clip==null) continue;
                    var cue=clip.name is "MaskLaugh1" or "MaskLaugh2" or "MaskLaugh3" ? Cue.MaskLaugh : Cue.ItemNoise;
                    AudioRegistry.Clips(clip,cue);
                }
            }
            else AudioRegistry.Clips(item.itemProperties.clinkAudios,Cue.ItemNoise);
            AudioRegistry.Clips(item.itemProperties.throwSFX,Cue.ItemNoise);
        }
        if(component is ItemDropship ship)
        {
            // One lifecycle traversal of this prefab, not a scene/per-frame scan.
            foreach(var audio in ship.GetComponentsInChildren<AudioSource>(true))
            {
                if(audio.clip==null) continue;
                Cue cue;
                switch(audio.clip.name)
                {
                    case "IcecreamTruckV2": case "IcecreamTruckFar": cue=Cue.SupplyLanding; break;
                    case "IcecreamTruckV2VehicleDeliveryVer": case "IcecreamTruckV2VehicleDeliveryVerFar": cue=Cue.VehicleDelivery; break;
                    default: continue;
                }
                AudioRegistry.Source(audio,cue,ship,true);
                AudioRegistry.Clips(audio.clip,cue);
            }
        }
    }
    internal static void Prune()
    {
        retired.Clear(); foreach(var entry in drops) if(entry.Key==null) retired.Add(entry.Key!);
        foreach(var key in retired) { drops.Remove(key); dropTimes.Remove(key); } retired.Clear();
    }
    internal static void Clear() { drops.Clear(); dropTimes.Clear(); retired.Clear(); localMaskClips.Clear(); }
}
