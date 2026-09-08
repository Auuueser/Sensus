namespace Sensus.Captions;

internal static class CuePresentation
{
    internal static bool Unlocated(Cue cue) => Indicator(cue) && CreatureSoundCatalog.Basis(cue) is not (Cue.ItemNoise or Cue.Mechanism);
    // External activity belongs in the sound field; self movement and mood do not.
    internal static bool Indicator(Cue cue, bool localMovement = false) =>
        !localMovement && CreatureSoundCatalog.Basis(cue) is not (Cue.ShipTravel or Cue.ShipArrival or Cue.Drowning or Cue.ItemPickup or Cue.ItemStow or Cue.ItemEquip or Cue.ItemHandling or Cue.BackgroundMusic or Cue.Injury or Cue.Underwater or Cue.UiFeedback or Cue.UiWarning or Cue.EarRinging or Cue.PoisonFeedback or Cue.ShipHum or Cue.Terminal or Cue.Heartbeat or Cue.TensionMusic or Cue.Cling or Cue.VisorHit or Cue.HeadRustling or Cue.NeckSnap or Cue.Broadcast or Cue.Creature or Cue.MaskAttachLocal or Cue.LocalMaskInfection or Cue.RadioVoice);

    // Only directly operated item feedback is personal. Released explosives and
    // world hazards remain external even if the player originally used the item.
    internal static bool ActorFeedback(Cue cue) => CreatureSoundCatalog.Basis(cue) is Cue.PlantClear or Cue.MudSink or Cue.WaterSplash or Cue.BodyImpact or Cue.BloomTreatment;
    internal static bool Personal(Cue cue, bool held, bool riding) =>
        (riding && cue==Cue.VehicleRattle) ||
        (held && CreatureSoundCatalog.Basis(cue) is (Cue.ToolWindup or Cue.LockMount or Cue.MusicStop or Cue.ZedDog or Cue.TzpEmpty or Cue.TzpRelease or Cue.CanShake or Cue.ToyTrain or Cue.LaserSwitch or Cue.RadarSwitch or Cue.AirHorn or Cue.ClownHorn or Cue.DuckQuack or Cue.CashRegister or Cue.GiftOpen or Cue.BagZip or Cue.FlashlightSwitch or Cue.Inhaling or Cue.Zap or Cue.WeedSpray or Cue.Teeth or Cue.Hairdryer or Cue.Phone or Cue.ToyRobot or Cue.Remote or Cue.Impact or Cue.ItemNoise or Cue.ItemMusic or Cue.Jetpack or Cue.EquipmentWarning or Cue.BatteryWarning or Cue.PinPull or Cue.Ladder or Cue.ToolSwing or Cue.SprayPaint or Cue.Gunshot or Cue.Reload or Cue.GunClick or Cue.RadioSignal)) ||
        (riding && CreatureSoundCatalog.Basis(cue) is (Cue.CruiserHood or Cue.CruiserDoor or Cue.CruiserRearDoor or Cue.CruiserIgnition or Cue.CruiserStarted or Cue.CruiserKey or Cue.VehicleJump or Cue.VehicleBoost or Cue.SeatEject or Cue.VehicleInteraction or Cue.Horn or Cue.Skidding or Cue.VehicleImpact or Cue.VehicleFire or Cue.EquipmentWarning or Cue.RadioMusic));
    internal static bool Ambient(Cue cue) => CreatureSoundCatalog.Basis(cue) is Cue.DiscoMusic or Cue.AmbientFire or Cue.ElevatorMusic or Cue.Fan or Cue.RecordPlayer or Cue.Candle or Cue.Shower or Cue.Television or Cue.Environment or Cue.WorldHeartbeat or Cue.Flies or Cue.PowerHum or Cue.Clock or Cue.ItemMusic or Cue.RadioMusic;
    internal static bool Both(Cue cue) => CreatureSoundCatalog.Basis(cue) is Cue.SupplyLanding or Cue.VehicleDelivery or Cue.GroundRadioVoice;
    internal static bool Caption(Cue cue, bool localMovement = false) => !Indicator(cue,localMovement) || Both(cue);
}
