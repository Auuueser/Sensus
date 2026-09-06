namespace Sensus.Captions;

internal static class WorldSoundPolicy
{
    // Facility and exterior are separate acoustic spaces, even while the dungeon loads.
    internal static bool ClockSpace(bool inShip, bool itemInside, bool listenerInside) =>
        inShip || itemInside==listenerInside;

    // Retain nearby device bearings only for an authored curve that becomes spatial.
    internal static float DeviceBlend(bool authoredSpatial, float distance, float maxDistance, float actualBlend) =>
        authoredSpatial && distance<=maxDistance ? 1 : actualBlend;
}
