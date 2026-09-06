namespace Sensus.Captions;

internal static class CompanySoundRules
{
    internal static Cue Resolve(string clip,bool counter,bool hatch,bool warehouse) => clip switch
    {
        "BellDinger" => Cue.CounterBell,
        "SmallHatchDoorOpen" or "DoorShut" when counter => Cue.CounterShutter,
        "TakeItems1" or "TakeItems2" or "TakeItems3" when counter => Cue.CounterGrab,
        "TentaclesAttack" or "WallRumbleVoice" when counter => Cue.CounterAttack,
        "AngerAtDesk" or "AngerAtDesk2" or "WallRumble" when counter => Cue.CounterWarning,
        "CalmBehindDoor" or "DangerousMoodAmbience" when counter => Cue.CounterAmbience,
        "Snoring" when counter => Cue.CounterSnore,
        "MetalHatchOpen" or "MetalHatchClose" when hatch => Cue.AccessHatch,
        "BigDoorOpen" or "BigDoorClose" or "GarageDoorMove" or "GarageDoorShut" or "GarageDoorShut2" when warehouse => Cue.WarehouseDoor,
        _ => Cue.Creature
    };
    internal static bool DoorClip(string clip) => clip is "MetalHatchOpen" or "MetalHatchClose" or "BigDoorOpen" or "BigDoorClose" or "GarageDoorMove" or "GarageDoorShut" or "GarageDoorShut2";
}
