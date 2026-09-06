using Sensus.Captions;
using UnityEngine;

namespace Sensus.Audio;

internal static class CueResolver
{
    private static bool Contains(AudioClip[] clips, AudioClip clip)
    {
        if (clips == null) return false;
        foreach (var candidate in clips) if (candidate == clip) return true;
        return false;
    }
    internal static bool TryResolve(AudioSource source, AudioClip clip, out Cue cue)
    {
        var local=GameNetworkManager.Instance?.localPlayerController;
        if(HUDManager.Instance!=null && source==HUDManager.Instance.UIAudio && InteractionAudio.LocalMaskClip(clip))
        { cue=Cue.MaskAttachLocal; return true; }
        var item=source.GetComponentInParent<GrabbableObject>();
        if(CompanyAudio.Resolve(source,clip,out cue)) return true;
        if(FeedbackFifth.Resolve(source,clip,out cue)) return true;
        if(FeedbackFourth.Resolve(source,clip,out cue)) return true;
        if(FeedbackRound3.Resolve(source,clip,out cue)) return true;
        var movingMask=source.GetComponentInParent<MaskedPlayerEnemy>();
        if(movingMask!=null && source==movingMask.movementAudio && AudioRegistry.IsSurfaceStep(clip))
        { cue=Cue.Footsteps; return true; }
        if(FeedbackAudioBindings.Resolve(source,clip,out cue)) return true;
        // These item handling clips contain an audible toy call, not just a grab tap.
        cue=AcousticDetails.Resolve(clip.name);
        if(cue is Cue.ToyTrain or Cue.DuckQuack) return true;
        // GrabObject plays before held ownership settles, on the player's itemAudio.
        var actor=source.GetComponentInParent<GameNetcodeStuff.PlayerControllerB>();
        if(actor!=null && StartOfRound.Instance!=null && source==actor.itemAudio &&
            (Contains(StartOfRound.Instance.playerGrabSFX,clip) ||
             (actor.currentlyGrabbingObject!=null && clip==actor.currentlyGrabbingObject.itemProperties.grabSFX) ||
             (actor.currentlyHeldObjectServer!=null && clip==actor.currentlyHeldObjectServer.itemProperties.grabSFX)))
        { cue=Cue.ItemPickup; return true; }
        if(item!=null && item.itemProperties!=null && clip==item.itemProperties.grabSFX)
        { cue=Cue.ItemPickup; return true; }
        var hurtEnemy=source.GetComponentInParent<EnemyAI>();
        if(hurtEnemy!=null && hurtEnemy.enemyType!=null &&
            (clip==hurtEnemy.enemyType.hitBodySFX || clip==hurtEnemy.enemyType.hitEnemyVoiceSFX))
        { cue=Cue.CreatureHit; return true; }
        if(item is HauntedMaskItem mask && clip==mask.maskAttachAudio) { cue=Cue.MaskSound; return true; }
        if(item!=null && item.itemProperties!=null && clip==item.itemProperties.dropSFX) { cue=Cue.ItemDrop; return true; }
        if(item is SprayPaintItem spray && spray.isWeedKillerSprayBottle && (clip==spray.spraySFX || clip==spray.sprayStart || clip==spray.sprayStop)) { cue=Cue.WeedSpray; return true; }
        if(item is KiwiBabyItem egg)
        {
            if(clip==egg.peepAudio) { cue=Cue.EggCall; return true; }
            if(clip==egg.screamAudio) { cue=Cue.EggCry; return true; }
            if(clip==egg.scream3SFX) { cue=Cue.EggScream; return true; }
            if(clip==egg.breakEggSFX) { cue=Cue.EggBreak; return true; }
        }
        if(AuditAudioBindings.Resolve(source,clip,out cue)) return true;
        cue=AcousticDetails.Resolve(clip.name);
        if(cue!=Cue.Creature) return true;
        if(AudioRegistry.ContextCue(source,clip,out cue)) return true;
        if(AuditAudioBindings.NativeClip(clip,out cue) || SupplementalAudio.Clip(clip,out cue)) return true;
        var masked=source.GetComponentInParent<MaskedPlayerEnemy>();
        if(masked!=null && masked.enemyType.audioClips!=null)
        {
            var clips=masked.enemyType.audioClips;
            if((clips.Length>0 && clip==clips[0]) || (clips.Length>1 && clip==clips[1]) || (clips.Length>2 && clip==clips[2]))
            { cue=masked.inSpecialAnimationWithPlayer==local && local!=null ? Cue.LocalMaskInfection : Cue.MaskInfection; return true; }
        }
        var centipede=source.GetComponentInParent<CentipedeAI>();
        if(centipede!=null)
        {
            if(source==centipede.clingingToPlayer2DAudio) { cue=Cue.Cling; return true; }
            if(clip==centipede.clingToPlayer3D) { cue=Cue.OtherCling; return true; }
        }
        var flower=source.GetComponentInParent<FlowermanAI>();
        if(flower!=null && clip==flower.crackNeckSFX)
        { cue=flower.inSpecialAnimationWithPlayer!=null && flower.inSpecialAnimationWithPlayer!=local ? Cue.OtherNeckSnap : Cue.NeckSnap; return true; }
        if(AudioRegistry.Owner(source) is RedLocustBees bees)
        {
            if(source==bees.beesIdle) { cue=Cue.Buzz; return true; }
            if(source==bees.beesDefensive) { cue=Cue.BuzzAlert; return true; }
            if(source==bees.beesAngry) { cue=Cue.BuzzAttack; return true; }
        }
        if(AudioRegistry.KnownSource(source,clip,out cue)) return true;
        cue = Cue.Creature;
        var gun = source.GetComponentInParent<ShotgunItem>();
        if (gun != null)
        {
            cue = source == gun.gunShootAudio ? Cue.Gunshot : source == gun.gunBulletsRicochetAudio ? Cue.Ricochet :
                clip == gun.gunReloadSFX || clip == gun.gunReloadFinishSFX ? Cue.Reload : Cue.GunClick;
            return true;
        }
        var enemy = source.GetComponentInParent<EnemyAI>();
        if(enemy is FlowermanAI bracken)
        {
            if(clip == bracken.crackNeckSFX || source == bracken.crackNeckAudio) { cue=Cue.NeckSnap; return true; }
            if(source == bracken.creatureAngerVoice) { cue=Cue.Growl; return true; }
        }
        if (enemy is MouthDogAI dog)
        {
            if (clip == dog.screamSFX) cue = Cue.Roar;
            else if (clip == dog.breathingSFX) cue = Cue.Breathing;
            else if (clip == dog.killPlayerSFX) cue = Cue.Impact;
            else if (dog.enemyBehaviourStates.Length > 1 && clip == dog.enemyBehaviourStates[1].VoiceClip) cue = Cue.Growl;
            else if (dog.enemyBehaviourStates.Length > 3 && clip == dog.enemyBehaviourStates[3].SFXClip) cue = Cue.Lunge;
            return true;
        }
        if (enemy is SandWormAI worm)
        {
            if (Contains(worm.groundRumbleSFX, clip)) cue = Cue.GroundRumble;
            else if (Contains(worm.ambientRumbleSFX, clip)) cue = Cue.Underground;
            else if (clip == worm.emergeFromGroundSFX) cue = Cue.Emerge;
            else if (clip == worm.hitGroundSFX) cue = Cue.GroundImpact;
            else if (Contains(worm.roarSFX, clip)) cue = Cue.Roar;
            return true;
        }
        if (enemy is JesterAI jester)
        {
            cue = clip == jester.popGoesTheWeaselTheme ? Cue.Music : clip == jester.popUpSFX ? Cue.BoxOpen :
                clip == jester.screamingSFX ? Cue.Screaming : clip == jester.killPlayerSFX ? Cue.Impact : Cue.Creature;
            return true;
        }
        if (enemy is NutcrackerEnemyAI nut)
        {
            cue = clip == nut.aimSFX ? Cue.Aim : source == nut.torsoTurnAudio ? Cue.MechanicalTurn :
                nut.enemyType.audioClips.Length > 2 && clip == nut.enemyType.audioClips[2] ? Cue.Reload :
                clip == nut.kickSFX || clip == nut.dieSFX ? Cue.Impact : Cue.MechanicalAlert;
            return true;
        }
        if (enemy is LassoManAI || enemy is TestEnemy) return false;
        return AudioRegistry.Resolve(source, clip, out cue, out _);
    }
}
