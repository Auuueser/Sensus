using System;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Sensus;

// Native UI lifetimes survive destruction of the startup BepInEx host.
// Deduplicate menu/HUD updates if both run in the same rendered frame.
internal static class RuntimeFramePump
{
    private static Plugin? owner;
    private static ManualLogSource? log;
    private static int lastFrame = -1;
    private static bool failed;

    internal static void Install(Harmony harmony, Plugin plugin, ManualLogSource logger)
    {
        owner = plugin;
        log = logger;
        lastFrame = -1;
        failed = false;
        var postfix = new HarmonyMethod(AccessTools.Method(typeof(RuntimeFramePump), nameof(Pump)));
        harmony.Patch(AccessTools.Method(typeof(MenuManager), "Update"), postfix: postfix);
        harmony.Patch(AccessTools.Method(typeof(HUDManager), "Update"), postfix: postfix);
        logger.LogInfo("Sensus frame pump installed on native menu and HUD updates.");
    }

    private static void Pump()
    {
        if (ReferenceEquals(owner, null) || log == null || failed || lastFrame == Time.frameCount) return;
        lastFrame = Time.frameCount;
        try { owner!.TickFrame(); }
        catch (Exception e)
        {
            failed = true;
            log.LogError($"Sensus frame entry failed: {e}");
        }
    }

    internal static void Detach(Plugin plugin)
    {
        if (!ReferenceEquals(owner, plugin)) return;
        owner = null;
        log = null;
    }
}
