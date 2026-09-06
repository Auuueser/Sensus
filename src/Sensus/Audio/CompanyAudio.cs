using UnityEngine;
using Sensus.Captions;
namespace Sensus.Audio;

internal static class CompanyAudio
{
    internal static bool Resolve(AudioSource source,AudioClip clip,out Cue cue)
    {
        bool counter=AudioRegistry.Owner(source) is DepositItemsDesk;
        bool hatch=false,warehouse=false;
        if(CompanySoundRules.DoorClip(clip.name))
            for(var t=source.transform;t!=null;t=t.parent)
            {
                hatch |= t.name is "TrapDoor" or "TrapDoor(Clone)";
                warehouse |= t.name is "GarageDoorsContainer" or "GarageDoorsContainer(Clone)";
            }
        cue=CompanySoundRules.Resolve(clip.name,counter,hatch,warehouse);
        return cue!=Cue.Creature;
    }
    internal static int Group(AudioSource source,Cue cue)
    {
        // Both attack layers (desk + wall) are one audible action, not two threats.
        if(cue is Cue.CounterAttack or Cue.CounterWarning && AudioRegistry.Owner(source) is DepositItemsDesk desk) return desk.GetInstanceID();
        return AudioRegistry.Group(source);
    }
}
