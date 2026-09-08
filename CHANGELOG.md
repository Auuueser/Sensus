# 更新日志 / Changelog

## 中文

<details>
<summary><strong>1.0.1 · 修复</strong></summary>

- 修复预加载模组使用内存加载游戏程序集时，声音采集无法启动的问题。
- 修复音响关闭后丢下，仍出现音乐方向提示的问题。
- 兼容多人扩容的语音混音通道，减少重复参数警告，并限制密集对讲机采样开销。
- 优化方向图标、字幕布局与密集事件缓存，减少重复网格重建和内存分配。
- 优化音频名称、衰减曲线与临时音源查找；关闭辅助时暂停采集处理。
- 无方向提示默认开启。
- 区分拾取、收起与取用声音：本人操作显示字幕，他人操作提供声音提示。
- 细分工具蓄力、开锁器安装、音响关闭及枪械保险音效，修正小刀刺击的提示与警示颜色。
- 普通物品碰响仅显示字幕。
- 优化行数限制；超额事件在顶部显示“另有 N 项”。
- 字幕与无方向提示共用缩放，默认 0.75。

</details>

<details>
<summary><strong>1.0.0 · Auditus</strong></summary>

- 提供周边声音方向提示、事件字幕与预设音频对白字幕。
- 支持仅字幕、仅提示及两者同时显示，并提供大小、底板与动态设置。
- 支持中英双语、LethalConfig 与 LC Chinese Project 对白字幕接管。
- 优化物品发声与电梯运行图标。
- 优化飞船降落、狒狒鹰与丛林狼脚步图标。
- 修复多种环境声音互相遮蔽提示，以及提示满额时的轮换遗漏。
- 支持无上限数量设置，加入密集场景布局、字幕轮换与屏幕空间保护。
- 修复连续转头时方向提示异常绕圈。
- 缓和行走与穿过声源附近时的额外避让移动。
- 稳定多声源避让位置，取消右下角内容随方向提示经过而闪烁。
- 修复下方方向提示消失，恢复居中椭圆轨道与方向提示优先显示。
- 改善英文后方标签与方向跟随，将本人物品栏中的声音归入无方向提示。
- 确立 Auditus / Visus 双方向；当前实现 Auditus，Visus 计划后续支持。
- 采用 GPL-3.0，统一文档与徽章风格。

</details>

<details>
<summary><strong>0.1.0 · 内测迭代</strong></summary>

- 建立生物、玩家、物品、车辆、设施与环境声音适配。
- 加入方向图标、字幕、警示配色与音频节奏反馈。
- 根据实机反馈优化声音分工、重复提示及界面布局。

</details>

## English

<details>
<summary><strong>1.0.1 · Fixes</strong></summary>

- Fixed sound capture failing to start when preloaders load the game assembly into memory.
- Fixed music indicators reappearing when dropping a boombox after turning it off.
- Supported shared voice mixer channels in expanded lobbies, reduced repeated parameter warnings, and bounded sampling work for crowded radio scenes.
- Optimized directional icons, caption layout and dense-event caches to reduce repeated mesh rebuilds and allocations.
- Optimized audio name and curve lookups, batched temporary-source discovery, and paused capture work while assistance is disabled.
- Enabled unlocated indicators by default.
- Distinguished pickup, stowing and equipping sounds: local actions use captions, while other players' actions provide sound indicators.
- Added distinct tool windup, lock picker mounting, boombox stopping and gun safety cues; corrected knife strike labels and warning colors.
- Routed ordinary item rattling to captions.
- Refined row limits and added an overflow notice above the cards.
- Shared scaling between captions and unlocated indicators, defaulting to 0.75.

</details>

<details>
<summary><strong>1.0.0 · Auditus</strong></summary>

- Added directional sound indicators, event captions and supported prerecorded dialogue subtitles.
- Supported captions, indicators, or both, with size, background and motion settings.
- Included Chinese and English, LethalConfig integration and LC Chinese Project dialogue ownership.
- Refined generic item-sound and elevator-operation icons.
- Refined ship landing, baboon hawk and bush-wolf footstep icons.
- Fixed ambient indicators suppressing each other and events being starved when indicator slots are full.
- Added unlimited count settings, dense-scene layouts, caption rotation and screen-space safeguards.
- Fixed spurious full-circle indicator motion during repeated turns.
- Eased avoidance motion while walking around or past nearby sound sources.
- Stabilized multi-sound avoidance and removed collision-driven caption/tray flicker.
- Fixed disappearing lower-direction indicators and restored a centered elliptical orbit with directional display priority.
- Improved rear English labels and smooth direction tracking; routed locally carried item sounds to unlocated indicators.
- Established the Auditus / Visus direction: Auditus is available; Visus is planned.
- Adopted GPL-3.0 and refreshed documentation and badges.

</details>

<details>
<summary><strong>0.1.0 · Internal testing</strong></summary>

- Established audio adaptation for creatures, players, items, vehicles, facilities and environments.
- Introduced directional icons, captions, warning colors and rhythm feedback.
- Refined sound routing, duplicate handling and layout through playtest feedback.

</details>
