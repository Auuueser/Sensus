using System.Collections.Generic;
using UnityEngine;
using Sensus.Captions;

namespace Sensus.Audio;

// Populated at component lifecycle boundaries, never by scanning the scene.
internal static class AudioRegistry
{
    private sealed class Binding
    {
        internal AudioSource Source = null!;
        internal Component Owner = null!;
        internal Cue Default;
        internal bool Merge;
        internal Dictionary<int,(AudioClip Clip,Cue Cue)>? Context;
    }
    private static readonly Dictionary<int, Binding> sources = new();
    private static readonly Dictionary<int, (AudioClip Clip, Cue Cue, bool Ambiguous)> clips = new();
    private static readonly List<int> retired = new();
    private static readonly List<AudioSource> recovery = new();
    private static readonly List<AudioSource> pending = new();
    private static int recoveryCursor;
    private static bool footstepsRegistered;
    private static readonly HashSet<AudioClip> surfaceSteps=new();
    internal static bool IsSurfaceStep(AudioClip clip) => surfaceSteps.Contains(clip);
    internal static Component? Owner(AudioSource source) => sources.TryGetValue(source.GetInstanceID(),out var b) && b.Source==source ? b.Owner : null;
    internal static int SourceCount => sources.Count;
    internal static void ContextClips(AudioSource? source,AudioClip[]? values,Cue cue)
    {
        if(source==null || values==null || !sources.TryGetValue(source.GetInstanceID(),out var binding)) return;
        binding.Context ??= new();
        foreach(var clip in values) if(clip!=null && binding.Context.Count<128) binding.Context[clip.GetInstanceID()]=(clip,cue);
    }
    internal static bool ContextCue(AudioSource source,AudioClip clip,out Cue cue)
    {
        cue=Cue.Creature;
        if(sources.TryGetValue(source.GetInstanceID(),out var binding) && binding.Source==source && binding.Context!=null && binding.Context.TryGetValue(clip.GetInstanceID(),out var entry) && entry.Clip==clip)
        { cue=entry.Cue; return true; }
        return false;
    }
    internal static bool KnownSource(AudioSource source, AudioClip clip, out Cue cue)
    {
        cue=Cue.Creature;
        if(!sources.TryGetValue(source.GetInstanceID(),out var b) || b.Source!=source || b.Owner==null) return false;
        // Preserve the more specific original four-creature resolver.
        if(b.Owner is MouthDogAI or SandWormAI or JesterAI or NutcrackerEnemyAI or FlowermanAI) return false;
        return Resolve(source,clip,out cue,out _);
    }
    internal static void RegisterFootsteps()
    {
        if(footstepsRegistered || StartOfRound.Instance == null || StartOfRound.Instance.footstepSurfaces == null) return;
        foreach(var surface in StartOfRound.Instance.footstepSurfaces)
        {
            Clips(surface.clips,Cue.Footsteps);
            if(surface.clips!=null) foreach(var clip in surface.clips) if(clip!=null) surfaceSteps.Add(clip);
            Clips(surface.jumpLandSFX,Cue.Landing);
            Clips(surface.hitSurfaceSFX,Cue.Impact);
        }
        Clips(StartOfRound.Instance.playerJumpSFX,Cue.Jump);
        Clips(StartOfRound.Instance.playerHitGroundSoft,Cue.Landing);
        Clips(StartOfRound.Instance.playerHitGroundHard,Cue.Landing);
        Clips(StartOfRound.Instance.playerDragFootSFX,Cue.DragFootsteps);
        if(StartOfRound.Instance.unlockablesList!=null)
            foreach(var suit in StartOfRound.Instance.unlockablesList.unlockables) Clips(suit.jumpAudio,Cue.Jump);
        footstepsRegistered=true;
    }
    internal static int Group(AudioSource source)
    {
        var teleporter=source.GetComponentInParent<ShipTeleporter>();
        if(teleporter!=null) return teleporter.GetInstanceID();
        var elevator=source.GetComponentInParent<MineshaftElevatorController>();
        if(elevator!=null) return elevator.GetInstanceID();
        for(var t=source.transform;t!=null;t=t.parent)
            if(t.name=="SpikeSlamBodyStickyPoint" || t.name=="SpikeSlamBodyStickyPoint(Clone)") return t.GetInstanceID();
        int id=source.GetInstanceID();
        return sources.TryGetValue(id,out var b) && b.Source == source && b.Owner != null && b.Merge
            ? b.Owner.GetInstanceID() : id;
    }
    internal static void Source(AudioSource? source, Cue cue, Component owner, bool merge)
    {
        if (source == null) return;
        int id = source.GetInstanceID();
        bool added = !sources.TryGetValue(id, out var old) || old.Source != source;
        if (added && sources.Count >= 2048) { Prune(); if (sources.Count >= 2048) return; }
        if (added) { sources[id] = new Binding { Source=source, Owner=owner, Default=cue, Merge=merge }; recovery.Add(source); pending.Add(source); }
        else { old!.Default=cue; old.Merge=merge; old.Owner=owner; }
        if (source.loop) Clips(source.clip, cue);
        // Priming is deferred until all of this component's clip mappings exist.
    }
    internal static void Prime()
    {
        foreach(var source in pending) if(source!=null && source.isPlaying && source.clip!=null) AudioCapture.RecordPlay(source);
        pending.Clear();
    }
    internal static void Clips(AudioClip[]? values, Cue cue)
    {
        if (values != null) foreach (var clip in values) Clips(clip, cue);
    }
    internal static void Clips(AudioClip? clip, Cue cue)
    {
        if (clip == null) return;
        int id=clip.GetInstanceID();
        bool ambiguous=false;
        if (clips.TryGetValue(id,out var prior) && prior.Clip == clip)
        {
            if (prior.Ambiguous || prior.Cue == cue || cue == Cue.Creature) return;
            // Unknown metadata must not overwrite a useful acoustic category.
            if (prior.Cue != Cue.Creature) { cue=Cue.Creature; ambiguous=true; }
        }
        else if (clips.Count >= 4096) return;
        clips[id]=(clip,cue,ambiguous);
    }
    internal static bool Resolve(AudioSource source, AudioClip clip, out Cue cue, out int group)
    {
        group=source.GetInstanceID();
        bool bound=sources.TryGetValue(group,out var binding) && binding != null && binding.Source == source && binding.Owner != null;
        if (clips.TryGetValue(clip.GetInstanceID(),out var match) && match.Clip == clip) cue=match.Ambiguous && bound ? binding!.Default : match.Cue;
        else if (bound) cue=binding!.Default;
        else { cue=Cue.Creature; return false; }
        // Non-spatial feedback never combines with an owner's world sounds.
        if (bound && binding!.Merge && source.spatialBlend > 0.1f) group=binding!.Owner!.GetInstanceID();
        return true;
    }
    internal static void Prune()
    {
        retired.Clear();
        foreach (var pair in sources) if (pair.Value.Source == null || pair.Value.Owner == null) retired.Add(pair.Key);
        foreach (int id in retired) sources.Remove(id);
        retired.Clear();
        foreach (var pair in clips) if (pair.Value.Clip == null) retired.Add(pair.Key);
        foreach (int id in retired) clips.Remove(id);
        retired.Clear();
        for(int i=recovery.Count-1;i>=0;i--) if(recovery[i]==null) recovery.RemoveAt(i);
    }
    internal static void RecoverLoops(AudioCapture capture)
    {
        // Bounded rotating sweep of registered sources, not a scene search.
        // Recovers PlayOnAwake and loops evicted by a busy encounter.
        for(int i=0, budget=ScanBudget.For(recovery.Count,8);i<budget;i++)
        {
            if(recoveryCursor>=recovery.Count) recoveryCursor=0;
            var source=recovery[recoveryCursor++];
            if(source!=null && Owner(source) is LockPicker picker && (!picker.isOnDoor || !picker.isPickingLock)) continue;
            if(source!=null && (source.loop || Owner(source) is ItemDropship) && source.isActiveAndEnabled && source.isPlaying && !source.mute && source.volume>0 && !capture.HasLoop(source))
                AudioCapture.RecordPlay(source);
        }
    }
    internal static void Clear() { sources.Clear(); clips.Clear(); surfaceSteps.Clear(); retired.Clear(); recovery.Clear(); pending.Clear(); recoveryCursor=0; footstepsRegistered=false; }
}
