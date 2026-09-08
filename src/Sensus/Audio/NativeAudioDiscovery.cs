using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Sensus.Audio;

// Incremental one-pass scene discovery. No periodic full-scene AudioSource scan.
internal static class NativeAudioDiscovery
{
    private static readonly Queue<Transform> pending=new();
    private static readonly List<GameObject> roots=new();
    private static readonly List<AudioSource> sources=new();
    private static readonly HashSet<AudioSource> pointSources=new();
    private static readonly List<(AudioClip Clip,Vector3 Position)> pointRequests=new(64);
    private static bool attached;
    internal static void Refresh()
    {
        pending.Clear();
        for(int i=0;i<SceneManager.sceneCount;i++) Enqueue(SceneManager.GetSceneAt(i));
    }
    private static void Loaded(Scene scene,LoadSceneMode mode) => Enqueue(scene);
    private static void Enqueue(Scene scene)
    {
        if(!scene.isLoaded) return;
        roots.Clear(); scene.GetRootGameObjects(roots);
        foreach(var root in roots) pending.Enqueue(root.transform);
        roots.Clear();
    }
    internal static void Tick()
    {
        if(!attached) { attached=true; SceneManager.sceneLoaded+=Loaded; Refresh(); }
        FlushPoints();
        for(int i=0;i<128 && pending.Count>0;i++)
        {
            var node=pending.Dequeue(); if(node==null) continue;
            node.GetComponents(sources);
            foreach(var source in sources)
            {
                if(source.clip==null || AudioRegistry.Owner(source)!=null) continue;
                if(FeedbackFourth.Resolve(source,source.clip,out var cue) || AuditAudioBindings.NativeClip(source.clip,out cue) || SupplementalAudio.Clip(source.clip,out cue))
                {
                    AudioRegistry.Source(source,cue,node,false);
                    AudioRegistry.Clips(source.clip,cue);
                }
            }
            sources.Clear();
            for(int c=0;c<node.childCount;c++) pending.Enqueue(node.GetChild(c));
        }
        AudioRegistry.Prime();
    }
    internal static void RequestPoint(AudioClip clip,Vector3 position)
    {
        if(clip==null || !AudioCapture.AcceptingEvents || pointRequests.Count>=64) return;
        pointRequests.Add((clip,position));
    }
    internal static void CancelPointRequests() => pointRequests.Clear();
    private static void FlushPoints()
    {
        if(pointRequests.Count==0) return;
        try
        {
            pointSources.RemoveWhere(s=>s==null);
            // Native PlayClipAtPoint has no returned handle. Coalesce all requests
            // into one unsorted, demand-only lookup on the next capture tick.
            foreach(var source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
            {
                var clip=source.clip;
                if(clip==null || pointSources.Contains(source)) continue;
                foreach(var request in pointRequests)
                    if(clip==request.Clip && source.isPlaying && (source.transform.position-request.Position).sqrMagnitude<0.0001f && source.name=="One shot audio")
                    { pointSources.Add(source); AudioCapture.RecordPlay(source); break; }
            }
        }
        catch(System.Exception e) { Debug.LogWarning("Sensus point sound observation: "+e.Message); }
        finally { pointRequests.Clear(); }
    }
    internal static void Clear()
    { if(attached) SceneManager.sceneLoaded-=Loaded; attached=false; pending.Clear(); roots.Clear(); sources.Clear(); pointSources.Clear(); pointRequests.Clear(); }
}
