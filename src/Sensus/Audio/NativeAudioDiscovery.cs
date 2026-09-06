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
        if(clip==null) return;
        try
        {
            pointSources.RemoveWhere(s=>s==null);
            // Rare managed PlayClipAtPoint call only: its helper returns no source.
            // Read back the actual playing helper, never synthesize an audible event.
            foreach(var source in Object.FindObjectsOfType<AudioSource>())
                if(source.name=="One shot audio" && source.clip==clip && source.isPlaying && (source.transform.position-position).sqrMagnitude<0.0001f && pointSources.Add(source))
                    AudioCapture.RecordPlay(source);
        }
        catch(System.Exception e) { Debug.LogWarning("Sensus point sound observation: "+e.Message); }
    }
    internal static void Clear()
    { if(attached) SceneManager.sceneLoaded-=Loaded; attached=false; pending.Clear(); roots.Clear(); sources.Clear(); pointSources.Clear(); }
}
