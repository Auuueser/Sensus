using System;
namespace Sensus.Captions;

internal static class StableOrbit
{
    // Allocate in sound/world angles: camera rotation must not change the layout.
    // Hold an admitted offset until a real sound change makes that slot unavailable.
    internal static float Offset(float bearing,float previous,float[] admitted,int count,float gap)
    {
        if(Free(bearing+previous,admitted,count,gap*0.85f)) return previous;
        float best=previous,score=float.MaxValue;
        for(int i=0;i<count;i++) for(int side=-1;side<=1;side+=2)
        {
            float candidate=admitted[i]+side*gap;
            float offset=SoundField.Relative(candidate,bearing);
            float travel=Math.Abs(SoundField.Relative(offset,previous));
            if(travel<score && Free(candidate,admitted,count,gap))
            { best=offset; score=travel; }
        }
        if(score<float.MaxValue) return best;
        // Finite-space overload: retain position rather than oscillating.
        return previous;
    }
    // Ease avoidance separately from real bearing tracking. Limit velocity and
    // damp reversals so changing neighbors cannot cause a rapid sideways sweep.
    internal static float Ease(float current,float target,ref float velocity,float seconds,bool reducedMotion)
    {
        if(reducedMotion) { velocity=0; return SoundField.Relative(target,0); }
        float dt=Math.Max(0,Math.Min(0.1f,seconds));
        float delta=SoundField.Relative(target,current);
        float desired=Math.Max(-90,Math.Min(90,delta/0.25f));
        velocity+=(desired-velocity)*(1-(float)Math.Exp(-dt/0.12f));
        float step=velocity*dt;
        if(step*delta>=0 && Math.Abs(step)>=Math.Abs(delta)) { step=delta; velocity=0; }
        return SoundField.Relative(current+step,0);
    }
    private static bool Free(float angle,float[] admitted,int count,float gap)
    {
        for(int i=0;i<count;i++)
            if(Math.Abs(SoundField.Relative(angle,admitted[i]))<gap-0.001f) return false;
        return true;
    }
}
