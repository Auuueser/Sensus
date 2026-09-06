using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine;
using Sensus.Captions;

namespace Sensus.Audio;

internal sealed class VoiceActivity
{
    private readonly Dictionary<int,(string Raw,string Safe)> names=new();
    private readonly float[] replaySamples=new float[128];
    private readonly Dictionary<int,(AudioSource Source,float Last,int Frames)> replayEvidence=new();
    private int receiverCursor;
    private string Speaker(PlayerControllerB player)
    {
        int id=player.GetInstanceID(); string raw=player.playerUsername ?? "";
        if(names.TryGetValue(id,out var entry) && entry.Raw==raw) return entry.Safe;
        if(names.Count>=128) names.Clear();
        string safe=SpeechText.SafeName(raw);
        names[id]=(raw,safe); return safe;
    }
    internal void Tick(AudioCapture capture)
    {
        var round=StartOfRound.Instance;
        var local=GameNetworkManager.Instance?.localPlayerController;
        if(round==null || local==null || round.allPlayerScripts==null) return;
        int count=System.Math.Min(64,round.allPlayerScripts.Length);
        for(int i=0;i<count;i++)
        {
            var player=round.allPlayerScripts[i];
            if(player==null || player==local || !player.isPlayerControlled || player.isPlayerDead) continue;
            var state=player.voicePlayerState; var source=player.currentVoiceChatAudioSource;
            if(state==null || source==null || !SpeechText.Active(player==local,player.isPlayerDead,state.IsSpeaking,state.IsLocallyMuted,state.Volume,state.Amplitude)) continue;
            if(SoundManager.Instance!=null && player.playerClientId<(ulong)SoundManager.Instance.playerVoiceVolumes.Length && SoundManager.Instance.playerVoiceVolumes[(int)player.playerClientId]<=-60) continue;
            string name=Speaker(player);
            // Amplitude already gates real speech in Active. Do not apply a second,
            // arbitrary amplitude multiplier to the spatial audibility estimate.
            float volume=state.Volume;
            var sound=SoundManager.Instance;
            int playerIndex=(int)player.playerClientId;
            if(sound!=null && sound.volumeFilterNames!=null && playerIndex>=0 && playerIndex<sound.volumeFilterNames.Length &&
                sound.diageticMixer!=null && sound.diageticMixer.GetFloat(sound.volumeFilterNames[playerIndex],out float voiceDb))
                volume*=Mathf.Pow(10f,voiceDb/20f);
            bool heldRadio=player.speakingToWalkieTalkie && local.holdingWalkieTalkie && source.spatialBlend<=0.1f;
            capture.ObserveVoice(source,heldRadio ? Cue.RadioVoice : Cue.Voice,player.GetInstanceID(),name,volume,heldRadio);
            if(!player.speakingToWalkieTalkie) continue;
            // Observe the original receiver's actual replay mapping. A powered radio
            // alone or a pressed PTT button is not evidence that speech is audible.
            var radios=WalkieTalkie.allWalkieTalkies;
            for(int r=0;r<System.Math.Min(radios.Count,16);r++)
            {
                var receiver=radios[(receiverCursor+r)%radios.Count];
                if(receiver==null || !receiver.isBeingUsed || !receiver.isActiveAndEnabled || receiver.playerHeldBy==local) continue;
                if(receiver.audioSourcesReceiving.TryGetValue(source,out var replay) && replay!=null && replay.isActiveAndEnabled && replay.isPlaying && !replay.mute)
                {
                    replay.GetOutputData(replaySamples,0);
                    int id=replay.GetInstanceID(); float now=Time.unscaledTime;
                    bool valid=StreamEvidence.HasModulation(replaySamples);
                    int frames=valid ? 1 : 0;
                    if(valid && replayEvidence.TryGetValue(id,out var prior) && prior.Source==replay && now-prior.Last<0.2f)
                        frames=System.Math.Min(2,prior.Frames+1);
                    if(replayEvidence.Count>=128 && !replayEvidence.ContainsKey(id)) replayEvidence.Clear();
                    replayEvidence[id]=(replay,now,frames);
                    if(frames>=2)
                        capture.ObserveVoice(replay,Cue.GroundRadioVoice,player.GetInstanceID(),name,volume,false);
                }
            }
        }
        receiverCursor=(receiverCursor+16)%64;
    }
    internal void Clear() { names.Clear(); replayEvidence.Clear(); System.Array.Clear(replaySamples,0,replaySamples.Length); receiverCursor=0; }
}
