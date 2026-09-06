using System;
using System.Threading;
using System.Runtime.CompilerServices;
using BepInEx;
using BepInEx.Bootstrap;
using HarmonyLib;
using Sensus.Audio;
using UnityEngine;

namespace Sensus;

[BepInPlugin(Guid, "Sensus", PluginVersion)]
[BepInDependency(LanguagePolicy.ChinesePluginGuid, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency(LanguagePolicy.LegacyChinesePluginGuid, BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("ainavt.lc.lethalconfig", BepInDependency.DependencyFlags.SoftDependency)]
public sealed class Plugin : BaseUnityPlugin
{
    public const string Guid = "Auuueser.Sensus";
    public const string PluginVersion = "1.0.0";
    private SensusSettings settings = null!;
    private NativeCaptionPresenter presenter = null!;
    private Action<bool>? updateConfigLanguage;
    private bool chinese;
    private int settingsDirty = 1;
    private bool reportedPresentationFailure;
    private bool reportedCaptureFailure;
    private AudioCapture capture = null!;
    private readonly Harmony harmony = new(Guid);
    private bool firstFrame = true;
    private bool shuttingDown;
    private long measuredTicks, worstTicks;
    private int measuredFrames;
    private float reportPerformanceAt;

    private void Awake()
    {
        settings = new SensusSettings(Config);
        presenter = new NativeCaptionPresenter(Logger);
        capture = new AudioCapture(settings, Logger);
        try
        {
            if (!RuntimeBaseline.Matches()) Logger.LogWarning("Game assembly differs from the audited V81 baseline; sound capture stays disabled. Update the audit before enabling new game versions.");
            else { PlaybackHooks.Install(harmony, Logger); capture.Ready = true; }
        }
        catch (Exception e)
        {
            harmony.UnpatchSelf();
            Logger.LogError($"Playback observers were not installed; capture stays disabled: {e}");
        }
        chinese = LanguagePolicy.UseChinese(settings.Language.Value, Chainloader.PluginInfos.Keys);
        Config.SettingChanged += OnSettingChanged;
        if (Chainloader.PluginInfos.ContainsKey("ainavt.lc.lethalconfig"))
        {
            try { updateConfigLanguage = LethalConfigIntegration.Register(settings, chinese); }
            catch (Exception e) { Logger.LogWarning($"LethalConfig registration failed; CFG remains available: {e}"); }
        }
        Logger.LogInfo($"Sensus {PluginVersion} loaded; language={(chinese ? "Chinese" : "English")}; sound capture ready={capture.Ready}. V81 creature audio adapters; per-event live validation pending.");
        Application.quitting += Shutdown;
        RuntimeFramePump.Install(harmony, this, Logger);
    }

    private void OnSettingChanged(object sender, BepInEx.Configuration.SettingChangedEventArgs e)
    {
        // Config callbacks may come from outside Unity's main thread.
        Interlocked.Exchange(ref settingsDirty, 1);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal void TickFrame()
    {
        if (firstFrame)
        {
            firstFrame = false;
            Logger.LogInfo($"Sensus first frame entered: settingsReady={settings != null}, presenterReady={presenter != null}.");
        }
        if (settings == null || presenter == null) return;
        if (Interlocked.Exchange(ref settingsDirty, 0) != 0)
        {
            reportedPresentationFailure = false;
            reportedCaptureFailure = false;
            chinese = LanguagePolicy.UseChinese(settings.Language.Value, Chainloader.PluginInfos.Keys);
            try { updateConfigLanguage?.Invoke(chinese); }
            catch (Exception e) { Logger.LogWarning($"Could not refresh config labels: {e.Message}"); }
            Logger.LogInfo($"Settings applied: enabled={settings.Enabled.Value}, preview={settings.Preview.Value}, language={(chinese ? "Chinese" : "English")}, scale={settings.Scale.Value}, position={settings.VerticalPosition.Value}.");
        }
        long measuredStart = settings.Diagnostics.Value ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        if (!reportedCaptureFailure)
        {
            try { capture.Tick(chinese); }
            catch (Exception e)
            {
                reportedCaptureFailure = true;
                Logger.LogError($"Sound capture stopped until a setting changes; UI preview remains available: {e}");
            }
        }
        if (reportedPresentationFailure) return;
        try { presenter.Tick(settings, chinese, reportedCaptureFailure ? "" : capture.Text, capture.Ready && !reportedCaptureFailure, capture.Field, reportedCaptureFailure ? "" : capture.SpokenText); }
        catch (Exception e)
        {
            presenter.Clear();
            if (!reportedPresentationFailure)
            {
                reportedPresentationFailure = true;
                Logger.LogError($"Caption presentation stopped until a setting changes: {e}");
            }
        }
        if(measuredStart != 0)
        {
            long elapsed=System.Diagnostics.Stopwatch.GetTimestamp()-measuredStart;
            measuredTicks+=elapsed; worstTicks=Math.Max(worstTicks,elapsed); measuredFrames++;
            if(Time.unscaledTime>=reportPerformanceAt)
            {
                reportPerformanceAt=Time.unscaledTime+10;
                double ms=1000d/System.Diagnostics.Stopwatch.Frequency;
                Logger.LogInfo($"Sensus frame work: frames={measuredFrames}, mean={measuredTicks*ms/measuredFrames:F3}ms, max={worstTicks*ms:F3}ms, registeredSources={AudioRegistry.SourceCount}; excludes native render/audio and playback callbacks.");
                measuredTicks=worstTicks=0; measuredFrames=0;
            }
        }
    }

    private void OnDestroy()
    {
        // Some modded startup flows destroy the BepInEx host before the first
        // menu frame. This is not application shutdown; native UI callbacks
        // retain only our managed state and continue driving the runtime.
        if (!shuttingDown)
            Logger.LogWarning("Sensus plugin component destroyed before application quit; preserving sound observers, settings and native UI frame pump.");
    }

    private void Shutdown()
    {
        if (shuttingDown) return;
        shuttingDown = true;
        Application.quitting -= Shutdown;
        RuntimeFramePump.Detach(this);
        Config.SettingChanged -= OnSettingChanged;
        presenter?.Clear();
        capture?.Dispose();
        harmony.UnpatchSelf();
        Logger.LogInfo("Sensus runtime shut down on application quit.");
    }
}
