namespace Sensus.Captions;

// Observed audible onsets only. No AI sprint flags, transforms or velocity.
internal struct FootstepCadence
{
    private float last;
    private int quickIntervals;
    private bool heard;
    internal Cue Observe(float now)
    {
        float gap=now-last;
        if(heard && gap<0.08f) return quickIntervals>=2 ? Cue.Running : Cue.Footsteps;
        quickIntervals=heard && gap<=0.38f ? System.Math.Min(2,quickIntervals+1) : 0;
        heard=true; last=now;
        return quickIntervals>=2 ? Cue.Running : Cue.Footsteps;
    }
}
