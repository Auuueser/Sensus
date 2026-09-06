using UnityEngine;
using Sensus.Captions;

namespace Sensus.Audio;

// Classify the clip actually playing. No attack/death/countdown state is inferred.
internal static class FeedbackRound3
{
    [System.ThreadStatic] internal static bool DrowningPlayback;
    internal static bool Resolve(AudioSource source,AudioClip clip,out Cue cue)
    {
        cue=Cue.Creature;
        var detail=AcousticDetails.Resolve(clip.name);
        if(detail is Cue.ZedDog or Cue.EggCry or Cue.EggScream or Cue.EggBreak)
        { cue=detail; return true; }
        if(AudioRegistry.Owner(source) is MicrowaveItem microwave && (clip==microwave.microwaveOpen || clip==microwave.microwaveClose))
        { cue=Cue.MicrowaveDoor; return true; }
        var enemy=source.GetComponentInParent<EnemyAI>();
        if(enemy!=null && (source==enemy.creatureVoice || source==enemy.creatureSFX))
        {
            if(clip==enemy.dieSFX) { cue=Cue.CreatureDeath; return true; }
            if(enemy is BaboonBirdAI bird && bird.enemyType!=null && bird.enemyType.audioClips!=null)
            {
                var clips=bird.enemyType.audioClips;
                if((clips.Length>4 && clip==clips[4]) || (clips.Length>5 && clip==clips[5]))
                { cue=Cue.CreatureAttack; return true; }
            }
            if(enemy is MouthDogAI dog && clip==dog.killPlayerSFX || enemy is JesterAI jester && clip==jester.killPlayerSFX ||
                enemy is HoarderBugAI bug && clip==bug.hitPlayerSFX)
            { cue=Cue.CreatureAttack; return true; }
        }
        if(enemy is GiantKiwiAI kiwi && kiwi.attackSFX!=null)
            foreach(var attack in kiwi.attackSFX) if(clip==attack) { cue=Cue.CreatureAttack; return true; }
        var vehicle=AudioRegistry.Owner(source) as VehicleController ?? source.GetComponentInParent<VehicleController>();
        if(vehicle!=null && clip==vehicle.jumpInCarSFX) { cue=Cue.VehicleJump; return true; }
        var tzp=source.GetComponentInParent<TetraChemicalItem>();
        if(tzp!=null)
        {
            if(clip==tzp.removeCanSFX) { cue=Cue.TzpRelease; return true; }
            if(clip==tzp.outOfGasSFX) { cue=Cue.TzpEmpty; return true; }
        }
        var round=StartOfRound.Instance;
        if(round!=null)
        {
            if(source==round.startGameWhir || source==round.shipLandingAudio)
            { cue=Cue.ShipLanding; return true; }
            if(clip==round.shipDepartSFX) { cue=Cue.ShipTravel; return true; }
            if(clip==round.shipArriveSFX) { cue=Cue.ShipArrival; return true; }
            var local=GameNetworkManager.Instance?.localPlayerController;
            // The playback wrapper identifies the actual native oxygen-alert call site.
            // Never read the oxygen timer or synthesize a warning from being underwater.
            if(DrowningPlayback && local!=null && HUDManager.Instance!=null && source==HUDManager.Instance.UIAudio && clip==round.HUDSystemAlertSFX)
            { cue=Cue.Drowning; return true; }
        }
        return false;
    }
}
