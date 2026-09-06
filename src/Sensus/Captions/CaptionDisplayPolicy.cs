namespace Sensus.Captions;

internal enum CaptionDisplayMode { Disabled, WaitingForPlayer, Dead, Menu, HiddenHud, Idle, Preview, Startup, Live }

internal static class CaptionDisplayPolicy
{
    internal static CaptionDisplayMode Select(bool enabled, bool playerReady, bool dead, bool menuOpen,
        bool hudHidden, bool preview, bool hasCaptions, bool startup)
    {
        if (!enabled) return CaptionDisplayMode.Disabled;
        if (!playerReady) return CaptionDisplayMode.WaitingForPlayer;
        if (dead) return CaptionDisplayMode.Dead;
        // Explicit preview must not depend on unrelated tutorial/intro animation state.
        if (menuOpen) return CaptionDisplayMode.Menu;
        // A layout preview must never mask an actual audible event.
        if (hasCaptions && !hudHidden) return CaptionDisplayMode.Live;
        if (preview) return CaptionDisplayMode.Preview;
        if (hudHidden) return CaptionDisplayMode.HiddenHud;
        return startup ? CaptionDisplayMode.Startup : CaptionDisplayMode.Idle;
    }
}
