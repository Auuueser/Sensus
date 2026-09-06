using System;

namespace Sensus.Captions;

internal static class IndicatorDetail
{
    internal static string Label(Cue cue, bool chinese, bool rear, bool unlocated) => CueText.Name(cue,chinese) +
        (unlocated ? (chinese ? "\n无方向" : "\nUnlocated") : rear ? (chinese ? "\n后方" : "\nBehind") : "");
    internal static float Follow(float angle, float target, float seconds, bool reducedMotion)
    {
        if(reducedMotion) return SoundField.Relative(target,0);
        float delta=SoundField.Relative(target,angle);
        return SoundField.Relative(angle+delta*(1-(float)Math.Exp(-Math.Max(0,Math.Min(0.25f,seconds))/0.07f)),0);
    }
    internal static bool Carried(Cue cue, bool localItem, bool radioReplay) =>
        localItem && !radioReplay && CuePresentation.Indicator(cue) &&
        CreatureSoundCatalog.Basis(cue) is not (Cue.ItemDrop or Cue.Footsteps or Cue.Running or Cue.DragFootsteps or Cue.Jump or Cue.Landing or Cue.LadderClimb or Cue.BodyImpact or Cue.WaterSplash or Cue.MudSink or Cue.DeathSound);
    // Segment intersection with the expanded lower-right HUD obstacle.
    internal static bool CrossesCorner(float ax,float ay,float bx,float by,float left,float top)
    {
        float enter=0,leave=1;
        float dx=bx-ax,dy=by-ay;
        if(Math.Abs(dx)<0.0001f) { if(ax<=left) return false; }
        else if(dx>0) enter=Math.Max(enter,(left-ax)/dx);
        else leave=Math.Min(leave,(left-ax)/dx);
        if(Math.Abs(dy)<0.0001f) { if(ay>=top) return false; }
        else if(dy<0) enter=Math.Max(enter,(top-ay)/dy);
        else leave=Math.Min(leave,(top-ay)/dy);
        return enter<leave-0.0001f && leave>0 && enter<1;
    }
    internal static float OrbitX(float angle,float radius) => (float)Math.Sin(angle*Math.PI/180)*radius;
    internal static float OrbitY(float angle,float radius) => (float)Math.Cos(angle*Math.PI/180)*radius;
}
