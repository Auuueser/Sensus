using Sensus.Captions;
using UnityEngine;
namespace Sensus.Audio;

// Multiple simultaneous clips on one source share the same native curves.
internal static class AudioCurves
{
    private sealed class Entry
    {
        internal AnimationCurve? Spatial, Rolloff;
        internal float SpatialAt=-1, RolloffAt=-1;
    }
    private static readonly BoundedCache<AudioSource,Entry> entries=new(1024);
    internal static AnimationCurve Get(AudioSource source,AudioSourceCurveType type)
    {
        if(!entries.TryGetValue(source,out var entry))
        {
            if(!entries.TryTakeOldest(out entry)) entry=new Entry();
            entry.Spatial=entry.Rolloff=null; entry.SpatialAt=entry.RolloffAt=-1;
            entries.Set(source,entry);
        }
        float now=Time.unscaledTime;
        if(type==AudioSourceCurveType.SpatialBlend)
        {
            if(entry.Spatial==null || now>=entry.SpatialAt)
            { entry.Spatial=source.GetCustomCurve(type); entry.SpatialAt=now+0.25f; }
            return entry.Spatial;
        }
        if(entry.Rolloff==null || now>=entry.RolloffAt)
        { entry.Rolloff=source.GetCustomCurve(type); entry.RolloffAt=now+0.25f; }
        return entry.Rolloff;
    }
    internal static void Clear() => entries.Clear();
}
