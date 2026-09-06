using BepInEx.Logging;
using Sensus.Captions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sensus;

internal sealed class NativeCaptionPresenter
{
    private readonly ManualLogSource log;
    private HUDManager? owner;
    private RectTransform? panel;
    private Canvas? canvas;
    private TextMeshProUGUI? header, caption;
    private Image? background;
    private float startupRemaining = 4f, nextDiagnostic;
    private bool firstLiveReported;
    private string lastLoggedState = "";
    private TMP_FontAsset? checkedFont;
    private string measuredText = "", measuredTitle = "";
    private float measuredWidth, measuredUnit, measuredBodyHeight, measuredTitleHeight, measuredContentWidth;
    private TMP_FontAsset? measuredFont;
    private readonly PeripheralPresenter peripheral;
    internal string Status { get; private set; } = "waiting-for-hud";
    private readonly Vector3[] slotCorners = new Vector3[4];
    private const string ChinesePreview = "左侧 · 上弦音乐\n右前方 · 枪声";
    private const string EnglishPreview = "Left · Winding music\nFront-right · Gunshot";

    internal NativeCaptionPresenter(ManualLogSource log) { this.log = log; peripheral = new PeripheralPresenter(log); }

    internal void Tick(SensusSettings settings, bool chinese, string liveCaptions, bool captureReady, SoundField field, string spoken="")
    {
        var hud = HUDManager.Instance;
        if (owner != hud)
        {
            Clear();
            owner = hud;
            startupRemaining = 4;
            firstLiveReported = false;
        }
        if (hud == null) { SetState("waiting-for-hud", settings); return; }
        var player = GameNetworkManager.Instance?.localPlayerController;
        bool menu = player != null && (player.inTerminalMenu || (player.quickMenuManager != null && player.quickMenuManager.isMenuOpen));
        var mode = CaptionDisplayPolicy.Select(settings.Enabled.Value, player != null && player.isPlayerControlled,
            player != null && player.isPlayerDead, menu, hud.hudHidden, settings.Preview.Value,
            !string.IsNullOrEmpty(spoken) || !string.IsNullOrEmpty(liveCaptions) || (settings.DisplayMode.Value != "Captions" && field.HasSignals),
            false);
        if (mode is not (CaptionDisplayMode.Preview or CaptionDisplayMode.Startup or CaptionDisplayMode.Live))
        {
            peripheral.Hide();
            if (panel != null) panel.gameObject.SetActive(false);
            if (mode == CaptionDisplayMode.Disabled) startupRemaining = 4;
            SetState(mode.ToString(), settings);
            return;
        }
        if (panel == null && !CreatePanel(hud)) { SetState("waiting-for-native-templates", settings); return; }
        if (canvas == null || !canvas.isActiveAndEnabled) { SetState("native-canvas-inactive", settings); return; }
        if (hud.tipsPanelBody == null || hud.tipsPanelHeader == null) { SetState("native-font-source-unavailable", settings); return; }

        bool preview = mode == CaptionDisplayMode.Preview;
        bool startup = mode == CaptionDisplayMode.Startup;
        bool hybrid = settings.DisplayMode.Value != "Captions";
        float unit = Mathf.Max(14, hud.tipsPanelBody.fontSize) * settings.Scale.Value * 0.85f;
        var canvasRect = (RectTransform)canvas.transform;
        Rect fullSafe = CaptionArea(hud,canvasRect,unit);
        // Keep the combined text/tray column in the lower half, leaving space for
        // right-hand direction markers even when all count settings are unlimited.
        fullSafe.yMax=Mathf.Min(fullSafe.yMax,fullSafe.yMin+canvasRect.rect.height*0.48f);
        if (hybrid && !startup) peripheral.Tick(canvas, hud.tipsPanelBody, field, settings, chinese, preview, fullSafe);
        else peripheral.Hide();
        peripheral.MeasureUnlocated(fullSafe,Mathf.Max(14,hud.tipsPanelBody.fontSize)*settings.MarkerScale.Value*0.85f);
        float stackGap=unit*0.5f;
        float emptyBaseline=Mathf.Clamp(settings.BottomAligned.Value ? fullSafe.yMin : canvasRect.rect.yMin+canvasRect.rect.height*settings.VerticalPosition.Value,fullSafe.yMin,fullSafe.yMax-peripheral.UnlocatedHeight);
        peripheral.ArrangeUnlocated(fullSafe,emptyBaseline,canvasRect.rect.center);
        if(settings.DisplayMode.Value == "Peripheral" && spoken.Length==0)
        {
            panel!.gameObject.SetActive(false);
            SetState("indicators-only",settings);
            return;
        }
        string text = preview ? (chinese ? ChinesePreview : EnglishPreview) : startup
            ? (captureReady ? (chinese ? "声音字幕已启用" : "Sound captions enabled") : (chinese ? "声音采集未就绪，请查看日志" : "Sound capture unavailable; check the log"))
            : spoken.Length==0 ? liveCaptions : spoken+(liveCaptions.Length==0 ? "" : "\n"+liveCaptions);
        if (preview && hybrid) text = chinese ? "转头查看方向 · 后方低吼每隔数秒消退" : "Turn to see directions · rear growl fades periodically";
        if(!preview && liveCaptions.Length>0 && Sensus.Audio.AudioCapture.Current is { OmittedCaptions: > 0 } overflowCapture)
            text += chinese ? $"\n另有 {overflowCapture.OmittedCaptions} 项 · 轮换显示" : $"\n{overflowCapture.OmittedCaptions} more · rotating";
        if(string.IsNullOrEmpty(text)) { panel!.gameObject.SetActive(false); return; }
        string title = preview ? (chinese ? "SENSUS / 界面演示 · 非真实事件" : "SENSUS / UI PREVIEW · NOT LIVE")
            : (chinese ? "SENSUS / 声音线索" : "SENSUS / SOUND CUES");
        if (preview && !captureReady) title += chinese ? " · 采集未就绪" : " · CAPTURE UNAVAILABLE";
        panel!.gameObject.SetActive(true);
        SyncStyle(header!, hud.tipsPanelHeader);
        SyncStyle(caption!, hud.tipsPanelBody);
        bool showHeading = preview || startup;
        header!.gameObject.SetActive(showHeading);
        header.text = title;
        caption!.text = text;
        // Use the native Canvas coordinate system and native font size. A fixed
        // pixel font size would be tiny or huge on different Canvas scalers.
        header.fontSize = unit * 0.65f;
        caption.fontSize = unit;
        // Native tip colors animate to black while the original tip is hidden.
        // Retain its font and geometry, but own our readable display colors.
        header.color = new Color(1f, 0.62f, 0.22f, 1);
        caption.color = new Color(0.96f, 0.96f, 0.92f, 1);
        background!.color = new Color(0.02f, 0.025f, 0.02f, settings.BackgroundOpacity.Value);
        background.gameObject.SetActive(settings.ShowBackground.Value && settings.BackgroundOpacity.Value > 0);
        Rect safe=fullSafe;
        if(peripheral.UnlocatedHeight>0) safe.yMax-=peripheral.UnlocatedHeight+stackGap;
        // The next capture tick composes only readable rows; no giant string or
        // tiny-font attempt to fit the configured number into a finite screen.
        if(Sensus.Audio.AudioCapture.Current is { } capture)
            capture.CaptionScreenBudget=Mathf.Max(1,DisplayCapacity.Rows(safe.height-unit*3-(spoken.Length>0 ? unit*4 : 0),unit*3));
        float width = Mathf.Min(unit * (showHeading ? 18 : 14), safe.width);
        if (width <= unit * 3) { panel.gameObject.SetActive(false); SetState("native-canvas-layout-pending", settings); return; }
        float padding = unit * (hybrid ? 0.4f : 0.7f);
        string layoutTitle = showHeading ? title : "";
        if(measuredText != text || measuredTitle != layoutTitle || measuredWidth != width-padding*2 || measuredUnit != unit || measuredFont != caption.font)
        {
            measuredText=text; measuredTitle=layoutTitle; measuredWidth=width-padding*2; measuredUnit=unit; measuredFont=caption.font;
            var titleSize=showHeading ? header.GetPreferredValues(title,measuredWidth,Mathf.Infinity) : Vector2.zero;
            var bodySize=caption.GetPreferredValues(text,measuredWidth,Mathf.Infinity);
            measuredTitleHeight=titleSize.y; measuredBodyHeight=bodySize.y;
            measuredContentWidth=Mathf.Max(titleSize.x,bodySize.x);
        }
        if(safe.height < padding*2+unit*2) { panel.gameObject.SetActive(false); SetState("native-canvas-layout-pending",settings); return; }
        float titleHeight=Mathf.Min(measuredTitleHeight,safe.height*0.3f);
        float headingGap=showHeading ? unit*0.3f : 0;
        // Keep width stable, but fit height to the actual wrapped text. MaxCaptions
        // limits events in the buffer; it must not reserve empty background rows.
        float bodyHeight=Mathf.Min(Mathf.Max(unit*1.2f,measuredBodyHeight),safe.height-padding*2-titleHeight-headingGap);
        caption.enableAutoSizing=measuredBodyHeight>bodyHeight;
        caption.fontSizeMin=unit*0.55f; caption.fontSizeMax=unit;
        // Keep the full safe column width even for a short caption.
        float height = padding * 2 + titleHeight + headingGap + bodyHeight;
        panel.anchorMin = panel.anchorMax = new Vector2(0, 0);
        panel.pivot = new Vector2(1, 0.5f);
        float y = Mathf.Clamp(settings.BottomAligned.Value ? safe.yMin+height*0.5f : canvasRect.rect.yMin+canvasRect.rect.height * settings.VerticalPosition.Value, safe.yMin+height*0.5f, safe.yMax-height*0.5f);
        panel.anchoredPosition = new Vector2(safe.xMax-canvasRect.rect.xMin, y-canvasRect.rect.yMin);
        panel.sizeDelta = new Vector2(width, height);
        peripheral.ArrangeUnlocated(fullSafe,y+height*0.5f+stackGap,canvasRect.rect.center);
        Place(header.rectTransform, padding, padding, width - 2 * padding, titleHeight);
        Place(caption.rectTransform, padding, padding + titleHeight + headingGap, width - 2 * padding, bodyHeight);
        // TMP rebuilds only when dirty; do not force both meshes every frame.
        if (chinese && caption.font != null && checkedFont != caption.font)
        {
            checkedFont = caption.font;
            if (!caption.font.HasCharacters(ChinesePreview + title, out uint[] _, true, true))
                log.LogWarning("Sensus native font is missing Chinese glyphs; check LC Chinese Project font support or select English.");
        }
        SetState($"visible-{mode}", settings);
        if (startup) startupRemaining = Mathf.Max(0, startupRemaining - Time.unscaledDeltaTime);
        if (mode == CaptionDisplayMode.Live && !firstLiveReported)
        {
            firstLiveReported = true;
            log.LogInfo("First live sound caption submitted to the Sensus panel. Visual confirmation is still required.");
        }
        if (settings.Diagnostics.Value && Time.unscaledTime >= nextDiagnostic)
        {
            nextDiagnostic = Time.unscaledTime + 10;
            log.LogInfo($"UI: state={Status}, active={panel.gameObject.activeInHierarchy}, canvas={canvas.isActiveAndEnabled}, alpha={caption.canvasRenderer.GetInheritedAlpha():F2}, glyphs={caption.textInfo.characterCount}, size={width:F0}x{height:F0}.");
        }
    }

    private bool CreatePanel(HUDManager hud)
    {
        if (hud.tipsPanelBody == null || hud.tipsPanelHeader == null || hud.HUDContainer == null) return false;
        canvas = hud.HUDContainer.GetComponentInParent<Canvas>()?.rootCanvas;
        var imageTemplate = hud.tipsPanelBody.GetComponentInParent<Image>();
        if (canvas == null || imageTemplate == null) return false;
        var root = new GameObject("SensusCaptionPanel", typeof(RectTransform), typeof(CanvasGroup));
        root.SetActive(false);
        // Same original Canvas; sibling of animated HUD containers and menus.
        root.transform.SetParent(canvas.transform, false);
        panel = root.GetComponent<RectTransform>();
        var group = root.GetComponent<CanvasGroup>();
        group.alpha = 1;
        group.interactable = false;
        group.blocksRaycasts = false;
        background = NativeUiTemplates.CloneImage(imageTemplate, panel, "Background");
        background.rectTransform.anchorMin = Vector2.zero;
        background.rectTransform.anchorMax = Vector2.one;
        background.rectTransform.offsetMin = background.rectTransform.offsetMax = Vector2.zero;
        header = NativeUiTemplates.CloneText(hud.tipsPanelHeader, panel, "Heading");
        caption = NativeUiTemplates.CloneText(hud.tipsPanelBody, panel, "CaptionRows");
        header.alignment = caption.alignment = TextAlignmentOptions.Center;
        header.overflowMode = caption.overflowMode = TextOverflowModes.Ellipsis;
        log.LogInfo($"Sensus UI created from native UGUI templates under Canvas '{canvas.name}'; independent panel, original controls retained.");
        return true;
    }

    private Rect CaptionArea(HUDManager hud, RectTransform canvasRect, float unit)
    {
        var bounds=canvasRect.rect;
        var camera=canvas!.renderMode==RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if(RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,Screen.safeArea.min,camera,out var a) &&
           RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect,Screen.safeArea.max,camera,out var b))
            bounds=Rect.MinMaxRect(Mathf.Max(bounds.xMin,a.x),Mathf.Max(bounds.yMin,a.y),Mathf.Min(bounds.xMax,b.x),Mathf.Min(bounds.yMax,b.y));
        float gap=Mathf.Max(10,unit*0.6f);
        var safe=Rect.MinMaxRect(bounds.xMin+gap,bounds.yMin+gap,bounds.xMax-gap,bounds.yMax-gap);
        float left=Mathf.Max(safe.xMin,safe.xMax-bounds.width*0.32f);
        float slotsRight=float.NegativeInfinity, slotsTop=float.NegativeInfinity;
        // Only read the existing slot geometry; no scene scan or original UI edits.
        if(hud.itemSlotIconFrames!=null) foreach(var slot in hud.itemSlotIconFrames)
        {
            if(slot==null || !slot.gameObject.activeInHierarchy) continue;
            slot.rectTransform.GetWorldCorners(slotCorners);
            foreach(var corner in slotCorners)
            {
                var point=canvasRect.InverseTransformPoint(corner);
                slotsRight=Mathf.Max(slotsRight,point.x); slotsTop=Mathf.Max(slotsTop,point.y);
            }
        }
        if(slotsRight+gap>left)
        {
            if(safe.xMax-slotsRight-gap>=unit*7) left=slotsRight+gap;
            else safe.yMin=Mathf.Max(safe.yMin,slotsTop+gap);
        }
        safe.xMin=left;
        return safe;
    }

    private static void SyncStyle(TextMeshProUGUI copy, TextMeshProUGUI template)
    {
        if (copy.font != template.font) copy.font = template.font;
        if (copy.fontSharedMaterial != template.fontSharedMaterial) copy.fontSharedMaterial = template.fontSharedMaterial;
        copy.canvasRenderer.SetAlpha(1);
        copy.alpha = 1;
    }

    private static void Place(RectTransform rect, float left, float top, float width, float height)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(left, -top);
        rect.sizeDelta = new Vector2(width, height);
    }

    private void SetState(string state, SensusSettings settings)
    {
        Status = state;
        if (state == lastLoggedState) return;
        // Always leave an activation/preview breadcrumb; detailed idle transitions
        // remain opt-in, so a normal match doesn't flood the log.
        if (settings.Diagnostics.Value || state == "visible-Preview" || state == "visible-Startup" ||
            state == "waiting-for-native-templates" || state.StartsWith("native-"))
            log.LogInfo($"Sensus UI state: {state}.");
        lastLoggedState = state;
    }

    internal void Clear()
    {
        peripheral.Clear();
        if (panel != null) Object.Destroy(panel.gameObject);
        panel = null;
        canvas = null;
        background = null;
        header = caption = null;
        checkedFont = null;
        measuredFont=null; measuredText=measuredTitle="";
        owner = null;
    }
}
