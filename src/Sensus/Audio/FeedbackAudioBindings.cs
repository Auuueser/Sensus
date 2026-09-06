using UnityEngine;
using Sensus.Captions;

namespace Sensus.Audio;

internal static class FeedbackAudioBindings
{
    internal static Cue TriggerCue(AnimatedObjectTrigger trigger)
    {
        // A trigger can use the same cupboard clips as an actual room door.
        if(trigger.GetComponent<DoorLock>()!=null || trigger.GetComponentInParent<DoorLock>()!=null) return Cue.Door;
        for(var t=trigger.transform;t!=null;t=t.parent)
        {
            string name=t.name;
            if(name.Contains("NormalDoor") || name.Contains("OfficeDoor") || name.Contains("DoubleDoors") || name.Contains("YellowMineDoor")) return Cue.Door;
            if(name.Contains("Cupboard") || name.Contains("Dresser") || name.Contains("Cabinet") || name.Contains("StorageCloset")) return Cue.Cabinet;
        }
        return Cue.Creature;
    }
    internal static bool Resolve(AudioSource source,AudioClip clip,out Cue cue)
    {
        cue=Cue.Creature;
        var owner=AudioRegistry.Owner(source);
        if(owner is TVScript tv && source==tv.tvSFX) { cue=Cue.Television; return true; }
        var vehicle=owner as VehicleController ?? source.GetComponentInParent<VehicleController>();
        if(vehicle!=null)
        {
            if(clip==vehicle.carHoodOpenSFX || clip==vehicle.carHoodCloseSFX) { cue=Cue.CruiserHood; return true; }
            if(clip==vehicle.revEngineStart || clip==vehicle.engineRev) { cue=Cue.CruiserIgnition; return true; }
            if(clip==vehicle.engineStartSuccessful) { cue=Cue.CruiserStarted; return true; }
            if(clip==vehicle.insertKey || clip==vehicle.twistKey || clip==vehicle.removeKey) { cue=Cue.CruiserKey; return true; }
            if(clip.name is "BackDoorOpen" or "BackDoorClose") { cue=Cue.CruiserRearDoor; return true; }
            if(clip.name is "DoorOpen" or "DoorClose" or "CabinDoorSlide") { cue=Cue.CruiserDoor; return true; }
            // Both native boost layers describe one jet event, not the engine loop.
            if(clip==vehicle.turboBoostSFX || clip==vehicle.turboBoostSFX2)
            { cue=Cue.VehicleBoost; return true; }
            if(clip==vehicle.pourTurbo) { cue=Cue.VehicleRefuel; return true; }
        }
        if(vehicle!=null && source==vehicle.hoodFireAudio) { cue=Cue.VehicleFire; return true; }
        for(var t=source.transform;t!=null;t=t.parent)
            if(t.name=="SpikeSlamBodyStickyPoint" || t.name=="SpikeSlamBodyStickyPoint(Clone)")
            { cue=Cue.BodyCrush; return true; }
        if(owner is AnimatedObjectTrigger trigger)
        {
            // Lock picking shares the door hierarchy but is not a door movement.
            if(clip.name is "LockpickPlayer" or "LockPickerMount" or "LockPickerFinish" or "DoorUnlock" or "DoorUnlock2" or "MineDoorUnlock") return false;
            var detail=AcousticDetails.Resolve(clip.name);
            var kind=TriggerCue(trigger);
            if(kind==Cue.Door && detail is Cue.CabinetDoor or Cue.Cabinet or Cue.Door or Cue.DoorOpen or Cue.DoorClose)
            { cue=detail is Cue.CabinetDoor or Cue.Cabinet ? Cue.Door : detail; return true; }
            if(kind==Cue.Cabinet && detail==Cue.Cabinet) { cue=kind; return true; }
        }
        var teleporter=owner as ShipTeleporter ?? source.GetComponentInParent<ShipTeleporter>();
        if(teleporter!=null && (source==teleporter.buttonAudio || source==teleporter.shipTeleporterAudio))
        { cue=teleporter.isInverseTeleporter ? Cue.InverseTeleport : Cue.Teleport; return true; }
        var item=source.GetComponentInParent<GrabbableObject>();
        if(item is FlashlightItem && (item.name=="LaserPointer" || item.name=="LaserPointer(Clone)") &&
            clip.name is "FlashlightClickMini" or "FlashlightClickMini2")
        { cue=Cue.LaserSwitch; return true; }
        if(owner is DeadBodyInfo body && source==body.playAudioOnDeath)
        { cue=clip.name=="CrushGore" ? Cue.BodyCrush : Cue.DeathSound; return true; }
        // Only the actual loop is lock picking. Handling uses pickup/drop rules.
        if(item is LockPicker picker && source==picker.lockPickerAudio && clip==source.clip && source.loop)
        { cue=Cue.LockPicking; return true; }
        return false;
    }
}
