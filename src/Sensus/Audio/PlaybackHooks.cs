using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Sensus.Audio;

public static class PlaybackHooks
{
    internal static readonly string[] Scope = {
        "HighAndLowAltitudeAudio", "AudioReverbTrigger", "GlobalEffects", "PhysicsKnockbackOnHit", "TimeOfDay", "IngamePlayerSettings",
        "ShipTeleporter", "MeteorShowers", "AnimatedObjectFloatSetter", "RadarBoosterItem", "TVScript", "MicrowaveItem", "CozyLights", "GiftBoxItem", "BeltBagItem", "EventWhenDroppedItem", "DeadBodyInfo", "HUDManager", "ShipBuildModeManager", "PlayAudioOnParticleDeath", "MoldSpreadManager", "MoveToExitSpecialAnimation", "RandomPeriodicAudioPlayer", "BaboonHawkAudioEvents", "LoopShapeKey", "UnlockableSuit",
        "MouthDogAI", "SandWormAI", "JesterAI", "NutcrackerEnemyAI", "ShotgunItem", "EnemyAI", "PlayAudioAnimationEvent", "AnimatedObjectTrigger",
        "CaveDwellerAI", "DressGirlAI", "ClaySurgeonAI", "SpringManAI", "FlowermanAI", "CrawlerAI", "CentipedeAI", "SandSpiderAI",
        "HoarderBugAI", "BlobAI", "PufferAI", "ButlerEnemyAI", "ButlerBeesEnemyAI", "MaskedPlayerEnemy", "StingrayAI",
        "CadaverGrowthAI", "CadaverBloomAI", "ForestGiantAI", "BaboonBirdAI", "RedLocustBees", "RadMechAI", "BushWolfEnemy",
        "PumaAI", "GiantKiwiAI", "FlowerSnakeEnemy", "DoublewingAI", "DocileLocustBeesAI", "DepositItemsDesk", "SnowmanSimpleAI",
        "KiwiBabyItem", "HauntedMaskItem", "SandSpiderWebTrap", "PlayerControllerB", "RoundManager", "GrabbableObject",
        "Turret", "Landmine", "SteamValveHazard", "SoundManager", "EntranceTeleport", "ItemDropship", "VehicleController", "NoisemakerProp", "BoomboxItem", "WhoopieCushionItem", "WalkieTalkie", "StormyWeather", "BridgeTrigger", "ShipAlarmCord", "JetpackItem", "StunGrenadeItem", "ExtensionLadderItem", "Shovel", "KnifeItem", "SprayPaintItem", "FlashlightItem", "DoorLock","LockPicker","EnemyVent","ItemCharger","MineshaftElevatorController","LungProp","RemoteProp","ClockProp","AnimatedItem","BreakerBox","Terminal","SpikeRoofTrap","TetraChemicalItem","PatcherTool","StartOfRound","StartMatchLever","RandomFlyParticle","ElevatorAnimationEvents" };
    private static readonly Dictionary<MethodInfo, MethodInfo> Replacements = BuildReplacements();
    private static Dictionary<MethodInfo, MethodInfo> BuildReplacements()
    {
        var pairs = new Dictionary<MethodInfo, MethodInfo>();
        void Add(string name, Type[] arguments, string replacement)
        {
            pairs.Add(AccessTools.Method(typeof(AudioSource), name, arguments), AccessTools.Method(typeof(PlaybackHooks), replacement));
        }
        Add("PlayClipAtPoint", new[] { typeof(AudioClip), typeof(Vector3), typeof(float) }, nameof(AtPoint));
        Add("PlayClipAtPoint", new[] { typeof(AudioClip), typeof(Vector3) }, nameof(AtPointDefault));
        Add("Play", Type.EmptyTypes, nameof(Play));
        Add("PlayOneShot", new[] { typeof(AudioClip) }, nameof(OneShot));
        Add("PlayOneShot", new[] { typeof(AudioClip), typeof(float) }, nameof(OneShotScaled));
        Add("Stop", Type.EmptyTypes, nameof(Stop));
        Add("Pause", Type.EmptyTypes, nameof(Pause));
        Add("UnPause", Type.EmptyTypes, nameof(UnPause));
        return pairs;
    }
    internal static int Install(Harmony harmony, ManualLogSource log)
    {
        var candidates = new List<MethodInfo>();
        var assembly = typeof(EnemyAI).Assembly;
        foreach (var type in assembly.GetTypes())
        {
            bool scoped = Scope.Contains(type.Name) || (type.DeclaringType != null && Scope.Contains(type.DeclaringType.Name));
            if (!scoped) continue;
            candidates.AddRange(type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsAbstract && !m.ContainsGenericParameters && m.GetMethodBody() != null));
        }
        harmony.Patch(AccessTools.Method(typeof(GrabbableObject), nameof(GrabbableObject.DiscardItem)), prefix: new HarmonyMethod(typeof(InteractionAudio), nameof(InteractionAudio.BeforeDiscard)));
        harmony.Patch(AccessTools.Method(typeof(GrabbableObject), nameof(GrabbableObject.DiscardItemFromEnemy)), prefix: new HarmonyMethod(typeof(InteractionAudio), nameof(InteractionAudio.EnemyDiscard)));
        harmony.Patch(AccessTools.Method(typeof(GameNetcodeStuff.PlayerControllerB), "SetObjectAsNoLongerHeld"), prefix: new HarmonyMethod(typeof(InteractionAudio), nameof(InteractionAudio.PlayerDrop)));
        harmony.Patch(AccessTools.Method(typeof(GameNetcodeStuff.PlayerControllerB), "PlaceGrabbableObject"), prefix: new HarmonyMethod(typeof(InteractionAudio), nameof(InteractionAudio.PlayerPlace)));
        harmony.Patch(AccessTools.Method(typeof(GrabbableObject), nameof(GrabbableObject.GrabItem)), prefix: new HarmonyMethod(typeof(InteractionAudio), nameof(InteractionAudio.PickedUp)));
        harmony.Patch(AccessTools.Method(typeof(GrabbableObject), nameof(GrabbableObject.GrabItemFromEnemy)), prefix: new HarmonyMethod(typeof(InteractionAudio), nameof(InteractionAudio.PickedUp)));
        harmony.Patch(AccessTools.Method(typeof(GameNetcodeStuff.PlayerControllerB), "DropHeldItem"), prefix: new HarmonyMethod(typeof(InteractionAudio), nameof(InteractionAudio.ForcedDrop)));
        harmony.Patch(AccessTools.Method(typeof(RoundManager), "FinishGeneratingLevel"), postfix: new HarmonyMethod(typeof(NativeAudioDiscovery), nameof(NativeAudioDiscovery.Refresh)));
        harmony.Patch(AccessTools.Method(typeof(TimeOfDay), nameof(TimeOfDay.WaterSplashEffect)),
            prefix: new HarmonyMethod(typeof(FeedbackFourth), nameof(FeedbackFourth.BeforeSplash)),
            finalizer: new HarmonyMethod(typeof(FeedbackFourth), nameof(FeedbackFourth.AfterSplash)));
        int installed = 0;
        foreach(string name in new[]{nameof(SprayPaintItem.LateUpdate),nameof(SprayPaintItem.KillCadaverPlantRpc),nameof(SprayPaintItem.KillWeedRpc)})
            harmony.Patch(AccessTools.Method(typeof(SprayPaintItem),name),
                transpiler:new HarmonyMethod(typeof(FeedbackFifth),nameof(FeedbackFifth.ClearingTranspiler)));
        foreach (var method in candidates)
        {
            if (!method.IsStatic && typeof(UnityEngine.Component).IsAssignableFrom(method.DeclaringType) &&
                ((method.GetParameters().Length==0 && method.Name is "Start" or "OnEnable" or "Awake") ||
                 (method.DeclaringType==typeof(ItemCharger) && method.Name is "ChargeItem" or "PlayChargeItemEffectClientRpc") ||
                 (method.DeclaringType==typeof(ElevatorAnimationEvents) && method.Name.StartsWith("PlayAudio"))))
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(PlaybackHooks), nameof(RegisterAudio)),
                    postfix: new HarmonyMethod(typeof(PlaybackHooks), nameof(RegisterAudio)));
            var il = method.GetMethodBody()?.GetILAsByteArray();
            if(il==null) continue;
            var calls=AudioCallTokens.Read(il).Select(token=>method.Module.ResolveMethod(token)).OfType<MethodInfo>().ToArray();
            if(calls.Any(m=>m.DeclaringType==typeof(UnityEngine.Object) && m.Name=="Instantiate" && m.ReturnType==typeof(GameObject)))
                harmony.Patch(method, transpiler: new HarmonyMethod(typeof(PlaybackHooks), nameof(SpawnTranspile)));
            if(!calls.Any(Replacements.ContainsKey)) continue;
            if(!method.IsStatic && typeof(Component).IsAssignableFrom(method.DeclaringType))
                harmony.Patch(method, prefix: new HarmonyMethod(typeof(PlaybackHooks), nameof(RegisterAtPlay)));
            harmony.Patch(method, transpiler: new HarmonyMethod(AccessTools.Method(typeof(PlaybackHooks), nameof(Transpile))));
            installed++;
        }
        if (installed == 0) throw new InvalidOperationException("No supported playback call sites found.");
        log.LogInfo($"Installed playback observers on {installed} managed methods, including coroutine MoveNext bodies. No native AudioSource method was patched.");
        return installed;
    }
    private static readonly HashSet<Component> registeredAtPlay = new();
    private static void RegisterAtPlay(Component __instance)
    { if(registeredAtPlay.Count<8192 && registeredAtPlay.Add(__instance)) RegisterAudio(__instance); }
    internal static void PruneRegistrations() => registeredAtPlay.RemoveWhere(c=>c==null);
    internal static void ClearRegistrations() => registeredAtPlay.Clear();
    private static bool registrationError;
    private static void RegisterAudio(UnityEngine.Component __instance)
    {
        try { FeedbackFourth.Register(__instance); CreatureBindings.Register(__instance); AnimationAudioBindings.Register(__instance); InteractionAudio.Register(__instance); SupplementalAudio.Register(__instance); AuditAudioBindings.Register(__instance); AudioRegistry.Prime(); }
        catch (Exception e)
        {
            if (!registrationError) { registrationError=true; UnityEngine.Debug.LogWarning("Sensus audio registration failed: " + e.Message); }
        }
    }
    private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        foreach (var instruction in instructions)
        {
            if ((instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) && instruction.operand is MethodInfo method && Replacements.TryGetValue(method, out var replacement))
            {
                // Mutate in place to preserve every label and exception-block boundary.
                if(__originalMethod.DeclaringType==typeof(GameNetcodeStuff.PlayerControllerB) &&
                    __originalMethod.Name=="SetFaceUnderwaterFilters" && replacement.Name==nameof(OneShot))
                    replacement=AccessTools.Method(typeof(PlaybackHooks),nameof(UnderwaterOneShot));
                if(__originalMethod.DeclaringType==typeof(MaskedPlayerEnemy) && __originalMethod.Name==nameof(MaskedPlayerEnemy.PlayFootstepSound) && replacement.Name==nameof(OneShotScaled))
                    replacement=AccessTools.Method(typeof(PlaybackHooks),nameof(MaskedFootstep));
                instruction.opcode = OpCodes.Call;
                instruction.operand = replacement;
            }
            yield return instruction;
        }
    }
    private static IEnumerable<CodeInstruction> SpawnTranspile(IEnumerable<CodeInstruction> instructions)
    {
        foreach(var instruction in instructions)
        {
            yield return instruction;
            if(instruction.opcode==OpCodes.Call && instruction.operand is MethodInfo m && m.DeclaringType==typeof(UnityEngine.Object) && m.Name=="Instantiate" && m.ReturnType==typeof(GameObject))
            {
                yield return new CodeInstruction(OpCodes.Dup);
                yield return new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(SupplementalAudio),nameof(SupplementalAudio.Spawned)));
            }
        }
    }
    public static void AtPoint(AudioClip clip, Vector3 position, float volume)
    {
        AudioSource.PlayClipAtPoint(clip,position,volume);
        // Observe the actual temporary source on the next bounded discovery pass.
        // No extra sound is played, no enemy location is inferred.
        NativeAudioDiscovery.RequestPoint(clip,position);
    }
    public static void AtPointDefault(AudioClip clip,Vector3 position)
    {
        AudioSource.PlayClipAtPoint(clip,position);
        NativeAudioDiscovery.RequestPoint(clip,position);
    }
    public static void Play(AudioSource source) { source.Play(); AudioCapture.RecordPlay(source, true); }
    public static void OneShot(AudioSource source, AudioClip clip) { source.PlayOneShot(clip); AudioCapture.Record(source, clip, 1f, true); }
    public static void MaskedFootstep(AudioSource source,AudioClip clip,float scale) { source.PlayOneShot(clip,scale); AudioCapture.Record(source,clip,scale,true,true); }
    public static void OneShotScaled(AudioSource source, AudioClip clip, float scale) { source.PlayOneShot(clip, scale); AudioCapture.Record(source, clip, scale, true); }
    public static void UnderwaterOneShot(AudioSource source, AudioClip clip)
    {
        source.PlayOneShot(clip);
        bool prior=FeedbackRound3.DrowningPlayback;
        try { FeedbackRound3.DrowningPlayback=true; AudioCapture.Record(source,clip,1f,true); }
        finally { FeedbackRound3.DrowningPlayback=prior; }
    }
    public static void Stop(AudioSource source) { source.Stop(); AudioCapture.Change(source, 0); }
    public static void Pause(AudioSource source) { source.Pause(); AudioCapture.Change(source, 1); }
    public static void UnPause(AudioSource source) { source.UnPause(); AudioCapture.Change(source, 2); }
}
