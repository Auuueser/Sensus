using System;
using System.Collections.Generic;
using System.Text;

namespace Sensus.Captions;

internal enum Cue
{
    // Keep previous enum values stable: new cues are appended after VehicleInteraction.
    Growl, Roar, Breathing, Lunge, Impact, Creature,
    Underground, GroundRumble, Emerge, GroundImpact,
    Music, BoxOpen, Screaming, MechanicalTurn, MechanicalAlert,
    Aim, Reload, Gunshot, Ricochet, GunClick,
    Footsteps, Crying, Clicking, Rustling, Transformation, Bite, Liquid,
    Heartbeat, March, Drum, Snip, Spring, Crack, Cling, Web, Chitter,
    Wings, Slime, Spray, Rattle, Sweeping, Buzz, Burst, TensionMusic,
    Flopping, Sliding, Spit, VisorHit, Whining, HeadRustling, Fire,
    Cackle, Electric, Broadcast, Launch, Explosion, Engine, Howl,
    Climbing, Scratch, Peck, BirdCall, DoorImpact, Alarm, WallRumble,
    Door, MaskSound, Laughter, HeavyFootsteps, NeckSnap, PipeFlow, PipeBurst, Steam, Jump, Landing, DoorOpen, DoorClose, OtherCling, OtherNeckSnap, MaskAttachLocal, MaskInfection, LocalMaskInfection, MaskLaugh, Voice, RadioVoice, GroundRadioVoice, ItemNoise, ItemDrop, SupplyLanding, VehicleDelivery, VehicleEngine, RadioSignal, Thunder, StaticWarning, BridgeCreak, BridgeCollapse, Horn, Jetpack, EquipmentWarning, BatteryWarning, PinPull, Ladder, ToolSwing, SprayPaint, Skidding, VehicleImpact, VehicleFire, RadioMusic, ItemMusic, Cushion, BuzzAlert, BuzzAttack, RadioRelay, Running, LadderClimb, LockPicking, Unlock, Vent, Charging, Elevator, Apparatus, Remote, Clock, Breaker, PowerHum, Terminal, SpikeSlam, SpikeCreak, Inhaling, Zap, ShipHum, ShipMechanism, Cabinet, Lever, WeedSpray, Flies, Teeth, Hairdryer, Phone, ToyRobot, EggCall, Crawling, Mechanism, VehicleInteraction,
    DragFootsteps, Injury, BodyImpact, Underwater, WorldHeartbeat, Environment, LightSwitch, Teleport, RadarPing, UiFeedback, UiWarning, EarRinging, PoisonFeedback, BackgroundMusic, Television,
    ItemPickup, AirHorn, ClownHorn, DuckQuack, CashRegister, GiftOpen, BagZip, FlashlightSwitch, SmallFootsteps, BareFootsteps, SkitterFootsteps, SkippingFootsteps, BootFootsteps, CreatureHit,
    CanShake, ToyTrain, LaserSwitch, KnifeAttack, SuitChange, SeatEject, BodyCrush, DeathSound, Fan, WaterSplash, RadarSwitch, PressureDoor, ShutterDoor, MineBeep, MinePress, Plushie, RecordPlayer, Toilet, Candle, Shower, Microwave, Pumpkin, InverseTeleport, ShipDoor, VehicleBoost, VehicleRefuel, AmbientFire, ElevatorMusic, BreakerDoor, BreakerSwitch, VehicleJump, CreatureAttack, CreatureDeath, TzpEmpty, TzpRelease, FurniturePlace, Fridge, MicrowaveDoor, ChairShock, ZedDog, Drowning, MudSink, ShipLanding, ShipTakeoff, EggCry, EggScream, EggBreak, DiscoMusic, CarHood, CabinetDoor, ShipTravel, ShipArrival, BloomBreath, BloomBurst, BloomChomp, BloomRoar, BloomTreatment, PlantGrowth, SporeCloud, BloomChestOpen, PlantClear, MeteorApproach, MeteorImpact, WolfCall, WolfGrowl, WolfSnarl, WolfTongue, WolfPull, WolfAttack, WolfHit, WolfDeath, WolfFootsteps, CruiserHood, CruiserDoor, CruiserRearDoor, CruiserIgnition, CruiserStarted, CruiserKey,
    // BEGIN GENERATED CREATURE CUES
    DetailBaboonBirdAICackle,
    DetailBaboonBirdAICreatureAttack,
    DetailBaboonBirdAICreatureDeath,
    DetailBaboonBirdAICreatureHit,
    DetailBaboonBirdAIFootsteps,
    DetailBaboonBirdAIScreaming,
    DetailBlobAILiquid,
    DetailBlobAISlime,
    DetailButlerBeesEnemyAIBuzz,
    DetailButlerBeesEnemyAIElectric,
    DetailButlerEnemyAIBurst,
    DetailButlerEnemyAIBuzz,
    DetailButlerEnemyAIFootsteps,
    DetailButlerEnemyAIRustling,
    DetailButlerEnemyAISweeping,
    DetailButlerEnemyAIToolSwing,
    DetailCaveDwellerAIBite,
    DetailCaveDwellerAIBreathing,
    DetailCaveDwellerAIClicking,
    DetailCaveDwellerAICreatureDeath,
    DetailCaveDwellerAICrying,
    DetailCaveDwellerAIFootsteps,
    DetailCaveDwellerAIGrowl,
    DetailCaveDwellerAILiquid,
    DetailCaveDwellerAIRustling,
    DetailCaveDwellerAIScreaming,
    DetailCaveDwellerAISmallFootsteps,
    DetailCentipedeAICreatureDeath,
    DetailCentipedeAIFootsteps,
    DetailCentipedeAIImpact,
    DetailCentipedeAIScreaming,
    DetailClaySurgeonAIDrum,
    DetailClaySurgeonAIMarch,
    DetailClaySurgeonAISnip,
    DetailCrawlerAICreatureDeath,
    DetailCrawlerAICreatureHit,
    DetailCrawlerAIHeavyFootsteps,
    DetailCrawlerAIImpact,
    DetailCrawlerAIRoar,
    DetailDepositItemsDeskGroundRumble,
    DetailDocileLocustBeesAIBuzz,
    DetailDoublewingAIBirdCall,
    DetailDoublewingAICreatureDeath,
    DetailDoublewingAICreatureHit,
    DetailDoublewingAIImpact,
    DetailDoublewingAIWings,
    DetailDressGirlAIBreathing,
    DetailDressGirlAIFootsteps,
    DetailDressGirlAILaughter,
    DetailDressGirlAISkippingFootsteps,
    DetailFlowerSnakeEnemyLaughter,
    DetailFlowerSnakeEnemyLunge,
    DetailFlowerSnakeEnemyWings,
    DetailFlowermanAIFootsteps,
    DetailForestGiantAIBite,
    DetailForestGiantAICreatureHit,
    DetailForestGiantAIFire,
    DetailForestGiantAIGroundImpact,
    DetailForestGiantAIHeavyFootsteps,
    DetailForestGiantAIRoar,
    DetailGiantKiwiAIBirdCall,
    DetailGiantKiwiAIBreathing,
    DetailGiantKiwiAICreatureAttack,
    DetailGiantKiwiAICreatureDeath,
    DetailGiantKiwiAIDoorImpact,
    DetailGiantKiwiAIFootsteps,
    DetailGiantKiwiAIPeck,
    DetailHoarderBugAIChitter,
    DetailHoarderBugAICreatureDeath,
    DetailHoarderBugAIScreaming,
    DetailHoarderBugAISkitterFootsteps,
    DetailHoarderBugAIWings,
    DetailJesterAIHeavyFootsteps,
    DetailJesterAIMechanicalTurn,
    DetailJesterAIScreaming,
    DetailMouthDogAIBreathing,
    DetailMouthDogAIGrowl,
    DetailMouthDogAIHeavyFootsteps,
    DetailMouthDogAILunge,
    DetailNutcrackerEnemyAIBootFootsteps,
    DetailNutcrackerEnemyAICreatureHit,
    DetailNutcrackerEnemyAIGrowl,
    DetailNutcrackerEnemyAIImpact,
    DetailNutcrackerEnemyAIMechanicalAlert,
    DetailNutcrackerEnemyAIMechanicalTurn,
    DetailPufferAIChitter,
    DetailPufferAIImpact,
    DetailPufferAIRattle,
    DetailPufferAISpray,
    DetailPumaAIBreathing,
    DetailPumaAIClimbing,
    DetailPumaAICreatureDeath,
    DetailPumaAICreatureHit,
    DetailPumaAIFootsteps,
    DetailPumaAIGrowl,
    DetailPumaAIImpact,
    DetailPumaAIRustling,
    DetailPumaAIScratch,
    DetailPumaAIScreaming,
    DetailRadMechAIEngine,
    DetailRadMechAIExplosion,
    DetailRadMechAIFire,
    DetailRadMechAIHeavyFootsteps,
    DetailRadMechAIMechanicalAlert,
    DetailRedLocustBeesBuzz,
    DetailRedLocustBeesElectric,
    DetailSandSpiderAIBite,
    DetailSandSpiderAICreatureDeath,
    DetailSandSpiderAICreatureHit,
    DetailSandSpiderAIRustling,
    DetailSandSpiderAISkitterFootsteps,
    DetailSandWormAIEmerge,
    DetailSandWormAIGroundImpact,
    DetailSandWormAIGroundRumble,
    DetailSandWormAIRoar,
    DetailSandWormAIUnderground,
    DetailSnowmanSimpleAIImpact,
    DetailSnowmanSimpleAILaughter,
    DetailSpringManAIBareFootsteps,
    DetailSpringManAISpring,
    DetailStingrayAICreatureAttack,
    DetailStingrayAICreatureDeath,
    DetailStingrayAIFlopping,
    DetailStingrayAISliding,
    DetailStingrayAISpit,
    DetailStingrayAIWhining
    // END GENERATED CREATURE CUES
    , SpringRetract, NutcrackerKick, NutcrackerFall, SlimeHit, PoolFloaty, VehicleRattle,
    CounterBell, CounterShutter, CounterGrab, CounterAttack, CounterWarning, CounterAmbience, CounterSnore, AccessHatch, WarehouseDoor, ItemStow, ToolWindup, LockMount, ItemHandling, MusicStop, ItemEquip, OtherItemPickup, OtherItemStow, OtherItemEquip, GunSafetyOn, GunSafetyOff, GunSafetyBlocked
}

internal static class CueText
{
    private static readonly string[] English = { "Growling", "Roaring", "Heavy breathing", "Lunging sound", "Impact", "Creature sound",
        "Underground rumbling", "Loud ground rumbling", "Ground breaking", "Heavy ground impact",
        "Winding music", "Box bursting open", "Screaming", "Mechanical turning", "Mechanical sound",
        "Mechanical aiming sound", "Reloading", "Gunshot", "Ricochet", "Gun mechanism clicking" };
    private static readonly string[] Chinese = { "低吼声", "咆哮声", "急促呼吸声", "扑动声", "撞击声", "生物声",
        "地下轰鸣", "强烈地面隆响", "破土声", "沉重落地声",
        "上弦音乐", "箱体爆开声", "尖叫声", "机械转动声", "机械声",
        "机械瞄准声", "装填声", "枪声", "跳弹声", "枪械咔嗒声" };
    private static readonly string[] DirectionsEn = { "Front", "Front-right", "Right", "Back-right", "Behind", "Back-left", "Left", "Front-left" };
    private static readonly string[] DirectionsZh = { "前方", "右前方", "右侧", "右后方", "后方", "左后方", "左侧", "左前方" };
    internal static string Name(Cue cue, bool chinese) => CreatureSoundCatalog.Label(cue,chinese) ?? ((int)cue < English.Length
        ? (chinese ? Chinese : English)[(int)cue] : ExtendedCueText.Name(cue, chinese));
    internal static string Direction(int direction, bool chinese) => direction < 0 ? (chinese ? "方位不明" : "Direction unclear") : (chinese ? DirectionsZh : DirectionsEn)[direction];
    internal static int DirectionIndex(float angle) => (int)Math.Floor(((angle % 360 + 360) % 360 + 22.5f) / 45) % 8;
    internal static int Priority(Cue cue) => cue==Cue.CounterAttack ? 3 : cue==Cue.CounterWarning ? 2 : BasePriority(CreatureSoundCatalog.Basis(cue));
    private static int BasePriority(Cue cue) => cue is Cue.NutcrackerKick or Cue.BloomChestOpen or Cue.MeteorImpact or Cue.WolfTongue or Cue.WolfPull or Cue.WolfAttack or Cue.BloomBurst or Cue.BloomChomp or Cue.ChairShock or Cue.Drowning or Cue.EggScream or Cue.MinePress or Cue.BodyCrush or Cue.DeathSound or Cue.Injury or Cue.UiWarning or Cue.PoisonFeedback or Cue.SpikeSlam or Cue.StaticWarning or Cue.BridgeCollapse or Cue.EquipmentWarning or Cue.VehicleFire or Cue.BuzzAttack or Cue.GroundRumble or Cue.BoxOpen or Cue.Aim or Cue.Gunshot or Cue.Roar or
        Cue.Transformation or Cue.Explosion or Cue.Launch or Cue.Alarm or Cue.Cling or Cue.VisorHit or Cue.DoorImpact or Cue.NeckSnap or Cue.PipeBurst or Cue.OtherCling or Cue.OtherNeckSnap or Cue.MaskInfection or Cue.LocalMaskInfection ? 3 :
        cue is Cue.GunSafetyOff or Cue.SlimeHit or Cue.PlantGrowth or Cue.MeteorApproach or Cue.WolfGrowl or Cue.WolfSnarl or Cue.WolfHit or Cue.BloomRoar or Cue.SporeCloud or Cue.MineBeep or Cue.DragFootsteps or Cue.MudSink or Cue.CreatureAttack or Cue.EggCry or Cue.VehicleJump or Cue.VehicleBoost or Cue.KnifeAttack or Cue.SeatEject or Cue.BootFootsteps or Cue.CreatureHit or Cue.Voice or Cue.SpikeCreak or Cue.Running or Cue.Zap or Cue.RadioRelay or Cue.BridgeCreak or Cue.BatteryWarning or Cue.PinPull or Cue.VehicleImpact or Cue.BuzzAlert or Cue.MaskSound or Cue.Music or Cue.Underground or Cue.Reload or Cue.Screaming or Cue.Crying or Cue.Drum or Cue.Snip or
        Cue.Fire or Cue.Electric or Cue.Spit or Cue.Heartbeat or Cue.HeadRustling or Cue.Growl or Cue.HeavyFootsteps or Cue.TensionMusic or Cue.Steam or Cue.Bite or Cue.RadioVoice or Cue.GroundRadioVoice or Cue.SupplyLanding or Cue.VehicleDelivery ? 2 : 1;
}

// Only audible observations enter this buffer. Historical directions are frozen.
internal sealed class CaptionBuffer
{
    internal sealed class Entry
    {
        internal int Source;
        internal Cue Cue;
        internal float LastHeard;
        internal float FirstHeard;
        internal int Direction;
        internal bool Spatial;
        internal bool Live;
        internal bool Continuous;
        internal bool LocalMovement;
        internal bool Fallback;
        internal string Speaker = "";
    }
    private readonly List<Entry> entries = new();
    private readonly List<Entry> visible = new();
    private readonly StringBuilder text = new();
    private readonly Stack<Entry> pool = new(64);
    private string composed="";
    private void Retire(int index)
    {
        var entry=entries[index]; entries.RemoveAt(index); entry.Speaker="";
        if(pool.Count<64) pool.Push(entry);
    }
    internal int Count => entries.Count;
    internal void BeginFrame() { foreach (var e in entries) e.Live = false; }
    internal void Observe(int source, Cue cue, float now, int direction, bool spatial, bool continuous, bool localMovement = false, string speaker = "", bool captionFallback = false)
    {
        if(CreatureSoundCatalog.Basis(cue) is Cue.Footsteps or Cue.Running)
            foreach(var prior in entries) if(prior.Source==source && prior.Cue==Cue.DragFootsteps && now-prior.LastHeard<0.85f) return;
        if(CreatureSoundCatalog.Basis(cue) is Cue.Footsteps or Cue.Running)
            for(int i=entries.Count-1;i>=0;i--) if(entries[i].Source==source && CreatureSoundCatalog.Basis(entries[i].Cue)!=CreatureSoundCatalog.Basis(cue) && CreatureSoundCatalog.Basis(entries[i].Cue) is Cue.Footsteps or Cue.Running) Retire(i);
        if(cue==Cue.DragFootsteps)
            for(int i=entries.Count-1;i>=0;i--) if(entries[i].Source==source && CreatureSoundCatalog.Basis(entries[i].Cue) is Cue.Footsteps or Cue.Running) Retire(i);
        Entry? found = null;
        foreach (var entry in entries) if (entry.Source == source && CreatureSoundCatalog.Basis(entry.Cue) == CreatureSoundCatalog.Basis(cue)) { found = entry; break; }
        if (found == null)
        {
            if (entries.Count >= 64)
            {
                int victim = 0;
                for (int i = 1; i < entries.Count; i++)
                    if (CueText.Priority(entries[i].Cue) < CueText.Priority(entries[victim].Cue) ||
                        (CueText.Priority(entries[i].Cue) == CueText.Priority(entries[victim].Cue) && entries[i].LastHeard < entries[victim].LastHeard)) victim = i;
                if (CueText.Priority(entries[victim].Cue) > CueText.Priority(cue)) return;
                Retire(victim);
            }
            found = pool.Count>0 ? pool.Pop() : new Entry();
            found.Source=source; found.Cue=cue; found.FirstHeard=now;
            entries.Add(found);
        }
        found.Cue=EventIdentity.Display(found.Cue,cue,now-found.LastHeard);
        found.LastHeard = now;
        found.Direction = direction;
        found.Spatial = spatial;
        found.Continuous = continuous;
        found.Live = true;
        found.LocalMovement = localMovement;
        found.Fallback = captionFallback;
        found.Speaker = speaker;
    }
    internal string Compose(float now, float holdSeconds, int maxRows, bool chinese, bool directions, bool compact = false)
    {
        for(int i=entries.Count-1;i>=0;i--) if(now-entries[i].LastHeard>holdSeconds) Retire(i);
        visible.Clear();
        foreach(var e in entries)
        {
            if(now-e.LastHeard>0.65f && CreatureSoundCatalog.Basis(e.Cue) is Cue.Running or Cue.LadderClimb or Cue.Clock or Cue.Footsteps or Cue.Jump or Cue.Landing or Cue.Voice or Cue.RadioVoice or Cue.GroundRadioVoice) continue;
            // Hybrid assigns each event to one channel; text-only retains all cues.
            if(compact && ((!e.Fallback && !CuePresentation.Caption(e.Cue,e.LocalMovement)) || (now-e.LastHeard>0.65f && CueText.Priority(e.Cue)<2))) continue;
            bool duplicate=false;
            if(compact) for(int i=0;i<visible.Count;i++) if(visible[i].Cue==e.Cue && visible[i].Speaker==e.Speaker)
            {
                var prior=visible[i];
                if((e.Live && !prior.Live) || (e.Live==prior.Live && e.LastHeard>prior.LastHeard)) visible[i]=e;
                duplicate=true; break;
            }
            if(!duplicate) visible.Add(e);
        }
        visible.Sort((a, b) => { int priority = CueText.Priority(b.Cue).CompareTo(CueText.Priority(a.Cue)); return priority != 0 ? priority : a.FirstHeard.CompareTo(b.FirstHeard); });
        maxRows=DisplayCapacity.Resolve(maxRows,visible.Count);
        Omitted=Math.Max(0,visible.Count-maxRows);
        text.Clear();
        for (int i = 0; i < Math.Min(maxRows, visible.Count); i++)
        {
            // Keep priority groups ordered; only page equally ranked overflow.
            int groupStart=i, groupEnd=i+1, rank=CueText.Priority(visible[i].Cue);
            while(groupStart>0 && CueText.Priority(visible[groupStart-1].Cue)==rank) groupStart--;
            while(groupEnd<visible.Count && CueText.Priority(visible[groupEnd].Cue)==rank) groupEnd++;
            int count=groupEnd-groupStart;
            int offset=groupEnd>maxRows ? (int)((Math.Max(0,(double)now)/3)%count) : 0;
            var e = visible[groupStart+(i-groupStart+offset)%count];
            if (i != 0) text.Append('\n');
            // Event cards describe what was heard, not a sampled live/past state.
            // Keep wording stable through cadence gaps and remove on expiry.
            if (!e.LocalMovement && directions && e.Spatial) text.Append(CueText.Direction(e.Direction, chinese)).Append(" · ");
            if(e.Speaker.Length>0) text.Append(e.Speaker).Append(chinese ? "：" : ": ");
            text.Append(CueText.Name(e.Cue, chinese));
        }
        composed=StableText.Reuse(text,composed);
        return composed;
    }
    internal int Omitted { get; private set; }
    internal void Clear() { for(int i=entries.Count-1;i>=0;i--) Retire(i); visible.Clear(); Omitted=0; composed=""; }
}
