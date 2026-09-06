using UnityEngine;
using Sensus.Captions;

namespace Sensus.Audio;

internal static class AnimationAudioBindings
{
    internal static void Register(Component component)
    {
        if(component is not PlayAudioAnimationEvent animation) return;
        var enemy=animation.GetComponentInParent<EnemyAI>();
        if(enemy is SpringManAI or CrawlerAI or HoarderBugAI or JesterAI or NutcrackerEnemyAI or BushWolfEnemy or MouthDogAI or PufferAI)
        {
            var step=enemy switch { BushWolfEnemy => Cue.WolfFootsteps, JesterAI => Cue.MechanicalTurn, NutcrackerEnemyAI => Cue.BootFootsteps, HoarderBugAI => Cue.SkitterFootsteps, SpringManAI => Cue.BareFootsteps, CrawlerAI or MouthDogAI => Cue.HeavyFootsteps, _ => Cue.Footsteps };
            // These exact arrays were verified against the same-GameObject animator.
            AudioRegistry.Clips(animation.randomClips,step);
            AudioRegistry.Source(animation.audioToPlay,step,enemy,true);
            AudioRegistry.ContextClips(animation.audioToPlay,animation.randomClips,step);
        }
        if(enemy is JesterAI)
        {
            AudioRegistry.Clips(animation.randomClips2,Cue.HeavyFootsteps);
            AudioRegistry.Clips(animation.audioClip,Cue.BareFootsteps);
        }
        if(enemy is CaveDwellerAI)
        {
            // V81 BabyAnimContainer.randomClips holds BabyFootstep1/2/3.
            AudioRegistry.Clips(animation.randomClips,Cue.SmallFootsteps);
            AudioRegistry.Source(animation.audioToPlay,Cue.SmallFootsteps,enemy,true);
        }
        else if(enemy is ForestGiantAI)
        {
            AudioRegistry.Clips(animation.randomClips,Cue.HeavyFootsteps);
            AudioRegistry.Clips(animation.randomClips2,Cue.HeavyFootsteps);
            AudioRegistry.Clips(animation.audioClip,Cue.Roar);
            AudioRegistry.Clips(animation.audioClip2,Cue.Bite);
            AudioRegistry.Source(animation.audioToPlay,Cue.HeavyFootsteps,enemy,true);
            AudioRegistry.Source(animation.audioToPlayB,Cue.HeavyFootsteps,enemy,true);
        }
        else if(enemy is DressGirlAI)
        {
            // V81 AnimContainer owns SkipWalk1..6, not DressGirlAI.skipWalkSFX.
            AudioRegistry.Clips(animation.randomClips,Cue.SkippingFootsteps);
            AudioRegistry.Source(animation.audioToPlay,Cue.Creature,enemy,true);
        }
        else if(enemy is FlowermanAI)
        {
            AudioRegistry.Clips(animation.randomClips,Cue.Rustling);
            AudioRegistry.Clips(animation.randomClips2,Cue.Footsteps);
            AudioRegistry.Clips(animation.audioClip,Cue.Creature);
        }
    }
}
