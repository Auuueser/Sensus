using UnityEngine;
using Sensus.Captions;

namespace Sensus.Audio;

internal static class AuditAudioBindings
{
    internal static void Register(Component component)
    {
        if(component is AudioReverbTrigger trigger && trigger.audioChanges!=null)
            foreach(var change in trigger.audioChanges)
                if(change.audio!=null && AudioRegistry.Owner(change.audio)==null)
                {
                    var clip=change.changeToClip!=null ? change.changeToClip : change.audio.clip;
                    var cue=Cue.Environment;
                    if(clip!=null && !NativeClip(clip,out cue) && !SupplementalAudio.Clip(clip,out cue)) cue=Cue.Environment;
                    AudioRegistry.Source(change.audio,cue,trigger,false);
                }
        if(component is SoundManager manager && manager.currentLevelAmbience!=null)
        {
            var library=manager.currentLevelAmbience;
            AudioRegistry.Clips(library.insideAmbience,Cue.Environment);
            AudioRegistry.Clips(library.outsideAmbience,Cue.Environment);
            AudioRegistry.Clips(library.shipAmbience,Cue.Environment);
            AudioRegistry.Clips(library.insanityMusicAudios,Cue.TensionMusic);
            RegisterAmbient(library.insideAmbienceInsanity);
            RegisterAmbient(library.outsideAmbienceInsanity);
            RegisterAmbient(library.shipAmbienceInsanity);
        }
    }
    private static void RegisterAmbient(RandomAudioClip[] clips)
    { if(clips!=null) foreach(var clip in clips) AudioRegistry.Clips(clip.audioClip,Cue.Environment); }

    internal static bool Resolve(AudioSource source,AudioClip clip,out Cue cue)
    {
        var owner=AudioRegistry.Owner(source);
        cue=Cue.Creature;
        // Resolve shared assets by their actual emitter before global clip fallback.
        if(owner is CaveDwellerAI baby && Contains(baby.scaredBabyVoiceSFX,clip)) { cue=Cue.Crying; return true; }
        if(owner is BaboonBirdAI bird && Contains(bird.cawLaughSFX,clip)) { cue=Cue.Cackle; return true; }
        if(owner is ExtensionLadderItem ladder && (clip==ladder.hitWall || clip==ladder.hitRoof)) { cue=Cue.Impact; return true; }
        if(owner is LoopShapeKey heart && source==heart.repeatingAudioSource && (clip==heart.audioOn || clip==heart.audioOff))
        { cue=Cue.WorldHeartbeat; return true; }
        // Libraries can be replaced when the moon changes, after component Start.
        if(SoundManager.Instance is SoundManager sound)
        {
            if(source==sound.ambienceAudio) { cue=Cue.Environment; return true; }
            if(source==sound.ambienceAudioNonDiagetic) { cue=Cue.TensionMusic; return true; }
        }
        return false;
    }
    private static bool Contains(AudioClip[] clips,AudioClip clip)
    { if(clips!=null) foreach(var candidate in clips) if(candidate==clip) return true; return false; }

    internal static bool NativeClip(AudioClip clip,out Cue cue)
    {
        if(CreatureSoundCatalog.TryBase(ClipNames.Get(clip),out cue)) return true;
        cue=ClipNames.Get(clip) switch
        {
            "LightOn" or "LightOff" or "LightFlicker" or "NeonLightOn" or "NeonLightOff" or "NeonLightFlicker" => Cue.LightSwitch,
            "MaskLaugh1" or "MaskLaugh2" or "MaskLaugh3" or "Laugh1" => Cue.MaskLaugh,
            "MaskCry1" or "MaskCry2" or "MaskCry3" or "MaskCry4" => Cue.Crying,
            "BunnyHop" => Cue.Jump,
            "GiantKiwiDie2" or "KillBloomEnemy" => Cue.CreatureDeath,
            "BalloonPop" or "BalloonPopReverb" => Cue.Burst,
            "WavesHitting" or "WavesHitting2" => Cue.Liquid,
            "BigMachineRoom3" or "UVLightHum" => Cue.PowerHum,
            "MeteorLandClose" or "MeteorLandFar" or "MeteorLandInFactory" => Cue.MeteorImpact,
            "MeteorApproachingA" or "MeteorApproachingB" or "MeteorApproachingC" or "MeteorScream1" => Cue.MeteorApproach,
            "Rain" or "StormyRain" or "Blizzard" or "Forest" or "CalmWater" or "WaterAmbience" or "CaveWaterAmbience" or "EclipseAmbience" or "LowJungleAmbient" or "NighttimeAmbientDesert1" or "NighttimeAmbientForest1" or "NighttimeAmbientForestSwamp" => Cue.Environment,
            "Waterfall" or "WaterfallInside" or "CaveWaterTrickle1" or "CaveWaterTrickle2" or "WaterTrickle" or "WaterDroplet" or "PoolWaterQuiet" => Cue.Liquid,
            _ => Cue.Creature
        };
        return cue!=Cue.Creature;
    }
}
