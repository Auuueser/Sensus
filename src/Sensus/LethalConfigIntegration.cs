using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using LethalConfig;
using LethalConfig.ConfigItems;
using LethalConfig.ConfigItems.Options;

namespace Sensus;

internal static class LethalConfigIntegration
{
    // Keep optional assembly types behind a non-inlined method boundary.
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static Action<bool> Register(SensusSettings settings, bool chinese)
    {
        LethalConfigManager.SkipAutoGen();
        var labels = new List<Action<bool>>();
        T Describe<T>(T options, string sectionEn, string sectionZh, string en, string zh, string descriptionEn, string descriptionZh) where T : BaseOptions
        {
            options.RequiresRestart = false;
            void Apply(bool cn)
            {
                options.Section = cn ? sectionZh : sectionEn;
                options.Name = cn ? zh : en;
                options.Description = cn ? descriptionZh : descriptionEn;
            }
            Apply(chinese);
            labels.Add(Apply);
            return options;
        }
        LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(settings.SpokenSubtitles,
            Describe(new BoolCheckBoxOptions(), "Dialogue", "音频对白", "Spoken subtitles", "音频对白字幕",
                "Independent of event display mode. LC Chinese Project owns dialogue when loaded (including manual English mode). Manage its subtitles in its settings; provider readiness is not verified.",
                "独立于事件显示模式。安装汉化时对白由 LC-Chinese-Project 管理（含 Sensus 手动英文模式），请在汉化设置中调整。检测到插件不代表已验证字幕服务就绪。")));
        LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(settings.Enabled,
            Describe(new BoolCheckBoxOptions(), "General", "常规", "Enabled", "启用提示",
                "Enable local creature sound captions and peripheral cues.", "启用本地生物声音字幕与周边提示。")));
        LethalConfigManager.AddConfigItem(new TextDropDownConfigItem(settings.DisplayMode,
            Describe(new TextDropDownOptions(), "Peripheral", "周边声音", "Display mode", "显示模式",
                "Captions: subtitles only. Peripheral: indicators only. Hybrid: both. Applies immediately.", "Captions：仅字幕；Peripheral：仅周边提示；Hybrid：字幕与提示同时显示。即时生效。提示旁的名称仍由“显示声音标签”控制。")));
        LethalConfigManager.AddConfigItem(new FloatSliderConfigItem(settings.RingSize,
            Describe(new FloatSliderOptions(), "Peripheral", "周边声音", "Ring size", "方向环范围",
                "Move markers closer to or farther from the center without changing hearing range.", "调整标记距画面中心的范围，不改变听域。")));
        LethalConfigManager.AddConfigItem(new FloatSliderConfigItem(settings.MarkerScale,
            Describe(new FloatSliderOptions(), "Peripheral", "周边声音", "Marker size", "标记大小",
                "Scale symbols and their sound labels.", "缩放声音图形及名称标签。")));
        LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(settings.MarkerCount,
            Describe(new IntInputFieldOptions { Min=0, Max=int.MaxValue }, "Peripheral", "周边声音", "Maximum markers", "详细标记上限",
                SensusSettings.CountHelpEn, SensusSettings.CountHelpZh)));
        LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(settings.MarkerLabels,
            Describe(new BoolCheckBoxOptions(), "Peripheral", "周边声音", "Sound labels", "显示声音标签",
                "Keep names visible while learning sound symbols.", "学习图形含义时保留声音名称。")));
        LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(settings.LowMotion,
            Describe(new BoolCheckBoxOptions(), "Peripheral", "周边声音", "Low motion", "低动态模式",
                "No decorative pulse or smooth movement; directions still update.", "关闭装饰性扩张和平滑移动；方向仍正常更新。")));
        LethalConfigManager.AddConfigItem(new TextDropDownConfigItem(settings.Language,
            Describe(new TextDropDownOptions(), "General", "常规", "Language", "语言",
                "Auto detects LC Chinese Project by GUID only. Changes apply live; reopen the menu to refresh its labels.", "Auto 按汉化插件 GUID 自动切换，不受版本号影响；Chinese/English 可手动覆盖。提示立即切换，重新打开菜单刷新名称。")));
        LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(settings.Preview,
            Describe(new BoolCheckBoxOptions(), "Preview", "预览", "Show preview", "显示演示字幕",
                "Close the menu in a round to see a labelled native-style example. Turn off after testing.", "进入游戏回合并关闭菜单后显示原生样式演示字幕。它不代表真实怪物；测试后请关闭。")));
        LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(settings.UnlocatedIndicators,
            Describe(new BoolCheckBoxOptions(), "Peripheral", "周边声音", "Unlocated indicators", "显示无方向提示",
                "Fixed tray for sounds without a reliable bearing. On by default.", "为无法可靠定位的声音显示固定提示栏，默认开启，不影响字幕。")));
        LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(settings.UnlocatedCount,
            Describe(new IntInputFieldOptions { Min=0, Max=int.MaxValue }, "Peripheral", "周边声音", "Maximum unlocated", "无方向提示上限",
                SensusSettings.CountHelpEn, SensusSettings.CountHelpZh)));
        LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(settings.UnlocatedText,
            Describe(new BoolCheckBoxOptions(), "Peripheral", "周边声音", "Show Unlocated text", "显示“无方向”文字",
                "Append Unlocated to fixed-tray labels. Off by default; does not toggle indicators.", "在右下角提示名称后显示“无方向”文字，默认关闭；不影响无方向提示本身，即时生效。")));
        LethalConfigManager.AddConfigItem(new FloatSliderConfigItem(settings.Scale,
            Describe(new FloatSliderOptions(), "Layout", "布局", "Caption and unlocated scale", "字幕与无方向提示缩放",
                "Scale captions and unlocated cards together, applied immediately.", "共同缩放字幕与无方向提示，即时生效。")));
        LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(settings.BottomAligned,
            Describe(new BoolCheckBoxOptions(), "Layout", "布局", "Dock to bottom", "底部等距对齐",
                "Match bottom and right margins; turn off to use manual vertical position.", "底部与右侧等距；关闭后恢复手动纵向位置，即时生效。")));
        LethalConfigManager.AddConfigItem(new FloatSliderConfigItem(settings.VerticalPosition,
            Describe(new FloatSliderOptions(), "Layout", "布局", "Vertical position", "纵向位置",
                "0 is the bottom of the screen; 1 is the top. Applied immediately.", "0 为屏幕底部，1 为顶部；即时生效。")));
        LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(settings.ShowDirection,
            Describe(new BoolCheckBoxOptions(), "Captions", "字幕", "Show directions", "显示声音方向",
                "Coarse directions for spatial sounds. Unclear directions are labelled.", "空间声音显示粗略方位；不明确时会标注。")));
        LethalConfigManager.AddConfigItem(new FloatSliderConfigItem(settings.CaptionDuration,
            Describe(new FloatSliderOptions(), "Captions", "字幕", "Hold seconds", "字幕保留时间",
                "Retain past sounds with frozen directions. This does not mean the danger ended.", "保留刚才听到的声音，方向冻结；不代表危险解除。")));
        LethalConfigManager.AddConfigItem(new IntInputFieldConfigItem(settings.MaxCaptions,
            Describe(new IntInputFieldOptions { Min=0, Max=int.MaxValue }, "Captions", "字幕", "Maximum rows", "最大显示条数",
                SensusSettings.CountHelpEn, SensusSettings.CountHelpZh)));
        LethalConfigManager.AddConfigItem(new FloatSliderConfigItem(settings.MinimumGain,
            Describe(new FloatSliderOptions(), "Advanced", "高级", "Audibility threshold", "可感知阈值",
                "Prototype estimated gain threshold. Lower values include quieter sounds; listening calibration is pending.", "原型的估计音量阈值；越低越容易显示轻声，尚需试听校准。")));
        LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(settings.Diagnostics,
            Describe(new BoolCheckBoxOptions(), "Advanced", "高级", "Diagnostic counters", "诊断计数",
                "Write summary counts every ten seconds. Does not record audio or voice.", "每十秒记录汇总计数，不录制声音或语音。")));
        LethalConfigManager.AddConfigItem(new BoolCheckBoxConfigItem(settings.ShowBackground,
            Describe(new BoolCheckBoxOptions(), "Layout", "布局", "Show backgrounds", "显示声音背景板",
                "Show solid backgrounds behind captions and peripheral labels. Text and symbols stay visible when off.", "显示字幕和周边标签的纯色背景板；关闭后文字与图形仍显示。即时生效。")));
        LethalConfigManager.AddConfigItem(new FloatSliderConfigItem(settings.BackgroundOpacity,
            Describe(new FloatSliderOptions(), "Layout", "布局", "Background opacity", "声音背景板不透明度",
                "0 is transparent; 1 is opaque. Applies to captions and peripheral labels, without changing text opacity.", "0 全透明，1 不透明；同时用于字幕与周边标签，不影响文字透明度。即时生效。")));
        void Refresh(bool cn)
        {
            foreach (var apply in labels) apply(cn);
            LethalConfigManager.SetModDescription(cn
                ? "V81 生物听觉辅助：分类配色与图形、周边声音方向、短字幕与持续反馈。原生 UGUI 风格；逐事件实机与声学校准仍需验证。"
                : "V81 creature hearing assistance: category colors and shapes, peripheral directions and captions. Native UGUI style; per-event runtime and acoustic validation pending.");
        }
        Refresh(chinese);
        return Refresh;
    }
}
