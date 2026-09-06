using System;
using System.Collections.Generic;

namespace Sensus.Captions;

// Values here come only from audible samples, never from silent enemy tracking.
internal sealed class SoundField
{
    internal sealed class Signal
    {
        internal int Id, Source;
        internal Cue Cue;
        internal float Bearing, LastHeard, Started, LastRelative;
        internal bool DirectionKnown, Continuous;
        internal bool CarriedItem;
        internal int Strength;
        internal float Gain;
        internal float MusicEnvelope;
        internal float WindingSeconds=-1;
        internal float AdmittedAt, LastShown = -1000;
    }
    private readonly List<Signal> signals = new();
    private readonly List<Signal> selected = new();
    private readonly List<Signal> unlocated = new(3);
    internal IReadOnlyList<Signal> Visible => selected;
    internal IReadOnlyList<Signal> Unlocated => unlocated;
    internal bool HasSignals => signals.Count > 0;
    internal int OverflowDirections { get; private set; }
    private int sequence;
    private float replaceAt;
    private float unlocatedReplaceAt;
    private int[]? layoutSectors;
    internal bool CanDisplaySector(int sector) => layoutSectors==null || (sector>=0 && sector<8 && layoutSectors[sector]>0);
    internal int TrayCapacity { get; private set; } = 3;
    internal void Observe(int source, Cue cue, float now, float bearing, bool known, bool continuous, float gain, bool localMovement = false, float musicEnvelope = 0, float windingSeconds = -1, bool carriedItem = false)
    {
        // Heartbeats are explanatory captions, never spatial or fixed icons.
        if(!CuePresentation.Indicator(cue,localMovement)) return;
        if(carriedItem) known=false;
        if(CreatureSoundCatalog.Basis(cue) is Cue.Footsteps or Cue.Running)
            foreach(var prior in signals) if(prior.Source==source && prior.Cue==Cue.DragFootsteps && now-prior.LastHeard<0.85f) return;
        if(CreatureSoundCatalog.Basis(cue) is Cue.Footsteps or Cue.Running)
            for(int i=signals.Count-1;i>=0;i--) if(signals[i].Source==source && CreatureSoundCatalog.Basis(signals[i].Cue)!=CreatureSoundCatalog.Basis(cue) && CreatureSoundCatalog.Basis(signals[i].Cue) is Cue.Footsteps or Cue.Running)
            { selected.Remove(signals[i]); unlocated.Remove(signals[i]); signals.RemoveAt(i); }
        if(cue==Cue.DragFootsteps)
            for(int i=signals.Count-1;i>=0;i--) if(signals[i].Source==source && CreatureSoundCatalog.Basis(signals[i].Cue) is Cue.Footsteps or Cue.Running)
            { selected.Remove(signals[i]); unlocated.Remove(signals[i]); signals.RemoveAt(i); }
        Signal? signal = null;
        foreach(var existing in signals) if(existing.Source == source && CreatureSoundCatalog.Basis(existing.Cue) == CreatureSoundCatalog.Basis(cue)) { signal=existing; break; }
        if (signal == null)
        {
            if (signals.Count >= 64)
            {
                Signal victim = signals[0];
                foreach (var s in signals)
                    if (CueText.Priority(s.Cue) < CueText.Priority(victim.Cue) ||
                        (CueText.Priority(s.Cue) == CueText.Priority(victim.Cue) && s.LastHeard < victim.LastHeard)) victim = s;
                if (CueText.Priority(victim.Cue) > CueText.Priority(cue)) return;
                signals.Remove(victim); selected.Remove(victim);
            }
            signal = new Signal { Id = ++sequence, Source = source, Cue = cue, Started = now };
            signals.Add(signal);
        }
        signal.Cue=EventIdentity.Display(signal.Cue,cue,now-signal.LastHeard);
        if (now - signal.LastHeard > 0.3f) signal.Started = now;
        // Several near/far sources can represent one event. Keep the strongest
        // audible sample in this capture tick, independent of iteration order.
        if(signal.LastHeard == now && signal.CarriedItem==carriedItem && ((signal.DirectionKnown && !known) ||
            (signal.DirectionKnown==known && signal.Gain > gain))) return;
        signal.Gain=gain;
        signal.CarriedItem=carriedItem;
        signal.MusicEnvelope=musicEnvelope; signal.WindingSeconds=windingSeconds;
        signal.LastHeard = now; signal.Bearing = bearing; signal.DirectionKnown = known;
        signal.Continuous = continuous;
        signal.Strength = gain >= 0.3f ? 3 : gain >= 0.08f ? 2 : 1;
    }
    internal static bool IsLive(Signal s, float now) => now - s.LastHeard <= 0.12f;
    internal static float Alpha(Signal s, float now)
    {
        float hold = s.Continuous ? 0.12f : 0.65f;
        return Math.Max(0, Math.Min(1, 1 - (now - s.LastHeard - hold) / 0.35f));
    }
    internal static float Relative(float bearing, float yaw)
    {
        // C# remainder retains the dividend sign; one added revolution does
        // not normalize accumulated negative angles after repeated turns.
        float angle=(bearing-yaw)%360f;
        if(angle>=180f) angle-=360f;
        if(angle< -180f) angle+=360f;
        return angle;
    }
    internal static float SectorAngle(float angle, float previous)
    {
        // Five-degree hysteresis beyond a sector edge.
        if (Math.Abs(Relative(angle, previous)) < 27.5f) return previous;
        return CueText.DirectionIndex(angle) * 45f;
    }
    internal void Update(float now, int budget, float yaw, int unlocatedBudget = 3, int[]? sectorBudgets = null)
    {
        layoutSectors=sectorBudgets;
        TrayCapacity=Math.Max(0,unlocatedBudget);
        for(int i=signals.Count-1;i>=0;i--) if(Alpha(signals[i],now)<=0) signals.RemoveAt(i);
        foreach (var s in signals) if (IsLive(s, now)) s.LastRelative = Relative(s.Bearing, yaw);
        for(int i=selected.Count-1;i>=0;i--) if(!signals.Contains(selected[i]) || !selected[i].DirectionKnown) selected.RemoveAt(i);
        budget = Math.Max(0, Math.Min(DisplayCapacity.SignalSafety, budget));
        unlocatedBudget = Math.Max(0, Math.Min(DisplayCapacity.SignalSafety, unlocatedBudget));
        while (selected.Count > budget) selected.RemoveAt(selected.Count - 1);
        if(sectorBudgets!=null)
            for(int i=selected.Count-1;i>=0;i--)
                if(!Room(selected[i],selected,selected[i],sectorBudgets)) selected.RemoveAt(i);
        // Stable admission; sampling an existing sound does not move its slot.
        foreach (var s in signals)
        {
            // Unlocated sounds have a separate fixed tray, never a direction slot.
            if(!s.DirectionKnown || budget==0) continue;
            if (selected.Contains(s)) continue;
            // Distinct audible ambience uses the same bounded slots as other events.
            // Only actual shared events are merged during Observe (flies upstream).
            // A louder fan must not suppress a clock/music while slots remain free.
            if (selected.Count < budget && Room(s,selected,null,sectorBudgets)) { selected.Add(s); s.AdmittedAt=now; s.LastShown=now; continue; }
            int victim = -1;
            for (int i = 0; i < selected.Count; i++)
                if (Room(s,selected,selected[i],sectorBudgets) && (victim<0 || CueText.Priority(selected[i].Cue) < CueText.Priority(selected[victim].Cue) ||
                    (CueText.Priority(selected[i].Cue) == CueText.Priority(selected[victim].Cue) && selected[i].AdmittedAt < selected[victim].AdmittedAt))) victim = i;
            if(victim<0) continue;
            int rank = CueText.Priority(s.Cue), old = CueText.Priority(selected[victim].Cue);
            bool newCritical = rank == 3 && old == 3 && s.Started > selected[victim].Started;
            if ((!newCritical && rank <= old) || (rank < 3 && now < replaceAt)) continue;
            selected[victim].LastShown=now;
            selected[victim] = s; s.AdmittedAt=now; s.LastShown=now; replaceAt = now + 0.5f;
        }
        RotatePeer(selected, true, now, ref replaceAt,sectorBudgets);
        OverflowDirections = 0;
        foreach (var s in signals)
            if (!selected.Contains(s) && s.DirectionKnown && CueText.Priority(s.Cue) == 3)
                OverflowDirections |= 1 << CueText.DirectionIndex(s.LastRelative);
        for(int i=unlocated.Count-1;i>=0;i--)
            if(!signals.Contains(unlocated[i]) || unlocated[i].DirectionKnown) unlocated.RemoveAt(i);
        while(unlocated.Count>unlocatedBudget) unlocated.RemoveAt(unlocated.Count-1);
        foreach(var s in signals)
        {
            if(unlocatedBudget==0 || s.DirectionKnown || !CanUnlocate(s) || unlocated.Contains(s)) continue;
            int duplicate=-1;
            for(int i=0;i<unlocated.Count;i++) if(unlocated[i].Cue==s.Cue) { duplicate=i; break; }
            if(duplicate>=0)
            {
                if(s.LastHeard>unlocated[duplicate].LastHeard) { s.AdmittedAt=unlocated[duplicate].AdmittedAt; s.LastShown=unlocated[duplicate].LastShown; unlocated[duplicate]=s; }
                continue;
            }
            if(unlocated.Count<unlocatedBudget) { unlocated.Add(s); s.AdmittedAt=now; s.LastShown=now; continue; }
            int victim=0;
            for(int i=1;i<unlocated.Count;i++)
                if(CueText.Priority(unlocated[i].Cue)<CueText.Priority(unlocated[victim].Cue) || (CueText.Priority(unlocated[i].Cue)==CueText.Priority(unlocated[victim].Cue) && unlocated[i].AdmittedAt<unlocated[victim].AdmittedAt)) victim=i;
            int rank=CueText.Priority(s.Cue), old=CueText.Priority(unlocated[victim].Cue);
            if(rank>old) { unlocated[victim].LastShown=now; unlocated[victim]=s; s.AdmittedAt=now; s.LastShown=now; unlocatedReplaceAt=now+0.5f; }
        }
        RotatePeer(unlocated, false, now, ref unlocatedReplaceAt);
    }
    private static bool Room(Signal candidate, List<Signal> slots, Signal? removing, int[]? budgets)
    {
        if(budgets==null) return true;
        int sector=CueText.DirectionIndex(candidate.LastRelative), used=0;
        foreach(var s in slots) if(s!=removing && CueText.DirectionIndex(s.LastRelative)==sector) used++;
        return used<budgets[sector];
    }
    private void RotatePeer(List<Signal> slots, bool located, float now, ref float nextReplacement, int[]? sectorBudgets=null)
    {
        if(slots.Count==0 || now<nextReplacement) return;
        int victim=-1;
        Signal? next=null;
        // Choose the least recently displayed eligible peer, not the first entry.
        // One bounded scan; no sorting, allocations, or additional audio sampling.
        foreach(var s in signals)
        {
            if(s.DirectionKnown!=located || slots.Contains(s) || !IsLive(s,now) ||
                CueText.Priority(s.Cue)>=3) continue;
            if(!located)
            {
                if(!CanUnlocate(s)) continue;
                bool duplicate=false;
                foreach(var visible in slots) if(visible.Cue==s.Cue) { duplicate=true; break; }
                if(duplicate) continue;
            }
            for(int i=0;i<slots.Count;i++)
            {
                var prior=slots[i];
                if(CueText.Priority(prior.Cue)!=CueText.Priority(s.Cue) || now-prior.AdmittedAt<2f ||
                    s.LastShown>prior.AdmittedAt || !Room(s,slots,prior,located ? sectorBudgets : null)) continue;
                if(next==null || s.LastShown<next.LastShown || (s==next && prior.AdmittedAt<slots[victim].AdmittedAt))
                { next=s; victim=i; }
            }
        }
        if(next==null) return;
        slots[victim].LastShown=now;
        slots[victim]=next; next.AdmittedAt=now; next.LastShown=now; nextReplacement=now+0.5f;
    }
    internal void Clear() { signals.Clear(); selected.Clear(); unlocated.Clear(); OverflowDirections = 0; replaceAt = unlocatedReplaceAt = 0; layoutSectors=null; TrayCapacity=3; }
    internal static bool CanUnlocate(Signal s) => s.CarriedItem || CuePresentation.Unlocated(s.Cue);
}
