using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Sensus.Captions;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sensus.Audio;

internal sealed class AudioCapture : IDisposable
{
    private readonly System.Collections.Generic.Dictionary<int,FootstepCadence> maskedCadences=new();
    private readonly SpokenFrame spoken=new();
    private readonly bool externalDialogue;
    private bool spokenChinese;
    internal string SpokenText { get; private set; }="";
    internal int CaptionScreenBudget { get; set; } = 5;
    internal int OmittedCaptions => captions.Omitted;
    private sealed class Playback
    {
        internal AudioSource Source = null!;
        internal AudioClip Clip = null!;
        internal Cue Cue;
        internal bool Personal, PersonalMovement, MaskedStep, CadenceObserved;
        internal float Scale;
        internal int SpokenRank;
        internal string SpokenClip="";
        internal PlaybackClock Clock = new(0);
        internal bool OneShot, Loop, Paused, Explained;
        internal bool HadDirection, DeviceSpatial;
        internal float LastBearing;
        internal int Group;
        internal bool Radio;
        internal OccludeAudio? Occlusion;
        internal VehicleController? Vehicle;
        internal ItemDropship? Delivery;
        internal GrabbableObject? Item;
        internal AnimationCurve? Rolloff;
        internal AnimationCurve? SpatialCurve;
        internal float RefreshCurveAt;
    }
    internal static AudioCapture? Current { get; private set; }
    private readonly List<Playback> playing = new();
    private readonly List<Playback> ambientSources = new();
    private readonly HashSet<AudioSource> trackedSources = new();
    private Playback? ambientBest;
    private int ambientCursor;
    private readonly Stack<Playback> pool = new();
    private readonly HashSet<Cue> resolvedReported = new(), filteredReported = new(), audibleReported = new();
    private float nextPrune;
    private readonly CaptionBuffer captions = new();
    private readonly VoiceActivity voices = new();
    private readonly Dictionary<int,Playback> voiceSamples = new();
    private readonly SensusSettings settings;
    private readonly ManualLogSource log;
    private int playbackCalls, received, unresolved, rejected, observed;
    private float nextDiagnostic, nextTick;
    private string listenerState = "not-ready";
    private int listenerId;
    private bool observerError, reportedEmptyPlantAudio;
    private readonly Queue<(float time, AudioSource source, AudioClip clip)> recentPlantAudio = new();
    private float nextPlantTrace;
    private CadaverGrowthAI? plantTraceGrowth;
    private SprayPaintItem? plantTraceSprayer;
    private float plantTraceStarted;
    private int plantTraceStage, plantTraceEvents;
    private bool plantTraceLocal;
    internal bool BeginPlantTrace(CadaverGrowthAI growth, SprayPaintItem sprayer)
    {
        if(!settings.Diagnostics.Value || Time.unscaledTime<nextPlantTrace) return false;
        nextPlantTrace=Time.unscaledTime+5f;
        plantTraceGrowth=growth; plantTraceSprayer=sprayer;
        plantTraceStarted=Time.unscaledTime; plantTraceStage=0; plantTraceEvents=0;
        plantTraceLocal=FeedbackFifth.LocalClearing;
        WritePlantTrace("before",growth,sprayer);
        return true;
    }
    internal void WritePlantTrace(string phase,CadaverGrowthAI growth,SprayPaintItem sprayer)
    {
        // Diagnostic only: never turn destruction state into a player-facing sound.
        try
        {
            var source=growth.destroyAudio;
            log.LogInfo($"Cadaver clear trace {phase}: frame={Time.frameCount}, local={plantTraceLocal}, destroyClip={source?.clip?.name ?? "<null>"}, playing={source!=null && source.isPlaying}, sprayClip={sprayer.sprayAudio?.clip?.name ?? "<null>"}");
            TracePlantSource("destroy",source);
            TracePlantSource("spore ambience",growth.sporeAmbienceSource);
            TracePlantSource("plant",growth.plantAudio);
            TracePlantSource("vines",growth.vinesInHeadAudio);
            TracePlantSource("spray",sprayer.sprayAudio);
            foreach(var entry in recentPlantAudio)
                if(Time.unscaledTime-entry.time<=2f && entry.source!=null && entry.clip!=null)
                    log.LogInfo($"Cadaver clear nearby playback: age={Time.unscaledTime-entry.time:F3}, clip={entry.clip.name}, source={entry.source.name}, parent={entry.source.transform.parent?.name}, playing={entry.source.isPlaying}, distance={Vector3.Distance(entry.source.transform.position,sprayer.transform.position):F1}");
        }
        catch(Exception e) { ObserverFailure(e); }
    }
    private void TracePlantSource(string role,AudioSource? source)
    {
        if(source==null) return;
        log.LogInfo($"Cadaver source state: role={role}, clip={source.clip?.name ?? "<null>"}, playing={source.isPlaying}, enabled={source.isActiveAndEnabled}, volume={source.volume:F3}, pitch={source.pitch:F3}, samples={source.timeSamples}");
    }
    private void TickPlantTrace(float now)
    {
        if(plantTraceGrowth==null) return;
        if(!settings.Diagnostics.Value || plantTraceSprayer==null)
        { plantTraceGrowth=null; plantTraceSprayer=null; return; }
        float delay=plantTraceStage==0 ? 0.25f : plantTraceStage==1 ? 1f : 2f;
        if(now-plantTraceStarted<delay) return;
        WritePlantTrace($"delayed +{now-plantTraceStarted:F2}s",plantTraceGrowth,plantTraceSprayer);
        if(++plantTraceStage>=3) { plantTraceGrowth=null; plantTraceSprayer=null; }
    }
    private double tickMilliseconds, peakTickMilliseconds;
    private int timedTicks;
    private readonly HashSet<AudioSource> explicitPlaying = new();
    internal bool Ready { get; set; }
    internal string Text { get; private set; } = "";
    internal SoundField Field { get; } = new();
    internal bool HasLoop(AudioSource source)
    {
        return trackedSources.Contains(source);
    }

    internal AudioCapture(SensusSettings settings, ManualLogSource log)
    {
        this.settings = settings; this.log = log;
        externalDialogue=LanguagePolicy.UseChinese("Auto",BepInEx.Bootstrap.Chainloader.PluginInfos.Keys);
        log.LogInfo(externalDialogue ? "Dialogue owner: LC-Chinese-Project (GUID detected; readiness unverified). Sensus dialogue rendering withheld; translation settings untouched." : "Dialogue owner: Sensus. Timed English/Chinese tracks enabled by Dialogue.Enabled.");
        Current = this;
        SceneManager.sceneUnloaded += SceneUnloaded;
    }
    private void SceneUnloaded(Scene scene)
    {
        recentPlantAudio.Clear(); nextPlantTrace=0;
        plantTraceGrowth=null; plantTraceSprayer=null;
        explicitPlaying.RemoveWhere(s=>s==null || s.gameObject.scene.handle==scene.handle);
        playing.RemoveAll(p => p.Source == null || p.Source.gameObject.scene.handle == scene.handle);
        trackedSources.RemoveWhere(s=>s==null || s.gameObject.scene.handle==scene.handle);
        ambientSources.RemoveAll(p=>p.Source==null || p.Source.gameObject.scene.handle==scene.handle); ambientBest=null;
        captions.Clear(); Field.Clear(); voices.Clear(); voiceSamples.Clear(); Text = ""; SpokenText="";spoken.Clear();
        resolvedReported.Clear(); filteredReported.Clear(); audibleReported.Clear();
    }
    internal static void Record(AudioSource source, AudioClip clip, float scale, bool oneShot, bool humanFootstep=false)
    {
        var current = Current;
        if (current == null || !current.Ready) return;
        try { current.playbackCalls++; current.Add(source, clip, scale, oneShot,humanFootstep); }
        catch (Exception e) { current.ObserverFailure(e); }
    }
    internal static void RecordPlay(AudioSource source, bool explicitPlay = false)
    {
        var current = Current;
        if (current == null || !current.Ready) return;
        try {
            if(source==null) return;
            if(source.clip==null)
            {
                if(explicitPlay && current.settings.Diagnostics.Value && !current.reportedEmptyPlantAudio &&
                    AudioRegistry.Owner(source) is CadaverGrowthAI growth && source==growth.destroyAudio)
                { current.reportedEmptyPlantAudio=true; current.log.LogInfo("Cadaver destroyAudio has no assigned clip; this playback entry cannot emit its assigned recording. Other simultaneous sources are not excluded."); }
                return;
            }
            // isPlaying is also true during PlayOneShot; it cannot prove the assigned clip is playing.
            if(!explicitPlay && AudioRegistry.IsSurfaceStep(source.clip)) return;
            if(AudioRegistry.Owner(source) is MicrowaveItem || source.clip.name=="MicrowaveWhir" || FeedbackFifth.RequiresExplicitPlay(source))
            {
                if(explicitPlay && current.explicitPlaying.Count<128) current.explicitPlaying.Add(source);
                if(!current.explicitPlaying.Contains(source)) return;
            }
            current.playbackCalls++; current.Add(source, source.clip, 1f, false);
        }
        catch (Exception e) { current.ObserverFailure(e); }
    }
    private void ObserverFailure(Exception e)
    {
        if (observerError) return;
        observerError = true;
        log.LogWarning($"Sound observation failed; original playback was preserved: {e.Message}");
    }
    private void Add(AudioSource source, AudioClip clip, float scale, bool oneShot, bool humanFootstep=false)
    {
        if(settings.Diagnostics.Value && source!=null && clip!=null)
        {
            if(plantTraceGrowth!=null && Time.unscaledTime-plantTraceStarted<=2f && plantTraceEvents++<32)
                log.LogInfo($"Cadaver post-clear playback: dt={Time.unscaledTime-plantTraceStarted:F3}, clip={clip.name}, source={source.name}, parent={source.transform.parent?.name}, oneShot={oneShot}, volume={source.volume:F3}");
            if(recentPlantAudio.Count>=32) recentPlantAudio.Dequeue();
            recentPlantAudio.Enqueue((Time.unscaledTime,source,clip));
        }
        if (source == null || clip == null || !source.isActiveAndEnabled) return;
        if(FeedbackFourth.StartupVehicle(source,clip)) return;
        if (!CueResolver.TryResolve(source, clip, out var cue) && !humanFootstep) { unresolved++; return; }
        bool maskedStep=oneShot && (humanFootstep || AudioRegistry.IsSurfaceStep(clip)) && source.GetComponentInParent<MaskedPlayerEnemy>()!=null;
        if(maskedStep) cue=Cue.Footsteps;
        else if(humanFootstep || (AudioRegistry.IsSurfaceStep(clip) && oneShot && source.GetComponentInParent<GameNetcodeStuff.PlayerControllerB>()!=null)) cue=scale>0.8f ? Cue.Running : Cue.Footsteps;
        if(cue is Cue.Footsteps or Cue.Running)
            for(int i=playing.Count-1;i>=0;i--)
                if(playing[i].Source==source && CreatureSoundCatalog.Basis(playing[i].Cue) is Cue.Footsteps or Cue.Running) Retire(i);
        // Receiver-local replay conveys the receiver's position, not the distant
        // creature/item position or a victim identity from the transmitted clip.
        var receivingRadio=source.GetComponentInParent<WalkieTalkie>();
        if(receivingRadio!=null && cue!=Cue.RadioSignal &&
            (source==receivingRadio.target || receivingRadio.audioSourcesReceiving.ContainsValue(source))) cue=Cue.RadioRelay;
        if(cue==Cue.ItemDrop && !InteractionAudio.RecentDrop(source.GetComponentInParent<GrabbableObject>())) return;
        cue=CreatureSoundCatalog.Refine(clip.name,cue);
        bool ambient=cue==Cue.Flies;
        if(ambient)
        {
            foreach(var prior in ambientSources) if(prior.Source==source) return;
            if(ambientSources.Count>=1024) return;
        }
        if (!oneShot) for (int i=playing.Count-1;i>=0;i--) if (playing[i].Source==source && !playing[i].OneShot) Retire(i);
        if (!ambient && playing.Count >= 128)
        {
            int victim=0;
            for(int i=1;i<playing.Count;i++) if(CueText.Priority(playing[i].Cue)<CueText.Priority(playing[victim].Cue)) victim=i;
            if(CueText.Priority(playing[victim].Cue)>CueText.Priority(cue)) return;
            Retire(victim);
        }
        int spokenRank=SpokenSource.Rank(source,clip,oneShot);
        if(spokenRank>0)
            for(int i=playing.Count-1;i>=0;i--) if(playing[i].Source==source && playing[i].SpokenRank>0) Retire(i);
        var playback = pool.Count>0 ? pool.Pop() : new Playback();
        playback.SpokenRank=spokenRank;playback.SpokenClip=spokenRank>0 ? clip.name : "";
        playback.Vehicle=AudioRegistry.Owner(source) as VehicleController ?? source.GetComponentInParent<VehicleController>();
        playback.Delivery=AudioRegistry.Owner(source) as ItemDropship;
        playback.Item=source.GetComponentInParent<GrabbableObject>();
        var actor=source.GetComponentInParent<GameNetcodeStuff.PlayerControllerB>();
        if(actor!=null && source==actor.itemAudio)
            playback.Item=actor.currentlyGrabbingObject!=null ? actor.currentlyGrabbingObject : actor.currentlyHeldObjectServer;
        playback.PersonalMovement=(actor!=null && actor==GameNetworkManager.Instance?.localPlayerController) || (cue==Cue.WaterSplash && FeedbackFourth.LocalSplash) || (cue==Cue.PlantClear && FeedbackFifth.LocalClearing);
        playback.Personal=playback.Item!=null && playback.Item.playerHeldBy!=null && playback.Item.playerHeldBy==GameNetworkManager.Instance?.localPlayerController;
        if(actor!=null && actor==GameNetworkManager.Instance?.localPlayerController && source==actor.itemAudio && cue is Cue.ToyTrain or Cue.DuckQuack or Cue.ZedDog) playback.Personal=true;
        playback.MaskedStep=maskedStep; playback.CadenceObserved=false;
        playback.Source=source; playback.Clip=clip; playback.Cue=cue; playback.Scale=scale; playback.OneShot=oneShot;
        playback.Loop=!oneShot && source.loop; playback.Paused=false; playback.Explained=false; playback.HadDirection=false;
        playback.Clock.Reset(Time.unscaledTime); playback.Occlusion=source.GetComponent<OccludeAudio>();
        playback.Rolloff=null; playback.RefreshCurveAt=0;
        playback.SpatialCurve=source.GetCustomCurve(AudioSourceCurveType.SpatialBlend);
        playback.DeviceSpatial=FeedbackFourth.DeviceDirection(cue) && playback.SpatialCurve!=null &&
            playback.SpatialCurve.length>1 && playback.SpatialCurve.Evaluate(1)>0.1f;
        playback.Group=FeedbackFifth.Group(source,cue); playback.Radio=false;
        if(!oneShot || ambient) trackedSources.Add(source);
        if(ambient) { playback.Group=int.MinValue+1; ambientSources.Add(playback); return; }
        playing.Add(playback);
        if(settings.Diagnostics.Value && resolvedReported.Add(cue))
            log.LogInfo($"Cue resolved: {cue}, clip={clip.name}, source={source.name}, loop={playback.Loop}, scale={scale:F2}, explicitFootstep={humanFootstep}.");
        received++;
        if (received == 1) log.LogInfo("First supported audio playback received by Sensus.");
        // Observe now as well as on ticks, so a short clip is not lost between frames.
        if (CanListen(out var listener)) Observe(playback, listener, Time.unscaledTime);
    }
    private void Retire(int index)
    {
        var item=playing[index]; playing.RemoveAt(index);
        if(!item.OneShot) trackedSources.Remove(item.Source);
        item.Source=null!; item.Clip=null!; item.Occlusion=null; item.Rolloff=null; item.SpatialCurve=null; item.Vehicle=null; item.Delivery=null; item.Item=null;
        if(pool.Count<128) pool.Push(item);
    }
    internal static void Change(AudioSource source, int action)
    {
        var current = Current;
        if (current == null) return;
        try
        {
            if (action == 0) { current.explicitPlaying.Remove(source); for(int i=current.playing.Count-1;i>=0;i--) if(current.playing[i].Source==source) current.Retire(i); }
            else foreach (var p in current.playing) if (p.Source == source)
            {
                p.Clock.Sample(Time.unscaledTime, source.pitch, p.Paused || (AudioListener.pause && !source.ignoreListenerPause));
                p.Paused = action == 1;
            }
        }
        catch (Exception e) { current.ObserverFailure(e); }
    }
    private bool CanListen(out Transform listener)
    {
        listener = null!;
        if (!Ready) { listenerState = "capture-not-ready"; return false; }
        if (!settings.Enabled.Value) { listenerState = "disabled"; return false; }
        if (GameNetworkManager.Instance == null || GameNetworkManager.Instance.isDisconnecting || StartOfRound.Instance == null)
        { listenerState = "not-in-round"; return false; }
        var player = GameNetworkManager.Instance.localPlayerController;
        if (player == null || player.isPlayerDead || !player.isPlayerControlled || StartOfRound.Instance.audioListener == null)
        { listenerState = "local-player-or-listener-unavailable"; return false; }
        listenerState = "ready";
        listener = StartOfRound.Instance.audioListener.transform;
        if (listenerId != listener.GetInstanceID())
        {
            captions.Clear(); Field.Clear(); voices.Clear(); voiceSamples.Clear();
            maskedCadences.Clear(); listenerId = listener.GetInstanceID();
            log.LogInfo("Sensus local sound listener ready.");
        }
        return true;
    }
    internal void Tick(bool chinese)
    {
        float now = Time.unscaledTime;
        if (now < nextTick) return;
        long started=settings.Diagnostics.Value ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        nextTick = now + 1f / 30f;
        TickPlantTrace(now);
        spoken.Clear();spokenChinese=chinese;
        if(now>=nextPrune) { nextPrune=now+2; AudioRegistry.Prune(); InteractionAudio.Prune(); SupplementalAudio.Prune(); PlaybackHooks.PruneRegistrations(); FeedbackFourth.Prune();
            trackedSources.RemoveWhere(s=>s==null);
            for(int i=ambientSources.Count-1;i>=0;i--) if(ambientSources[i].Source==null) ambientSources.RemoveAt(i); }
        NativeAudioDiscovery.Tick();
        captions.BeginFrame();
        bool listen = CanListen(out var listener);
        if(listen) AudioRegistry.RecoverLoops(this);
        if (!listen) { captions.Clear(); Field.Clear(); voices.Clear(); voiceSamples.Clear(); }
        for (int i = playing.Count - 1; i >= 0; i--)
        {
            var p = playing[i];
            if (p.Source == null || p.Clip == null || !p.Source.isActiveAndEnabled) { Retire(i); continue; }
            bool paused = p.Paused || (AudioListener.pause && !p.Source.ignoreListenerPause);
            p.Clock.Sample(now, p.Source.pitch, paused);
            bool expired = !p.Loop && p.Clock.Elapsed >= p.Clip.length;
            bool replaced = !p.OneShot && p.Source.clip != p.Clip;
            if (expired || replaced || (!paused && !p.Source.isPlaying && p.Clock.Elapsed > 0.15f)) { Retire(i); continue; }
            if (listen && !paused) Observe(p, listener, now);
        }
        if(listen) { voices.Tick(this); SampleAmbient(listener,now); }
        SpokenText=listen && settings.SpokenSubtitles.Value && !externalDialogue ? spoken.Text : "";
        bool hybrid = settings.DisplayMode.Value == "Hybrid";
        Text = listen && settings.DisplayMode.Value != "Peripheral" ? captions.Compose(now, settings.CaptionDuration.Value,
            DisplayCapacity.Resolve(settings.MaxCaptions.Value, CaptionScreenBudget), chinese,
            settings.ShowDirection.Value && !hybrid, hybrid) : "";
        if(started!=0)
        {
            double elapsed=(System.Diagnostics.Stopwatch.GetTimestamp()-started)*1000d/System.Diagnostics.Stopwatch.Frequency;
            tickMilliseconds+=elapsed; peakTickMilliseconds=Math.Max(peakTickMilliseconds,elapsed); timedTicks++;
        }
        if (settings.Diagnostics.Value && now >= nextDiagnostic)
        {
            nextDiagnostic = now + 10;
            log.LogInfo($"Capture: listener={listenerState}, playbackCalls={playbackCalls}, resolved={received}, unresolved={unresolved}, filteredSamples={rejected}, audibleSamples={observed}, active={playing.Count}, flySources={ambientSources.Count}, voiceSources={voiceSamples.Count}, captions={captions.Count}; acoustic calibration remains pending.");
            log.LogInfo($"Capture tick CPU: meanMs={(timedTicks>0 ? tickMilliseconds/timedTicks : 0):F3}, maxMs={peakTickMilliseconds:F3}, ticks={timedTicks}, flyBudget={ScanBudget.For(ambientSources.Count,16)}, registeredSources={AudioRegistry.SourceCount}; excludes UI and asynchronous audio callbacks.");
            tickMilliseconds=peakTickMilliseconds=0; timedTicks=0;
        }
    }
    internal void ObserveVoice(AudioSource source,Cue cue,int speakerId,string speaker,float volume,bool radio)
    {
        if(!CanListen(out var listener)) return;
        int key=source.GetInstanceID();
        if(!voiceSamples.TryGetValue(key,out var voiceSample) || voiceSample.Source!=source)
        {
            if(voiceSamples.Count>=128) voiceSamples.Clear();
            voiceSample=new Playback { Source=source, SpatialCurve=source.GetCustomCurve(AudioSourceCurveType.SpatialBlend), Occlusion=source.GetComponent<OccludeAudio>() };
            voiceSamples[key]=voiceSample;
        }
        voiceSample.Cue=cue; voiceSample.Group=cue==Cue.GroundRadioVoice ? source.GetInstanceID() : speakerId;
        voiceSample.Scale=volume; voiceSample.Loop=true; voiceSample.Radio=radio;
        Observe(voiceSample,listener,Time.unscaledTime,speaker);
    }
    private void SampleAmbient(Transform listener,float now)
    {
        // Keep the strongest known audible source while advancing a fixed-size sweep.
        float strongest=ambientBest?.Source!=null ? Observe(ambientBest,listener,now,publish:false) : 0;
        if(strongest<=0) ambientBest=null;
        for(int i=0, budget=ScanBudget.For(ambientSources.Count,16);i<budget;i++)
        {
            if(ambientCursor>=ambientSources.Count) ambientCursor=0;
            var candidate=ambientSources[ambientCursor++];
            if(candidate.Source==null) continue;
            float gain=Observe(candidate,listener,now,publish:false);
            if(gain>strongest*1.1f) { strongest=gain; ambientBest=candidate; }
        }
        if(ambientBest!=null && strongest>0) Observe(ambientBest,listener,now);
    }
    private float Observe(Playback p, Transform listener, float now, string speaker="", bool publish=true)
    {
        var source = p.Source;
        var local=GameNetworkManager.Instance?.localPlayerController;
        if(p.Cue==Cue.Clock && p.Item!=null && local!=null && !LocallyCarried(p.Item,local))
        {
            var holder=p.Item.playerHeldBy;
            bool clockHeld=p.Item.isHeld && holder!=null;
            if(!WorldSoundPolicy.ClockSpace(clockHeld ? holder!.isInHangarShipRoom : p.Item.isInShipRoom,
                clockHeld ? holder!.isInsideFactory : p.Item.isInFactory,local.isInsideFactory)) return 0;
        }
        if(p.Cue is Cue.Teleport or Cue.InverseTeleport && local!=null && local.isInHangarShipRoom &&
            source.GetComponentInParent<ShipTeleporter>()==null) return 0;
        if(p.Cue==Cue.Microwave && !p.OneShot && !explicitPlaying.Contains(source)) return 0;
        if(p.Cue==Cue.LockPicking && p.Item is LockPicker picker && (!picker.isOnDoor || !picker.isPickingLock)) return 0;
        if(p.Cue==Cue.SupplyLanding && (p.Delivery==null || !p.Delivery.shipLanded)) return 0;
        bool riding=p.Vehicle!=null && local!=null &&
            (p.Vehicle.currentDriver==local || p.Vehicle.currentPassenger==local ||
             (p.Vehicle.physicsRegion!=null && local.physicsParent==p.Vehicle.physicsRegion.physicsTransform));
        if(p.Cue==Cue.VehicleEngine && riding) return 0;
        if (!source.isActiveAndEnabled || !source.isPlaying || source.isVirtual || source.mute || Mathf.Abs(source.pitch) < 0.001f || (AudioListener.pause && !source.ignoreListenerPause)) { Reject(p,"not-playing/virtual/muted/paused"); return 0; }
        float distance = Vector3.Distance(listener.position, source.transform.position);
        // V81 giant CloseWideSFX is 2D nearby, blending to 3D over distance.
        // The scalar exposes the first key, not the listener's effective blend.
        var spatialCurve=p.SpatialCurve;
        float blend=p.Radio ? 0 : spatialCurve!=null && spatialCurve.length>1
            ? Mathf.Clamp01(spatialCurve.Evaluate(Mathf.Clamp01(distance/Mathf.Max(0.01f,source.maxDistance))))
            : source.spatialBlend;
        float attenuation;
        if (source.rolloffMode == AudioRolloffMode.Linear) attenuation = AudibilityMath.Linear(distance, source.minDistance, source.maxDistance);
        else if (source.rolloffMode == AudioRolloffMode.Logarithmic) attenuation = AudibilityMath.Logarithmic(distance, source.minDistance, source.maxDistance);
        else
        {
            if (p.Rolloff == null || now >= p.RefreshCurveAt)
            {
                p.Rolloff = source.GetCustomCurve(AudioSourceCurveType.CustomRolloff);
                p.RefreshCurveAt = now + 0.25f;
            }
            var curve = p.Rolloff;
            if (curve == null || curve.length == 0) { Reject(p,"missing-rolloff"); return 0; }
            attenuation = Mathf.Max(0, curve.Evaluate(Mathf.Clamp01(distance / Mathf.Max(0.01f, source.maxDistance))));
        }
        // User master volume is AudioListener.volume: intentionally excluded.
        var soundManager=SoundManager.Instance;
        float gameDb = soundManager!=null && source.outputAudioMixerGroup!=null && source.outputAudioMixerGroup.audioMixer==soundManager.diageticMixer
            ? Mathf.Min(0,soundManager.currentDiageticVolume) : 0f;
        float gain = AudibilityMath.Gain(source.volume, p.Scale, blend, attenuation, gameDb);
        if (float.IsNaN(gain) || float.IsInfinity(gain) || gain < (p.Cue==Cue.Clock ? Mathf.Min(settings.MinimumGain.Value,0.005f) : settings.MinimumGain.Value)) { Reject(p,"below-audibility-threshold"); return 0; }
        if(!publish) return gain;
        bool ownDialogue=p.SpokenRank>0 && settings.SpokenSubtitles.Value && !externalDialogue;
        if(ownDialogue)
        {
            float speechTime=p.OneShot ? p.Clock.Elapsed : source.time;
            spoken.Observe(p.Group,p.SpokenRank,SpokenTracks.At(p.SpokenClip,speechTime,spokenChinese));
        }
        if(p.MaskedStep && !p.CadenceObserved)
        {
            int key=source.GetInstanceID();
            if(maskedCadences.Count>=128 && !maskedCadences.ContainsKey(key)) maskedCadences.Clear();
            maskedCadences.TryGetValue(key,out var cadence);
            p.Cue=cadence.Observe(now); maskedCadences[key]=cadence; p.CadenceObserved=true;
        }
        if(p.Cue==Cue.TensionMusic && p.Explained) return gain;
        // These authored near-2D / far-3D interaction sources still locate an audible device.
        // Never promote a constant 2D source, radio replay, or an out-of-range sound.
        float directionBlend=WorldSoundPolicy.DeviceBlend(p.DeviceSpatial,distance,source.maxDistance,blend);
        bool spatial = directionBlend > 0.1f && !p.Radio;
        bool uncertain = p.Occlusion != null && p.Occlusion.occluded;
        Vector3 direction = Vector3.ProjectOnPlane(source.transform.position - listener.position, Vector3.up);
        int sector = !AudibilityMath.DirectionKnown(directionBlend,p.Radio,direction.sqrMagnitude) ? -1 : CueText.DirectionIndex(Vector3.SignedAngle(Vector3.ProjectOnPlane(listener.forward, Vector3.up), direction, Vector3.up));
        var localPlayer=GameNetworkManager.Instance?.localPlayerController;
        bool localMovement=(localPlayer!=null && (source==localPlayer.movementAudio || source==localPlayer.slimeSlipAudio)) || (p.Cue==Cue.ItemDrop && InteractionAudio.LocalDrop(p.Item));
        bool held=p.Item!=null ? LocallyCarried(p.Item,local) : p.Personal;
        bool carried=IndicatorDetail.Carried(p.Cue,held,p.Radio || p.Cue is Cue.RadioRelay or Cue.RadioVoice or Cue.GroundRadioVoice);
        localMovement=localMovement || (p.PersonalMovement && CuePresentation.ActorFeedback(p.Cue)) || (p.PersonalMovement && CreatureSoundCatalog.Basis(p.Cue) is Cue.DragFootsteps or Cue.Footsteps or Cue.Running or Cue.LadderClimb or Cue.Jump or Cue.Landing) || CuePresentation.Personal(p.Cue,held,riding);
        if(carried) { localMovement=false; sector=-1; spatial=false; p.HadDirection=false; }
        float bearing=Mathf.Atan2(direction.x,direction.z)*Mathf.Rad2Deg;
        if(sector>=0) { p.HadDirection=true; p.LastBearing=bearing; }
        else if(spatial && p.HadDirection && direction.sqrMagnitude<0.04f)
        { bearing=p.LastBearing; sector=CueText.DirectionIndex(bearing-listener.eulerAngles.y); }
        p.Explained=true;
        if(!ownDialogue) captions.Observe(p.Group, p.Cue, now, sector, spatial, p.Loop,localMovement,speaker,
            sector<0 ? (!settings.UnlocatedIndicators.Value || Field.TrayCapacity==0 || (!carried && !CuePresentation.Unlocated(p.Cue))) : !Field.CanDisplaySector(sector));
        float windingSeconds=p.Cue==Cue.Music && !p.OneShot && p.Clip.name=="JackInTheBoxTheme" &&
            p.Clip.frequency==44100 && p.Clip.samples==1890304 ? p.Source.timeSamples/(float)p.Clip.frequency : -1;
        Field.Observe(p.Group, p.Cue, now,
            bearing, sector >= 0, p.Loop, gain,localMovement,
            WindingEnvelope.At(windingSeconds), windingSeconds,carried);
        if(settings.Diagnostics.Value && audibleReported.Add(p.Cue))
            log.LogInfo($"Cue audible: {p.Cue}, clip={p.Clip?.name ?? "voice-stream"}, gain={gain:F3}, blend={blend:F2}, occluded={uncertain}, directionKnown={sector>=0}.");
        if (++observed == 1) log.LogInfo("First supported sound passed the Sensus audibility filter.");
        return gain;
    }
    private static bool LocallyCarried(GrabbableObject item, GameNetcodeStuff.PlayerControllerB? local)
    {
        if(local==null || item==null) return false;
        if(local.ItemSlots!=null)
            foreach(var slot in local.ItemSlots) if(slot==item) return true;
        return item.isHeld && item.playerHeldBy==local;
    }
    private void Reject(Playback p,string reason)
    {
        rejected++;
        if(settings.Diagnostics.Value && filteredReported.Add(p.Cue))
            log.LogInfo($"Cue sample filtered: {p.Cue}, clip={p.Clip?.name ?? "voice-stream"}, reason={reason}, volume={p.Source.volume:F3}. Later samples may become audible.");
    }
    public void Dispose()
    {
        recentPlantAudio.Clear(); plantTraceGrowth=null; plantTraceSprayer=null;
        SceneManager.sceneUnloaded -= SceneUnloaded;
        trackedSources.Clear(); maskedCadences.Clear();
        explicitPlaying.Clear(); playing.Clear(); ambientSources.Clear(); ambientBest=null; ambientCursor=0; pool.Clear(); AudioRegistry.Clear(); FeedbackFourth.Clear(); InteractionAudio.Clear(); SupplementalAudio.Clear(); captions.Clear(); Field.Clear(); voices.Clear(); voiceSamples.Clear(); NativeAudioDiscovery.Clear(); PlaybackHooks.ClearRegistrations(); Text = ""; Ready = false;
        SpokenText="";spoken.Clear();
        if (Current == this) Current = null;
    }
}
