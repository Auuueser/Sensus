using UnityEngine;
using Sensus.Captions;
namespace Sensus.Audio;

internal static class SupplementalAudio
{
    private static readonly System.Collections.Generic.HashSet<Component> scanned=new();
    internal static void Prune() => scanned.RemoveWhere(c=>c==null);
    internal static void Clear() => scanned.Clear();
    internal static bool Clip(AudioClip clip,out Cue cue)
    {
        cue=AcousticDetails.Resolve(ClipNames.Get(clip));
        if(cue!=Cue.Creature) return true;
        cue=ClipNames.Get(clip) switch
        {
            "CentipedeWalk" => Cue.Crawling,
            "FliesBuzzing" or "FliesBuzzingAndMaggots" => Cue.Flies,
            "LungMachine" => Cue.PowerHum,
            "ChatteringTeeth" => Cue.Teeth,
            "Hairdryer1" or "HairdryerFar" => Cue.Hairdryer,
            "PhoneScream" => Cue.Phone,
            "robotTune" or "RobotToyCheer" => Cue.ToyRobot,
            "ClockTick" or "ClockTock" => Cue.Clock,
            "ClimbLadderLoosenGrip" or "ClimbLadderLoosenGrip2" or "ClimbLadderLoosenGrip3" or "ClimbLadderStep2" or "ClimbLadderStep3" or "ClimbLadderStep4" => Cue.LadderClimb,
            "SpikeRoofSlam" => Cue.SpikeSlam,
            "SpikeRoofTrapCreak" => Cue.SpikeCreak,
            "Cruiser_Explode" or "MineDetonate" => Cue.Explosion,
            "VentCrawl1" or "VentOpen1" or "NutcrackerVentCrawl" => Cue.Vent,
            _ => Cue.Creature
        };
        return cue!=Cue.Creature;
    }
    private static void Clips(AudioClip[]? clips)
    { if(clips!=null) foreach(var clip in clips) if(clip!=null && Clip(clip,out var cue)) AudioRegistry.Clips(clip,cue); }
    private static void Source(AudioSource? source,Component owner)
    {
        if(source==null || source.clip==null || (!FeedbackFourth.Resolve(source,source.clip,out var cue) && !Clip(source.clip,out cue) && !AuditAudioBindings.NativeClip(source.clip,out cue))) return;
        if(cue==Cue.Flies) { AudioCapture.RecordPlay(source); return; }
        AudioRegistry.Source(source,cue,owner,true); AudioRegistry.Clips(source.clip,cue);
        if(source.isPlaying) AudioCapture.RecordPlay(source);
    }
    internal static void Register(Component component)
    {
        if(component is PlayAudioAnimationEvent anim)
        {
            Clips(anim.randomClips); Clips(anim.randomClips2);
            foreach(var clip in new[]{anim.audioClip,anim.audioClip2,anim.audioClip3})
                if(clip!=null && (Clip(clip,out var cue) || AuditAudioBindings.NativeClip(clip,out cue))) AudioRegistry.Clips(clip,cue);
            // Trap animation callbacks play on child sources, outside its main field.
            var delivery=anim.GetComponentInParent<ItemDropship>();
            if(delivery!=null) { AudioRegistry.Source(anim.audioToPlay,Cue.ShipMechanism,delivery,true); AudioRegistry.Source(anim.audioToPlayB,Cue.ShipMechanism,delivery,true); }
            var lever=anim.GetComponentInParent<StartMatchLever>();
            if(lever!=null) { AudioRegistry.Source(anim.audioToPlay,Cue.Lever,lever,true); AudioRegistry.Source(anim.audioToPlayB,Cue.Lever,lever,true); }
            var trap=anim.GetComponentInParent<SpikeRoofTrap>();
            if(trap!=null) { AudioRegistry.Source(anim.audioToPlay,Cue.SpikeSlam,trap,true); AudioRegistry.Source(anim.audioToPlayB,Cue.SpikeSlam,trap,true); }
        }
        if(component is AnimatedObjectTrigger trigger)
        {
            Cue cue=Cue.Mechanism;
            for(var t=trigger.transform;t!=null;t=t.parent)
            {
                string name=t.name;
                if(name.Contains("Drawers")) { cue=Cue.Cabinet; break; }
                if(name.Contains("Cabinet") || name.Contains("Locker") || name.Contains("StorageCloset") || name.Contains("StorageShelf") || name.Contains("Fridge")) { cue=Cue.CabinetDoor; break; }
                if(t.GetComponent<StartMatchLever>()!=null) { cue=Cue.Lever; break; }
            }
            if(trigger.doorType>=0) cue=Cue.Door;
            AudioRegistry.Source(trigger.thisAudioSource,cue,trigger,true);
            AudioRegistry.Clips(trigger.boolTrueAudios,trigger.doorType>=0 ? Cue.DoorOpen : cue);
            AudioRegistry.Clips(trigger.boolFalseAudios,trigger.doorType>=0 ? Cue.DoorClose : cue);
            AudioRegistry.Clips(trigger.secondaryAudios,cue); AudioRegistry.Clips(trigger.playWhileTrue,cue);
        }
        if((component is GrabbableObject || component is SpikeRoofTrap) && scanned.Count<2048 && scanned.Add(component))
            foreach(var source in component.GetComponentsInChildren<AudioSource>(true)) Source(source,component);
    }
    // Called only with a newly instantiated explosion prefab; never searches a scene.
    public static void Spawned(GameObject instance)
    {
        try { foreach(var source in instance.GetComponentsInChildren<AudioSource>(true)) Source(source,instance.transform); AudioRegistry.Prime(); }
        catch(System.Exception e) { Debug.LogWarning("Sensus spawned audio registration: "+e.Message); }
    }
}
