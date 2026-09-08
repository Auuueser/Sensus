using BepInEx.Configuration;

namespace Sensus;

internal sealed class SensusSettings
{
    internal ConfigEntry<bool> SpokenSubtitles { get; }
    internal ConfigEntry<bool> Enabled { get; }
    internal ConfigEntry<string> Language { get; }
    internal ConfigEntry<bool> Preview { get; }
    internal ConfigEntry<bool> UnlocatedIndicators { get; }
    internal ConfigEntry<bool> UnlocatedText { get; }
    internal ConfigEntry<float> Scale { get; }
    internal ConfigEntry<bool> BottomAligned { get; }
    internal ConfigEntry<float> VerticalPosition { get; }
    internal ConfigEntry<bool> ShowDirection { get; }
    internal ConfigEntry<float> CaptionDuration { get; }
    internal ConfigEntry<int> MaxCaptions { get; }
    internal ConfigEntry<float> MinimumGain { get; }
    internal ConfigEntry<bool> Diagnostics { get; }
    internal ConfigEntry<float> BackgroundOpacity { get; }
    internal ConfigEntry<bool> ShowBackground { get; }
    internal ConfigEntry<string> DisplayMode { get; }
    internal ConfigEntry<float> RingSize { get; }
    internal ConfigEntry<float> MarkerScale { get; }
    internal ConfigEntry<int> MarkerCount { get; }
    internal ConfigEntry<int> UnlocatedCount { get; }
    internal const string CountHelpEn = "0 = unlimited; positive values set a display limit. Large counts may reduce FPS and readability. Screen space and overload protection still apply; excess events rotate by priority.";
    internal const string CountHelpZh = "0＝无上限，正数为显示上限。数量过多可能降低帧率和可读性；仍受屏幕空间与过载保护约束，放不下的事件按优先级轮换。";
    internal const string CountHelp = CountHelpEn + " / " + CountHelpZh;
    internal ConfigEntry<bool> MarkerLabels { get; }
    internal ConfigEntry<bool> LowMotion { get; }

    internal SensusSettings(ConfigFile config)
    {
        SpokenSubtitles=config.Bind("Dialogue", "Enabled", true,
            "Timed spoken subtitles, independent of event display mode. With LC Chinese Project loaded, dialogue is managed there; Sensus does not change its settings or claim it is ready. / 音频对白字幕，独立于事件显示模式；安装 LC-Chinese-Project 时交由汉化管理，不修改其设置，也不保证其字幕服务已就绪。");
        DisplayMode = config.Bind("Peripheral", "DisplayMode", "Hybrid", new ConfigDescription(
            "Hybrid: captions and indicators. Captions: subtitles only. Peripheral: indicators only. / Hybrid：字幕与提示；Captions：仅字幕；Peripheral：仅周边提示。", new AcceptableValueList<string>("Hybrid", "Captions", "Peripheral")));
        RingSize = config.Bind("Peripheral", "RingSize", 0.65f, new ConfigDescription(
            "Direction marker distance from screen center, as a fraction of half the screen. / 周边标记距中心的范围，占屏幕半径比例。", new AcceptableValueRange<float>(0.45f, 0.82f)));
        MarkerScale = config.Bind("Peripheral", "MarkerScale", 1f, new ConfigDescription(
            "Size of direction symbols and their labels. / 方向图形与标签大小。", new AcceptableValueRange<float>(0.75f, 1.75f)));
        MarkerCount = config.Bind("Peripheral", "MaximumMarkers", 10, new ConfigDescription(CountHelp, new AcceptableValueRange<int>(0, int.MaxValue)));
        UnlocatedCount = config.Bind("Peripheral", "MaximumUnlocated", 3, new ConfigDescription(CountHelp, new AcceptableValueRange<int>(0, int.MaxValue)));
        MarkerLabels = config.Bind("Peripheral", "ShowLabels", true, "Show sound names beside direction symbols. / 在方向图形旁显示声音名称。");
        LowMotion = config.Bind("Peripheral", "LowMotion", false, "Disable decorative pulses and smooth transitions. / 关闭装饰性扩张与平滑过渡，保留方向变化。");
        Enabled = config.Bind("General", "Enabled", true,
            "Enable sound captions and preview. / 启用声音字幕与预览。");
        Language = config.Bind("General", "Language", "Auto", new ConfigDescription(
            "Auto detects LC Chinese Project by GUID, independent of version. English / Chinese override it. / Auto 按 GUID 检测汉化，不比较版本；English 强制英文，Chinese 强制中文。",
            new AcceptableValueList<string>("Auto", "English", "Chinese")));
        Preview = config.Bind("Preview", "ShowPreview", false,
            "Show a labelled demonstration after closing menus. This is not a detected monster. / 关闭菜单后显示标注为演示的字幕，不代表检测到怪物；测试完请关闭。");
        UnlocatedIndicators = config.Bind("Peripheral", "ShowUnlocated", true,
            "Show a fixed tray for genuinely unlocated sounds. On by default; captions remain available. / 显示无法定位声音的固定提示栏，默认开启；字幕仍可用。");
        UnlocatedText = config.Bind("Peripheral", "ShowUnlocatedText", false,
            "Append Unlocated to names in the fixed tray. Does not enable the tray itself. / 在右下角提示名称后显示“无方向”，默认关闭；不影响提示本身的开关。");
        Scale = config.Bind("Layout", "CaptionScale", 0.75f, new ConfigDescription(
            "Shared scale for captions and unlocated cards. / 字幕与无方向提示共用缩放。", new AcceptableValueRange<float>(0.75f, 2f)));
        BottomAligned = config.Bind("Layout", "BottomAligned", true, "Align bottom and right margins; disable to use manual vertical position. / 底部与右侧等距，关闭后使用手动纵向位置。");
        VerticalPosition = config.Bind("Layout", "VerticalPosition", 0.24f, new ConfigDescription(
            "Vertical screen position: 0 bottom, 1 top. / 屏幕纵向位置：0 底部，1 顶部。",
            new AcceptableValueRange<float>(0.1f, 0.85f)));
        ShowDirection = config.Bind("Captions", "ShowDirection", true, "Show coarse directions for spatial sounds. / 为空间声音显示粗略方向。");
        CaptionDuration = config.Bind("Captions", "HoldSeconds", 2.5f, new ConfigDescription(
            "How long to retain a heard event after it becomes inaudible. / 停止听见后保留字幕的秒数。", new AcceptableValueRange<float>(1f, 8f)));
        MaxCaptions = config.Bind("Captions", "MaxRows", 5, new ConfigDescription(
            CountHelp, new AcceptableValueRange<int>(0, int.MaxValue)));
        MinimumGain = config.Bind("Advanced", "MinimumEstimatedGain", 0.015f, new ConfigDescription(
            "Prototype audibility threshold; requires listening calibration. / 原型可感知阈值，需要试听校准。", new AcceptableValueRange<float>(0.005f, 0.1f)));
        Diagnostics = config.Bind("Advanced", "Diagnostics", false, "Log bounded counters every 10 seconds; no audio recording. / 每 10 秒记录汇总计数，不录音。");
        ShowBackground = config.Bind("Layout", "ShowBackground", true,
            "Show solid backgrounds behind captions and peripheral sound labels. / 显示字幕与周边声音标签的纯色背景板。");
        BackgroundOpacity = config.Bind("Layout", "BackgroundOpacity", 0.65f, new ConfigDescription(
            "Background opacity for captions and sound labels: 0 transparent, 1 opaque. Text stays visible. / 字幕与声音标签底板不透明度：0 全透明，1 不透明；不影响文字。", new AcceptableValueRange<float>(0f, 1f)));
    }
}
