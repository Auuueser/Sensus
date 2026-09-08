using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Sensus.Captions;
using UnityEngine;

namespace Sensus.Audio;

internal static class FeedbackFifth
{
    [ThreadStatic] internal static bool LocalClearing;
    internal static bool RequiresExplicitPlay(AudioSource source)
    {
        var owner=AudioRegistry.Owner(source);
        // Stop leaves music assigned; a later one-shot must not revive it.
        var boombox=owner as BoomboxItem ?? source.GetComponentInParent<BoomboxItem>();
        if(boombox!=null && source==boombox.boomboxAudio) return true;
        var vehicle=owner as VehicleController ?? source.GetComponentInParent<VehicleController>();
        if(vehicle!=null && (source==vehicle.miscAudio || source==vehicle.turbulenceAudio)) return true;
        return owner is CadaverBloomAI bloom && source==bloom.burstSource ||
            owner is BushWolfEnemy wolf && source==wolf.tongueAudio;
    }
    internal static bool Resolve(AudioSource source, AudioClip clip, out Cue cue)
    {
        var owner=AudioRegistry.Owner(source);
        cue=Cue.Creature;
        if(ClipNames.Get(clip) is "PoolFloatyHit1" or "PoolFloatyHit2") { cue=Cue.PoolFloaty; return true; }
        if(ClipNames.Get(clip)=="Cruiser_Turbulence" && owner is VehicleController) { cue=Cue.VehicleRattle; return true; }
        if(owner is BlobAI blob && clip==blob.hitSlimeSFX) { cue=Cue.SlimeHit; return true; }
        if(owner is SpringManAI spring && clip==spring.enterCooldownSFX) { cue=Cue.SpringRetract; return true; }
        if(owner is NutcrackerEnemyAI nut)
        {
            if(clip==nut.kickSFX) { cue=Cue.NutcrackerKick; return true; }
            if(clip==nut.dieSFX) { cue=Cue.NutcrackerFall; return true; }
        }
        if(owner is VehicleController && ClipNames.Get(clip)=="Collision_Minimal") { cue=Cue.VehicleImpact; return true; }
        if(owner is CadaverGrowthAI growth && source==growth.destroyAudio)
        { cue=Cue.PlantClear; return true; }
        if(owner is MoldSpreadManager mold && source==mold.destroyAudio)
        { cue=Cue.PlantClear; return true; }
        var wolf=owner as BushWolfEnemy;
        if(wolf!=null)
        {
            if(clip==wolf.dieSFX) cue=Cue.WolfDeath;
            else if(clip==wolf.hitBushWolfSFX) cue=Cue.WolfHit;
            else if(clip==wolf.killSFX) cue=Cue.WolfAttack;
            else if(clip==wolf.snarlSFX) cue=Cue.WolfSnarl;
            else if(clip==wolf.shootTongueSFX || clip==wolf.tongueShootSFX) cue=Cue.WolfTongue;
            else if(source==wolf.tongueAudio) cue=Cue.WolfPull;
            else if(source==wolf.callClose || source==wolf.callFar) cue=Cue.WolfCall;
            else if(source==wolf.growlAudio) cue=Cue.WolfGrowl;
            if(cue!=Cue.Creature) return true;
        }
        var detail=AcousticDetails.Resolve(ClipNames.Get(clip));
        // Native death/hit routing otherwise overrides these verified distinct recordings.
        if(detail is Cue.WolfCall or Cue.WolfGrowl or Cue.WolfSnarl or Cue.WolfTongue or Cue.WolfPull or Cue.WolfAttack or Cue.WolfHit or Cue.WolfDeath)
        { cue=detail; return true; }
        bool hatch=ClipNames.Get(clip) is "MetalHatchOpen" or "MetalHatchClose";
        if(hatch || detail is Cue.CabinetDoor or Cue.Cabinet)
            for(var t=source.transform;t!=null;t=t.parent)
            {
                if(hatch && (t.name=="StorageShelfContainer" || t.name=="StorageShelfContainer(Clone)"))
                { cue=Cue.CabinetDoor; return true; }
                // Its trigger names say Drawer; the grid cupboard also uses that name.
                // This tall four-door cabinet uses misleading Drawer transform names.
                if(!hatch && (t.name=="FancyDresserContainer" || t.name=="FancyDresserContainer(Clone)"))
                { cue=Cue.CabinetDoor; return true; }
            }
        return false;
    }
    internal static int Group(AudioSource source,Cue cue)
    {
        if(cue is Cue.CounterAttack or Cue.CounterWarning) return CompanyAudio.Group(source,cue);
        if(cue==Cue.WolfCall && AudioRegistry.Owner(source) is BushWolfEnemy wolf) return wolf.GetInstanceID();
        if(cue is Cue.MeteorApproach or Cue.MeteorImpact)
            for(var t=source.transform;t!=null;t=t.parent)
                if(t.name is "MeteorObject" or "MeteorObject(Clone)" or "MeteorLandingEffect" or "MeteorLandingEffect(Clone)") return t.GetInstanceID();
        return AudioRegistry.Group(source);
    }
    // Wrap only the two actual spray -> destruction calls, rather than polling
    // infection/plant state or adding a prefix and finalizer to every LateUpdate.
    internal static IEnumerable<CodeInstruction> ClearingTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        foreach(var instruction in instructions)
        {
            if(instruction.operand is MethodInfo method &&
                ((method.DeclaringType==typeof(CadaverGrowthAI) && method.Name==nameof(CadaverGrowthAI.DestroyPlantAtPosition)) ||
                 (method.DeclaringType==typeof(MoldSpreadManager) && method.Name==nameof(MoldSpreadManager.DestroyMoldAtPosition))))
            {
                // Reuse the original instruction for ldarg, retaining branch targets.
                instruction.opcode=OpCodes.Ldarg_0;
                instruction.operand=null;
                yield return instruction;
                yield return new CodeInstruction(OpCodes.Call,AccessTools.Method(typeof(FeedbackFifth),method.DeclaringType==typeof(CadaverGrowthAI) ? nameof(DestroyPlant) : nameof(DestroyMold)));
                continue;
            }
            yield return instruction;
        }
    }
    public static void DestroyPlant(CadaverGrowthAI growth,Vector3 position,bool playEffect,SprayPaintItem sprayer)
    {
        bool previous=LocalClearing;
        try
        {
            var player=GameNetworkManager.Instance?.localPlayerController;
            LocalClearing=player!=null && sprayer.playerHeldBy==player;
            var capture=AudioCapture.Current;
            bool trace=capture?.BeginPlantTrace(growth,sprayer)==true;
            growth.DestroyPlantAtPosition(position,playEffect);
            if(trace) capture?.WritePlantTrace("after",growth,sprayer);
        }
        finally { LocalClearing=previous; }
    }
    public static void DestroyMold(MoldSpreadManager mold,Vector3 position,bool playEffect,SprayPaintItem sprayer)
    {
        bool previous=LocalClearing;
        try
        {
            var player=GameNetworkManager.Instance?.localPlayerController;
            LocalClearing=player!=null && sprayer.playerHeldBy==player;
            mold.DestroyMoldAtPosition(position,playEffect);
        }
        finally { LocalClearing=previous; }
    }
}
