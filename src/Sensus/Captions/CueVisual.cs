namespace Sensus.Captions;

internal enum SoundFamily { Creature, Movement, Music, Mechanical, Ground, Liquid, Insects, Air, Personal, Impact }

// Color describes audible material/category, never a hidden enemy identity.
// Onset animation is observed; winding rhythm separately follows the audible clip envelope.
internal static class CueVisual
{
    internal static SoundFamily Family(Cue cue) => CreatureSoundCatalog.Basis(cue) switch
    {
        Cue.GunSafetyOn or Cue.GunSafetyOff or Cue.GunSafetyBlocked or Cue.OtherItemPickup or Cue.OtherItemStow or Cue.OtherItemEquip or Cue.LockMount or Cue.MusicStop or Cue.ItemHandling or Cue.ItemStow => SoundFamily.Mechanical,
        Cue.ToolWindup => SoundFamily.Movement,
        Cue.CounterBell or Cue.CounterShutter or Cue.AccessHatch or Cue.WarehouseDoor => SoundFamily.Mechanical,
        Cue.CounterGrab or Cue.CounterAttack => SoundFamily.Impact,
        Cue.CounterWarning or Cue.CounterAmbience => SoundFamily.Ground,
        Cue.CounterSnore => SoundFamily.Creature,
        Cue.PoolFloaty => SoundFamily.Impact,
        Cue.VehicleRattle or Cue.SpringRetract => SoundFamily.Mechanical,
        Cue.NutcrackerKick or Cue.NutcrackerFall or Cue.SlimeHit => SoundFamily.Impact,
        Cue.BodyCrush or Cue.DeathSound or Cue.KnifeAttack => SoundFamily.Impact,
        Cue.WaterSplash or Cue.Shower or Cue.Toilet => SoundFamily.Liquid,
        Cue.CruiserHood or Cue.CruiserDoor or Cue.CruiserRearDoor or Cue.CruiserIgnition or Cue.CruiserStarted or Cue.CruiserKey => SoundFamily.Mechanical,
        Cue.WolfFootsteps => SoundFamily.Movement,
        Cue.DiscoMusic => SoundFamily.Music,
        Cue.MeteorApproach or Cue.WolfTongue or Cue.WolfPull => SoundFamily.Air,
        Cue.MeteorImpact or Cue.PlantClear or Cue.BloomChestOpen or Cue.WolfAttack or Cue.WolfHit or Cue.WolfDeath => SoundFamily.Impact,
        Cue.WolfCall or Cue.WolfGrowl or Cue.WolfSnarl => SoundFamily.Creature,
        Cue.CarHood or Cue.CabinetDoor or Cue.ShipTravel or Cue.ShipArrival => SoundFamily.Mechanical,
        Cue.BloomBreath or Cue.BloomChomp or Cue.BloomRoar => SoundFamily.Creature,
        Cue.BloomBurst or Cue.BloomTreatment => SoundFamily.Impact,
        Cue.PlantGrowth => SoundFamily.Ground,
        Cue.SporeCloud => SoundFamily.Air,
        Cue.AmbientFire or Cue.CreatureAttack or Cue.CreatureDeath or Cue.ChairShock => SoundFamily.Impact,
        Cue.MudSink => SoundFamily.Ground,
        Cue.Drowning => SoundFamily.Personal,
        Cue.ElevatorMusic => SoundFamily.Music,
        Cue.EggCry or Cue.EggScream or Cue.EggBreak or Cue.ZedDog => SoundFamily.Creature,
        Cue.BreakerDoor or Cue.BreakerSwitch or Cue.VehicleJump or Cue.TzpEmpty or Cue.TzpRelease or Cue.FurniturePlace or Cue.Fridge or Cue.MicrowaveDoor or Cue.ShipLanding or Cue.ShipTakeoff => SoundFamily.Mechanical,
        Cue.VehicleBoost => SoundFamily.Air,
        Cue.VehicleRefuel => SoundFamily.Liquid,
        Cue.Fan => SoundFamily.Air,
        Cue.CanShake or Cue.ToyTrain or Cue.LaserSwitch or Cue.SuitChange or Cue.SeatEject or Cue.RadarSwitch or Cue.PressureDoor or Cue.ShutterDoor or Cue.MineBeep or Cue.MinePress or Cue.Plushie or Cue.RecordPlayer or Cue.Candle or Cue.Microwave or Cue.Pumpkin or Cue.InverseTeleport or Cue.ShipDoor => SoundFamily.Mechanical,
        Cue.SmallFootsteps or Cue.BareFootsteps or Cue.SkitterFootsteps or Cue.SkippingFootsteps or Cue.BootFootsteps => SoundFamily.Movement,
        Cue.CreatureHit => SoundFamily.Impact,
        Cue.AirHorn or Cue.ClownHorn or Cue.CashRegister or Cue.GiftOpen or Cue.BagZip or Cue.FlashlightSwitch => SoundFamily.Mechanical,
        Cue.DuckQuack => SoundFamily.Creature,
        Cue.WorldHeartbeat => SoundFamily.Mechanical,
        Cue.BackgroundMusic or Cue.Television => SoundFamily.Music,
        Cue.Environment => SoundFamily.Air,
        Cue.Injury or Cue.Underwater or Cue.EarRinging or Cue.PoisonFeedback => SoundFamily.Personal,
        Cue.BodyImpact => SoundFamily.Impact,
        Cue.LightSwitch or Cue.Teleport or Cue.RadarPing or Cue.UiFeedback or Cue.UiWarning => SoundFamily.Mechanical,
        Cue.DragFootsteps or Cue.Running or Cue.LadderClimb or Cue.Crawling => SoundFamily.Movement,
        Cue.Flies => SoundFamily.Insects,
        Cue.WeedSpray or Cue.Inhaling or Cue.Hairdryer => SoundFamily.Air,
        Cue.EggCall => SoundFamily.Creature,
        Cue.SpikeSlam or Cue.SpikeCreak => SoundFamily.Impact,
        Cue.VehicleInteraction or Cue.LockPicking or Cue.Unlock or Cue.Vent or Cue.Charging or Cue.Elevator or Cue.Apparatus or Cue.Remote or Cue.Clock or Cue.Breaker or Cue.PowerHum or Cue.Terminal or Cue.Zap or Cue.ShipHum or Cue.ShipMechanism or Cue.Cabinet or Cue.Lever or Cue.Teeth or Cue.Phone or Cue.ToyRobot or Cue.Mechanism => SoundFamily.Mechanical,
        Cue.Thunder or Cue.StaticWarning or Cue.VehicleImpact or Cue.VehicleFire => SoundFamily.Impact,
        Cue.BridgeCreak or Cue.BridgeCollapse => SoundFamily.Ground,
        Cue.RadioRelay or Cue.Horn or Cue.EquipmentWarning or Cue.BatteryWarning or Cue.PinPull or Cue.Ladder => SoundFamily.Mechanical,
        Cue.Jetpack or Cue.SprayPaint => SoundFamily.Air,
        Cue.ToolSwing or Cue.Skidding => SoundFamily.Movement,
        Cue.RadioMusic or Cue.ItemMusic => SoundFamily.Music,
        Cue.BuzzAlert or Cue.BuzzAttack => SoundFamily.Insects,
        Cue.Cushion => SoundFamily.Air,
        Cue.SupplyLanding or Cue.VehicleDelivery or Cue.VehicleEngine => SoundFamily.Mechanical,
        Cue.OtherCling or Cue.MaskSound or Cue.MaskAttachLocal or Cue.MaskInfection or Cue.LocalMaskInfection => SoundFamily.Personal,
        Cue.OtherNeckSnap or Cue.ItemDrop => SoundFamily.Impact,
        Cue.Music or Cue.March or Cue.Drum or Cue.TensionMusic => SoundFamily.Music,
        Cue.MechanicalTurn or Cue.MechanicalAlert or Cue.Aim or Cue.Reload or Cue.Gunshot or Cue.GunClick or Cue.Ricochet or
        Cue.Engine or Cue.Broadcast or Cue.DoorOpen or Cue.DoorClose or Cue.Door or Cue.Alarm or Cue.Snip or Cue.Spring => SoundFamily.Mechanical,
        Cue.Underground or Cue.GroundRumble or Cue.Emerge or Cue.GroundImpact or Cue.WallRumble => SoundFamily.Ground,
        Cue.Slime or Cue.Liquid or Cue.Spit or Cue.Sliding or Cue.Flopping => SoundFamily.Liquid,
        Cue.Buzz or Cue.Chitter or Cue.Rattle => SoundFamily.Insects,
        Cue.Wings or Cue.BirdCall or Cue.Spray or Cue.Steam => SoundFamily.Air,
        Cue.Heartbeat or Cue.VisorHit or Cue.HeadRustling or Cue.Cling or Cue.MaskSound => SoundFamily.Personal,
        Cue.Footsteps or Cue.HeavyFootsteps or Cue.Jump or Cue.Landing or Cue.Climbing or Cue.Sweeping or Cue.Rustling or Cue.Peck => SoundFamily.Movement,
        Cue.Impact or Cue.Burst or Cue.Explosion or Cue.Launch or Cue.DoorImpact or Cue.Electric or Cue.Fire or Cue.Crack or Cue.Scratch or Cue.NeckSnap or Cue.PipeBurst => SoundFamily.Impact,
        Cue.PipeFlow => SoundFamily.Liquid,
        _ => SoundFamily.Creature
    };
    internal static float OnsetScale(Cue cue, float age, bool lowMotion)
    {
        if(lowMotion || age<0 || age>=0.24f) return 1;
        float amount = CueText.Priority(cue)==3 ? 0.12f : 0.05f;
        return 1 + amount*(float)System.Math.Sin(age/0.24f*System.Math.PI);
    }
}
