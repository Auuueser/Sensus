using System.Collections.Generic;
using BepInEx.Logging;
using Sensus.Captions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sensus;

internal sealed class PeripheralPresenter
{
    private sealed class Marker
    {
        internal RectTransform Root = null!;
        internal SoundMarkerGraphic Graphic = null!;
        internal SoundMarkerGraphic Arc = null!;
        internal TextMeshProUGUI Label = null!;
        internal Image Backing = null!;
        internal int Id;
        internal float Angle, Sector;
        internal bool Initialized;
        internal Vector2 Position;
        internal float OrbitAngle, OrbitOffset, DisplayOffset, OffsetVelocity;
        internal Cue LabelCue = (Cue)(-1);
        internal bool LabelChinese, LabelRear, LabelUnlocatedText;
        internal float LabelUnit;
        internal TMP_FontAsset? LabelFont;
        internal int Omitted;



        internal float PlacedUnit = -1;
    }
    private float summaryMeasuredUnit=-1, summaryTextWidth;
    private string summaryMeasuredText="";
    private TMP_FontAsset? summaryMeasuredFont;
    private int summaryCount=-1;
    private bool summaryChinese;
    private Image? summaryBacking;
    private TextMeshProUGUI? traySummary;
    private RectTransform? root;
    private readonly List<Marker> markers = new();
    private readonly List<Marker> overflow = new();
    private readonly List<Marker> tray = new();
    private readonly float[] placed=new float[DisplayCapacity.SignalSafety];
    private int placedCount;
    private readonly SoundField demo = new();
    private int positionCount;
    private bool previewing;
    private float previewYaw, previewStart;
    private readonly ManualLogSource log;
    private bool firstRealMarker;
    private float trayWidth, trayRowHeight, trayGap, trayUnit;
    private int trayCount;
    internal float UnlocatedHeight => trayCount==0 ? 0 : trayCount*trayRowHeight+(trayCount-1)*trayGap+(traySummary!=null && traySummary.gameObject.activeSelf ? trayUnit : 0);
    internal PeripheralPresenter(ManualLogSource log) => this.log = log;

    internal void Tick(Canvas canvas, TextMeshProUGUI template, SoundField field, SensusSettings settings, bool chinese, bool preview, Rect fullSafe)
    {
        if (root == null)
        {
            var go = new GameObject("SensusPeripheralSounds", typeof(RectTransform), typeof(CanvasGroup));
            go.SetActive(false); go.transform.SetParent(canvas.transform, false);
            root = go.GetComponent<RectTransform>();
            root.anchorMin = Vector2.zero; root.anchorMax = Vector2.one; root.offsetMin = root.offsetMax = Vector2.zero;
            var group = go.GetComponent<CanvasGroup>(); group.blocksRaycasts = false; group.interactable = false;
            var imageTemplate = template.GetComponentInParent<Image>();
            if (imageTemplate == null) { Clear(); return; }

            for (int i = 0; i < 8; i++) overflow.Add(Create(template, imageTemplate, "AdditionalSoundDirection", root));
            summaryBacking=NativeUiTemplates.CloneImage(imageTemplate,root,"UnlocatedOverflowBacking");
            traySummary=NativeUiTemplates.CloneText(template,root,"UnlocatedOverflowSummary");
            log.LogInfo("Sensus peripheral UGUI created: pooled sound symbols, native labels and critical overflow arcs.");
        }
        root.gameObject.SetActive(true);
        if(root.GetSiblingIndex()!=root.parent.childCount-1) root.SetAsLastSibling();
        var camera = GameNetworkManager.Instance?.localPlayerController?.gameplayCamera;
        float yaw = camera != null ? camera.transform.eulerAngles.y : 0;
        float now = Time.unscaledTime;
        if (preview != previewing) foreach (var marker in markers) marker.Initialized = false;
        if (preview)
        {
            if (!previewing) { previewStart = now; previewYaw = yaw; demo.Clear(); }
            // World-fixed demo bearings let turning teach the real interaction.
            demo.Observe(-1, Cue.Music, now, previewYaw-90, true, true, 0.2f, false, WindingEnvelope.At((now-previewStart)%43), (now-previewStart)%43);
            demo.Observe(-2, Cue.Reload, now, previewYaw+90, true, true, 0.5f);
            if ((now-previewStart)%8 < 5) demo.Observe(-3, Cue.Growl, now, previewYaw+180, true, true, 0.1f);
            field = demo;
        }
        previewing = preview;
        float unit = Mathf.Max(14, template.fontSize) * settings.MarkerScale.Value;
        var rect = ((RectTransform)canvas.transform).rect;
        var layoutBounds=rect;
        var uiCamera=canvas.renderMode==RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if(RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform,Screen.safeArea.min,uiCamera,out var safeMin) &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform,Screen.safeArea.max,uiCamera,out var safeMax))
            layoutBounds=Rect.MinMaxRect(Mathf.Max(rect.xMin,safeMin.x),Mathf.Max(rect.yMin,safeMin.y),Mathf.Min(rect.xMax,safeMax.x),Mathf.Min(rect.yMax,safeMax.y));
        float margin=unit*0.5f;
        // A centered ellipse, independent of all caption/tray counts and contents.
        float rx=Mathf.Max(1,Mathf.Min(rect.width*0.5f*settings.RingSize.Value,
            Mathf.Min(rect.center.x-layoutBounds.xMin,layoutBounds.xMax-rect.center.x)-unit*3.75f-margin));
        float ry=Mathf.Max(1,Mathf.Min(rect.height*0.5f*settings.RingSize.Value,
            Mathf.Min(rect.center.y-layoutBounds.yMin,layoutBounds.yMax-rect.center.y)-unit*5.1f-margin));
        positionCount=DisplayCapacity.Rows(2*Mathf.PI*Mathf.Min(rx,ry),unit*8.6f);
        int desired=DisplayCapacity.Resolve(settings.MarkerCount.Value,positionCount);
        float trayBase=Mathf.Max(14,template.fontSize)*settings.Scale.Value*0.85f;
        int trayFit=DisplayCapacity.Rows(fullSafe.height*0.75f-trayBase,trayBase*2.95f);
        int desiredTray=settings.UnlocatedIndicators.Value ? DisplayCapacity.Resolve(settings.UnlocatedCount.Value,trayFit) : 0;
        field.Update(now,desired,yaw,desiredTray);
        var poolTemplate=template.GetComponentInParent<Image>();
        if(poolTemplate==null) return;
        // Grow from actual selected events, at most four clones per pool per frame.
        Grow(markers,field.Visible.Count,template,poolTemplate,"Sound");
        Grow(tray,field.Unlocated.Count,template,poolTemplate,"UnlocatedSound");
        // Rotate only through slots that are ready, rather than losing selected events.
        if(markers.Count<field.Visible.Count || tray.Count<field.Unlocated.Count)
            field.Update(now,Mathf.Min(desired,markers.Count),yaw,Mathf.Min(desiredTray,tray.Count));
        Match(markers,field.Visible); Match(tray,field.Unlocated);
        // Reorder only when selection changes; cycling every sibling each frame
        // dirties the entire native Canvas even when the final order is unchanged.
        for(int i=0;i<field.Visible.Count && i<markers.Count;i++)
            if(markers[i].Root.GetSiblingIndex()!=root.childCount-field.Visible.Count+i) markers[i].Root.SetAsLastSibling();
        if(traySummary!=null)
        {
            int extra=field.OmittedUnlocated;
            traySummary.gameObject.SetActive(settings.UnlocatedIndicators.Value && field.Unlocated.Count>0 && extra>0);
            if(extra>0 && (summaryCount!=extra || summaryChinese!=chinese)) { summaryCount=extra; summaryChinese=chinese; string summary=chinese ? "另有 "+extra+" 项" : "+ "+extra+" more"; if(traySummary.text!=summary) traySummary.text=summary; }
            traySummary.color=new Color(0.96f,0.96f,0.92f,1);
            if(summaryBacking!=null)
            {
                summaryBacking.gameObject.SetActive(traySummary.gameObject.activeSelf && settings.ShowBackground.Value && settings.BackgroundOpacity.Value>0);
                summaryBacking.color=new Color(0.015f,0.02f,0.015f,1);
                summaryBacking.canvasRenderer.SetAlpha(settings.BackgroundOpacity.Value);
            }
        }
        placedCount=0;
        for (int i = 0; i < markers.Count+tray.Count; i++)
        {
            bool unlocated = i>=markers.Count;
            unit=Mathf.Max(14,template.fontSize)*(unlocated ? settings.Scale.Value : settings.MarkerScale.Value);
            int index=unlocated ? i-markers.Count : i;
            var marker = unlocated ? tray[index] : markers[index];
            var signals=unlocated ? field.Unlocated : field.Visible;
            if ((unlocated && !settings.UnlocatedIndicators.Value) || index >= signals.Count || (!unlocated && !settings.ShowDirection.Value))
            { marker.Root.gameObject.SetActive(false); marker.Initialized = false; continue; }
            var signal = signals[index];
            if(unlocated && !SoundField.CanUnlocate(signal)) { marker.Root.gameObject.SetActive(false); continue; }
            if (!unlocated && !preview && !firstRealMarker && SoundField.IsLive(signal,now))
            {
                firstRealMarker=true;
                log.LogInfo("First real audible direction submitted to the Sensus peripheral UI; visual confirmation is still required.");
            }
            float relative = SoundField.Relative(signal.Bearing, yaw);
            bool fresh=!marker.Initialized || marker.Id!=signal.Id;
            if (fresh)
            {
                marker.Id = signal.Id; marker.Initialized = true;
                marker.Sector = CueText.DirectionIndex(relative)*45f; marker.Angle = relative;
            }
            // Stop reprojecting a past sound when the player turns after silence.
            if (SoundField.IsLive(signal, now))
            {
                marker.Sector = SoundField.SectorAngle(relative, marker.Sector);
                marker.Angle = IndicatorDetail.Follow(marker.Angle,relative,Time.unscaledDeltaTime,settings.LowMotion.Value);
            }
            bool compact = false;
            marker.Root.gameObject.SetActive(true);
            if(!unlocated) SizeMarker(marker,unit);
            if(!unlocated)
            {
                float step=360f/Mathf.Max(1,positionCount);
                if(fresh) { marker.OrbitOffset=0; marker.OffsetVelocity=0; }
                marker.OrbitOffset=StableOrbit.Offset(signal.Bearing,marker.OrbitOffset,placed,placedCount,step);
                if(fresh) marker.DisplayOffset=marker.OrbitOffset;
                else marker.DisplayOffset=StableOrbit.Ease(marker.DisplayOffset,marker.OrbitOffset,ref marker.OffsetVelocity,Time.unscaledDeltaTime,settings.LowMotion.Value);
                float targetAngle=marker.Angle+marker.DisplayOffset;
                marker.OrbitAngle=fresh ? targetAngle : IndicatorDetail.Follow(marker.OrbitAngle,targetAngle,Time.unscaledDeltaTime,settings.LowMotion.Value);
                marker.Position=Orbit(marker.OrbitAngle,rx,ry);
                marker.Root.anchoredPosition=marker.Position;
                placed[placedCount++]=signal.Bearing+marker.OrbitOffset;

            }

            float alpha = SoundField.Alpha(signal, now);
            var tint=SoundMarkerGraphic.Tint(signal.Cue, alpha);
            float urgency=WindingEnvelope.Urgency(signal.WindingSeconds);
            if(signal.Cue==Cue.Music && signal.WindingSeconds>=0)
                tint=new Color(1,0.8f-0.55f*urgency,0.25f-0.03f*urgency,alpha);
            // Color animation uses the renderer multiplier, avoiding a mesh rebuild
            // for every frame of the winding gradient. Reset pooled non-music markers.
            marker.Graphic.color=Color.white; marker.Graphic.canvasRenderer.SetColor(tint);
            marker.Arc.color=Color.white; marker.Arc.canvasRenderer.SetColor(tint);
            // The expensive glyph is independent of bearing. Rotate a separate
            // static arc with the transform, never regenerate the whole glyph.
            marker.Graphic.Set(0, signal.Cue, signal.Strength, compact, false);
            marker.Arc.gameObject.SetActive(!unlocated);
            marker.Arc.Set(0,signal.Cue,1,true,true);
            marker.Arc.rectTransform.localRotation=Quaternion.Euler(0,0,-marker.Angle);
            marker.Label.font = template.font; marker.Label.fontSharedMaterial = template.fontSharedMaterial;
            marker.Label.color = new Color(0.96f,0.96f,0.92f,1);
            marker.Label.canvasRenderer.SetAlpha(alpha);
            bool rear = !unlocated && CueText.DirectionIndex(marker.Angle)==4;
            bool showUnlocatedText=unlocated && settings.UnlocatedText.Value;
            int omitted=0;
            bool layoutChanged = marker.Omitted!=omitted || marker.LabelCue != signal.Cue || marker.LabelChinese != chinese || marker.LabelRear != rear ||
                marker.LabelUnit != unit || marker.LabelFont != template.font || marker.LabelUnlocatedText != showUnlocatedText;
            bool labels = settings.MarkerLabels.Value && !compact;
            marker.Label.gameObject.SetActive(labels);
            marker.Backing.gameObject.SetActive(labels && settings.ShowBackground.Value && settings.BackgroundOpacity.Value > 0);
            marker.Backing.color = new Color(0.015f,0.02f,0.015f,1);
            marker.Backing.canvasRenderer.SetAlpha(settings.BackgroundOpacity.Value*alpha);
            if (layoutChanged)
            {
                marker.Label.enableAutoSizing=false; marker.Label.fontSize=unit*0.8f;
                marker.Label.text = IndicatorDetail.Label(signal.Cue,chinese,rear,showUnlocatedText) + (omitted>0 ? (chinese ? "\n另有 " : "\n+ ")+omitted+(chinese ? " 项" : " more") : "");
                marker.Omitted=omitted;
                marker.LabelCue=signal.Cue; marker.LabelChinese=chinese; marker.LabelRear=rear;
                marker.LabelUnit=unit; marker.LabelFont=template.font; marker.LabelUnlocatedText=showUnlocatedText;
                float labelWidth=unlocated ? Mathf.Min(unit*8,rect.width*0.27f) : unit*6.5f;
                var measured = marker.Label.GetPreferredValues(marker.Label.text, labelWidth, Mathf.Infinity);
                // Text and backing share a top edge and the measured text height.
                // Centering text in a fixed taller box put it below the backing.
                float labelHeight=unlocated ? measured.y : Mathf.Min(measured.y,unit*3.2f);
                marker.Label.enableWordWrapping=true;
                marker.Label.enableAutoSizing=!unlocated;
                marker.Label.fontSizeMin=unit*0.55f; marker.Label.fontSizeMax=unit*0.8f;
                marker.Label.overflowMode=TextOverflowModes.Ellipsis;
                marker.Label.rectTransform.sizeDelta = new Vector2(labelWidth,labelHeight);
                marker.Backing.rectTransform.sizeDelta = new Vector2(Mathf.Min(labelWidth,measured.x)+unit*0.5f,labelHeight+unit*0.35f);
            }
            float age = now-signal.Started;
            float pulse = CueVisual.OnsetScale(signal.Cue, age, settings.LowMotion.Value);
            bool live=SoundField.IsLive(signal,now);
            if(signal.Cue==Cue.Music)
                pulse=settings.LowMotion.Value || !live ? 1 : 1+(0.10f+0.22f*urgency)*signal.MusicEnvelope;
            marker.Graphic.rectTransform.localScale = Vector3.one*pulse;
            marker.Graphic.rectTransform.localRotation=Quaternion.Euler(0,0,
                signal.Cue==Cue.Music ? WindingEnvelope.Shake(signal.WindingSeconds,live,settings.LowMotion.Value) : 0);
        }
        unit=Mathf.Max(14,template.fontSize)*settings.MarkerScale.Value;
        for (int i = 0; i < overflow.Count; i++)
        {
            var marker = overflow[i];
            bool visible = settings.ShowDirection.Value && layoutBounds.width>unit*3 && layoutBounds.height>unit*3 && (field.OverflowDirections & (1<<i)) != 0;
            marker.Root.gameObject.SetActive(visible); if (!visible) continue;
            Place(marker,rect,i*45,0,unit,Mathf.Min(0.88f,settings.RingSize.Value+0.06f));
            var point=marker.Root.anchoredPosition;
            marker.Root.anchoredPosition=new Vector2(
                Mathf.Clamp(point.x,layoutBounds.xMin-rect.center.x+unit*1.5f,layoutBounds.xMax-rect.center.x-unit*1.5f),
                Mathf.Clamp(point.y,layoutBounds.yMin-rect.center.y+unit*1.5f,layoutBounds.yMax-rect.center.y-unit*1.5f));
            marker.Label.gameObject.SetActive(false); marker.Backing.gameObject.SetActive(false);
            marker.Graphic.color = SoundMarkerGraphic.Tint(Cue.Gunshot,0.8f);
            marker.Graphic.Set(i*45,Cue.Gunshot,1,true);
        }
    }
    internal void MeasureUnlocated(Rect safe, float unit)
    {
        trayCount=0; trayUnit=unit; trayGap=unit*0.35f;
        trayWidth=Mathf.Min(unit*11,safe.width);
        trayRowHeight=unit*2.6f;
        if(root==null || !root.gameObject.activeSelf) return;
        foreach(var marker in tray) if(marker.Root.gameObject.activeSelf) trayCount++;
    }
    internal void ArrangeUnlocated(Rect safe, float bottom, Vector2 canvasCenter)
    {
        float top=Mathf.Min(safe.yMax,Mathf.Max(safe.yMin,bottom)+UnlocatedHeight);
        if(traySummary!=null && traySummary.gameObject.activeSelf)
        {
            var summary=traySummary.rectTransform;
            summary.anchorMin=summary.anchorMax=new Vector2(0.5f,0.5f); summary.pivot=new Vector2(1,0.5f);
            summary.anchoredPosition=new Vector2(safe.xMax,top-trayUnit*0.4f)-canvasCenter;
            if(summaryMeasuredUnit!=trayUnit || summaryMeasuredText!=traySummary.text || summaryMeasuredFont!=traySummary.font)
            {
                traySummary.fontSize=trayUnit*0.55f; traySummary.enableAutoSizing=false;
                summaryTextWidth=traySummary.GetPreferredValues(traySummary.text,Mathf.Infinity,Mathf.Infinity).x;
                summaryMeasuredUnit=trayUnit; summaryMeasuredText=traySummary.text; summaryMeasuredFont=traySummary.font;
            }
            summary.sizeDelta=new Vector2(Mathf.Min(trayWidth,summaryTextWidth+trayUnit*0.8f),trayUnit*0.8f);
            traySummary.fontSize=trayUnit*0.55f; traySummary.enableAutoSizing=false;
            traySummary.alignment=TextAlignmentOptions.Right; traySummary.enableWordWrapping=false;
            traySummary.margin=new Vector4(trayUnit*0.4f,0,trayUnit*0.4f,0);
            if(summaryBacking!=null)
            {
                var backing=summaryBacking.rectTransform;
                backing.anchorMin=summary.anchorMin; backing.anchorMax=summary.anchorMax; backing.pivot=summary.pivot;
                backing.anchoredPosition=summary.anchoredPosition; backing.sizeDelta=summary.sizeDelta;
            }
            top-=trayUnit;
        }
        int row=0;
        for(int i=0;i<tray.Count;i++)
        {
            var m=tray[i]; if(!m.Root.gameObject.activeSelf) continue;
            m.Root.pivot=new Vector2(1,0.5f);
            m.Root.anchoredPosition=new Vector2(safe.xMax,top-trayRowHeight*0.5f-row++*(trayRowHeight+trayGap))-canvasCenter;
            m.Root.sizeDelta=new Vector2(trayWidth,trayRowHeight);
            float icon=Mathf.Min(trayUnit*2.2f,trayRowHeight*0.85f);
            var graphic=m.Graphic.rectTransform;
            graphic.anchorMin=graphic.anchorMax=new Vector2(0,0.5f);
            graphic.anchoredPosition=new Vector2(trayUnit*0.4f+icon*0.5f,0);
            graphic.sizeDelta=new Vector2(icon,icon);
            var text=m.Label.rectTransform;
            text.anchorMin=text.anchorMax=new Vector2(0,0.5f); text.pivot=new Vector2(0,0.5f);
            text.anchoredPosition=new Vector2(trayUnit*2.8f,0);
            text.sizeDelta=new Vector2(Mathf.Max(1,trayWidth-trayUnit*3.2f),Mathf.Max(1,trayRowHeight-trayUnit*0.8f));
            m.Label.alignment=TextAlignmentOptions.Center; m.Label.overflowMode=TextOverflowModes.Ellipsis;
            m.Label.enableAutoSizing=true; m.Label.fontSizeMin=trayUnit*0.55f; m.Label.fontSizeMax=trayUnit*0.8f;
            var backing=m.Backing.rectTransform;
            if(backing.GetSiblingIndex()!=0) backing.SetAsFirstSibling();
            backing.anchorMin=Vector2.zero; backing.anchorMax=Vector2.one;
            backing.pivot=new Vector2(0.5f,0.5f); backing.offsetMin=backing.offsetMax=Vector2.zero;
        }
    }
    private static Vector2 Orbit(float angle,float rx,float ry) => new Vector2(IndicatorDetail.OrbitX(angle,rx),IndicatorDetail.OrbitY(angle,ry));
    private static void Match(List<Marker> pool,IReadOnlyList<SoundField.Signal> signals)
    {
        for(int i=0;i<signals.Count && i<pool.Count;i++)
        {
            if(pool[i].Id==signals[i].Id) continue;
            for(int j=i+1;j<pool.Count;j++) if(pool[j].Id==signals[i].Id)
            { var swap=pool[i]; pool[i]=pool[j]; pool[j]=swap; break; }
        }
    }
    private void Grow(List<Marker> pool, int count, TextMeshProUGUI template, Image imageTemplate, string name)
    {
        int target=Mathf.Min(count,pool.Count+4);
        while(pool.Count<target) pool.Add(Create(template,imageTemplate,name,root!));
    }
    private static Marker Create(TextMeshProUGUI template, Image imageTemplate, string name, RectTransform parent)
    {
        var go = new GameObject(name,typeof(RectTransform)); go.SetActive(false); go.transform.SetParent(parent,false);
        var rect = go.GetComponent<RectTransform>(); rect.anchorMin=rect.anchorMax=new Vector2(0.5f,0.5f);
        var icon = new GameObject("DirectionAndSound",typeof(RectTransform)); icon.transform.SetParent(rect,false);
        var graphic = icon.AddComponent<SoundMarkerGraphic>(); graphic.raycastTarget=false; graphic.maskable=false;
        var arcObject=new GameObject("DirectionArc",typeof(RectTransform)); arcObject.transform.SetParent(icon.transform,false);
        var arc=arcObject.AddComponent<SoundMarkerGraphic>(); arc.raycastTarget=false; arc.maskable=false;
        arc.gameObject.SetActive(false);
        arc.rectTransform.anchorMin=Vector2.zero; arc.rectTransform.anchorMax=Vector2.one;
        arc.rectTransform.offsetMin=arc.rectTransform.offsetMax=Vector2.zero;
        var backing = NativeUiTemplates.CloneImage(imageTemplate,rect,"LabelContrast");
        var label = NativeUiTemplates.CloneText(template,rect,"SoundLabel"); label.alignment=TextAlignmentOptions.Top;
        return new Marker { Root=rect,Graphic=graphic,Arc=arc,Label=label,Backing=backing };
    }
    private static void Place(Marker marker, Rect rect, float angle, int lane, float unit, float radius)
    {
        marker.Root.gameObject.SetActive(true);
        float a=angle*Mathf.Deg2Rad;
        float rx=Mathf.Max(unit*3,rect.width*0.5f*radius-lane*unit*4.8f);
        float ry=Mathf.Max(unit*3,rect.height*0.5f*radius-lane*unit*3.8f);
        float maxX=Mathf.Max(0,rect.width*0.5f-unit*4.75f-8);
        float minY=-rect.height*0.5f+unit*4.2f+8;
        float maxY=Mathf.Max(minY,rect.height*0.5f-unit*1.5f-8);
        marker.Root.anchoredPosition=new Vector2(Mathf.Clamp(Mathf.Sin(a)*rx,-maxX,maxX),Mathf.Clamp(Mathf.Cos(a)*ry,minY,maxY));
        SizeMarker(marker,unit);
    }
    private static void SizeMarker(Marker marker,float unit)
    {
        if(marker.PlacedUnit==unit) return;
        marker.PlacedUnit=unit;
        marker.Root.sizeDelta=new Vector2(unit*9,unit*4);
        marker.Graphic.rectTransform.sizeDelta=new Vector2(unit*2.8f,unit*2.8f);
        marker.Graphic.rectTransform.anchorMin=marker.Graphic.rectTransform.anchorMax=new Vector2(0.5f,0.5f);
        marker.Graphic.rectTransform.pivot=new Vector2(0.5f,0.5f);
        marker.Graphic.rectTransform.anchoredPosition=Vector2.zero;
        var text=marker.Label.rectTransform;
        text.anchorMin=text.anchorMax=new Vector2(0.5f,0.5f); text.pivot=new Vector2(0.5f,1);
        text.anchoredPosition=new Vector2(0,-unit*1.4f);
        var backing=marker.Backing.rectTransform;
        backing.anchorMin=backing.anchorMax=text.anchorMin; backing.pivot=text.pivot;
        backing.anchoredPosition=text.anchoredPosition+new Vector2(0,unit*0.15f);
    }
    internal void Hide() { trayCount=0; if(root!=null) root.gameObject.SetActive(false); previewing=false; }
    internal void Clear()
    {
        if(root!=null) Object.Destroy(root.gameObject);
        root=null; traySummary=null; summaryBacking=null; summaryCount=-1; summaryMeasuredUnit=-1; markers.Clear(); tray.Clear(); overflow.Clear(); demo.Clear(); previewing=false; firstRealMarker=false;
    }
}
