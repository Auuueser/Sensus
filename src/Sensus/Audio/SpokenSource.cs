using UnityEngine;
using Sensus.Captions;
namespace Sensus.Audio;
internal static class SpokenSource
{
    // A shared or radio-relayed clip is not permission to display remotely sourced dialogue.
    internal static int Rank(AudioSource source,AudioClip clip,bool oneShot)
    {
        if(!SpokenTracks.MatchesLength(ClipNames.Get(clip),clip.length)) return 0;
        var round=StartOfRound.Instance;
        if(oneShot && round!=null && source==round.speakerAudioSource &&
            (clip==round.shipIntroSpeechSFX || clip==round.zeroDaysLeftAlertSFX || clip==round.firedVoiceSFX)) return 3;
        var owner=AudioRegistry.Owner(source);
        if(oneShot && owner is DepositItemsDesk desk && source==desk.speakerAudio) return 2;
        if(!oneShot && owner is TVScript tv && source==tv.tvSFX && tv.tvOn) return 1;
        return 0;
    }
}
