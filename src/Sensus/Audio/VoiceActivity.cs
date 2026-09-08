using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.Audio;
using Sensus.Captions;

namespace Sensus.Audio;

internal sealed class VoiceActivity
{
    internal const int PlayerBudget = 64, PairBudget = 64, FreshSampleBudget = 16;
    internal int LastPcmSamples { get; private set; }
    internal int LastPairsChecked { get; private set; }
    private readonly BoundedCache<int,(string Raw,string Safe)> names=new(128);
    private readonly float[] replaySamples=new float[128];
    private readonly Pending[] pending=new Pending[FreshSampleBudget];
    private readonly HashSet<AudioSource> sampled=new(FreshSampleBudget*2);
    private struct Pending
    {
        internal PlayerControllerB Player;
        internal WalkieTalkie Receiver;
        internal AudioSource Original, Replay;
        internal float At;
    }
    private int playerCursor, pendingCount;
    private long pairCursor;
    private AudioMixer? mixer;
    private int missingChannels, queriedChannels;
    private readonly float[] gains=new float[4];
    private static readonly string[] NativeVolumeNames={"PlayerVolume0","PlayerVolume1","PlayerVolume2","PlayerVolume3"};

    private string Speaker(PlayerControllerB player)
    {
        int id=player.GetInstanceID(); string raw=player.playerUsername ?? "";
        if(names.TryGetValue(id,out var entry) && entry.Raw==raw) return entry.Safe;
        string safe=SpeechText.SafeName(raw);
        names.Set(id,(raw,safe)); return safe;
    }
    private float MixerGain(AudioSource source)
    {
        var sound=SoundManager.Instance;
        var current=sound!=null ? sound.diageticMixer : null;
        if(current!=mixer) { mixer=current; missingChannels=queriedChannels=0; }
        var group=source.outputAudioMixerGroup;
        if(mixer==null || group==null || group.audioMixer!=mixer || sound!.playerVoiceMixers==null) return 1f;
        // Expanded slots may share a native group, while volume is applied on the
        // source (MoreCompany). Resolve the actual route, never synthesize a name
        // from playerClientId or trust an expanded volumeFilterNames array.
        for(int channel=0;channel<System.Math.Min(4,sound.playerVoiceMixers.Length);channel++)
        {
            if(group!=sound.playerVoiceMixers[channel]) continue;
            int bit=1<<channel;
            if((missingChannels & bit)!=0) return 1f;
            if((queriedChannels & bit)==0)
            {
                queriedChannels|=bit;
                if(!mixer.GetFloat(NativeVolumeNames[channel],out float db))
                { missingChannels|=bit; return 1f; }
                gains[channel]=Mathf.Pow(10f,db/20f);
            }
            return gains[channel];
        }
        // Custom routes have no proven exposed parameter. Source/state volume
        // and the existing spatial audibility gate still apply.
        return 1f;
    }
    private static bool Active(PlayerControllerB player,PlayerControllerB local,out AudioSource source)
    {
        source=null!;
        if(player==null || player==local || !player.isPlayerControlled || player.isPlayerDead) return false;
        var state=player.voicePlayerState; source=player.currentVoiceChatAudioSource;
        return state!=null && source!=null && SpeechText.Active(false,false,state.IsSpeaking,state.IsLocallyMuted,state.Volume,state.Amplitude);
    }
    private static bool ReceiverReady(WalkieTalkie receiver,PlayerControllerB local) =>
        receiver!=null && receiver.isBeingUsed && receiver.isActiveAndEnabled && receiver.playerHeldBy!=local && receiver.audioSourcesReceiving!=null;
    private static bool ReplayReady(AudioSource replay) => replay!=null && replay.isActiveAndEnabled && replay.isPlaying && !replay.mute && replay.volume>0;

    internal void Tick(AudioCapture capture)
    {
        LastPcmSamples=LastPairsChecked=0;
        var round=StartOfRound.Instance;
        var local=GameNetworkManager.Instance?.localPlayerController;
        if(round==null || local==null || round.allPlayerScripts==null) return;
        queriedChannels=0;
        bool anyRadio=false;
        int count=System.Math.Min(PlayerBudget,round.allPlayerScripts.Length);
        for(int i=0;i<count;i++)
        {
            if(playerCursor>=round.allPlayerScripts.Length) playerCursor=0;
            var player=round.allPlayerScripts[playerCursor++];
            if(!Active(player,local,out var source)) continue;
            float volume=player.voicePlayerState.Volume*MixerGain(source);
            bool heldRadio=player.speakingToWalkieTalkie && local.holdingWalkieTalkie && source.spatialBlend<=0.1f;
            capture.ObserveVoice(source,heldRadio ? Cue.RadioVoice : Cue.Voice,player.GetInstanceID(),Speaker(player),volume,heldRadio);
            anyRadio|=player.speakingToWalkieTalkie;
        }
        sampled.Clear();
        float now=Time.unscaledTime;
        // Confirm on the next tick rather than waiting for a crowded full sweep.
        for(int i=0;i<pendingCount;i++)
        {
            var p=pending[i]; pending[i]=default;
            if(now-p.At>=0.2f || now<=p.At || !Active(p.Player,local,out var original) || !p.Player.speakingToWalkieTalkie ||
                original!=p.Original || !ReceiverReady(p.Receiver,local) ||
                !p.Receiver.audioSourcesReceiving.TryGetValue(original,out var replay) || replay!=p.Replay || !ReplayReady(replay) || !sampled.Add(replay)) continue;
            LastPcmSamples++; replay.GetOutputData(replaySamples,0);
            if(StreamEvidence.HasModulation(replaySamples))
                capture.ObserveVoice(replay,Cue.GroundRadioVoice,p.Player.GetInstanceID(),Speaker(p.Player),p.Player.voicePlayerState.Volume*MixerGain(replay),false);
        }
        pendingCount=0;
        var radios=WalkieTalkie.allWalkieTalkies;
        if(radios!=null && radios.Count>0 && (anyRadio || round.allPlayerScripts.Length>PlayerBudget))
        {
            int slots=round.allPlayerScripts.Length;
            long pairs=(long)radios.Count*slots;
            int fresh=0;
            for(int attempt=0;attempt<System.Math.Min(PairBudget,pairs) && fresh<FreshSampleBudget;attempt++)
            {
                LastPairsChecked++;
                if(pairCursor>=pairs) pairCursor=0;
                long pair=pairCursor++;
                // Stable slot coordinates prevent phase-lock starvation as the
                // direct-voice window and speaking membership change each tick.
                var player=round.allPlayerScripts[(int)(pair%slots)];
                if(!Active(player,local,out var original) || !player.speakingToWalkieTalkie) continue;
                var receiver=radios[(int)(pair/slots)];
                if(!ReceiverReady(receiver,local)) continue;
                if(!receiver.audioSourcesReceiving.TryGetValue(original,out var replay) || !ReplayReady(replay) || !sampled.Add(replay)) continue;
                fresh++;
                LastPcmSamples++; replay.GetOutputData(replaySamples,0);
                if(StreamEvidence.HasModulation(replaySamples))
                    pending[pendingCount++]=new Pending { Player=player,Receiver=receiver,Original=original,Replay=replay,At=now };
            }
        }
    }
    internal void Clear()
    {
        names.Clear(); sampled.Clear();
        System.Array.Clear(pending,0,pending.Length);
        System.Array.Clear(replaySamples,0,replaySamples.Length);
        playerCursor=pendingCount=0; pairCursor=0; mixer=null; missingChannels=queriedChannels=0;
        LastPcmSamples=LastPairsChecked=0;
    }
}
