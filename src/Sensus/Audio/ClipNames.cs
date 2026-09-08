using Sensus.Captions;
using UnityEngine;
namespace Sensus.Audio;

// Unity Object.name crosses into native code and creates a managed string.
// AudioClip assets keep their identity across the many classification passes.
internal static class ClipNames
{
    private static readonly BoundedCache<AudioClip,string> names=new(4096);
    internal static string Get(AudioClip clip)
    {
        if(names.TryGetValue(clip,out var name)) return name;
        name=clip.name; names.Set(clip,name); return name;
    }
    internal static void Clear() => names.Clear();
}
