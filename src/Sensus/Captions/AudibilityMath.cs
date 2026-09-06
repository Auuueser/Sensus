using System;

namespace Sensus.Captions;

internal static class AudibilityMath
{
    // Occlusion changes audibility, not the existence of a coarse stereo bearing.
    internal static bool DirectionKnown(float blend,bool radio,float horizontalSquared) => blend>0.1f && !radio && horizontalSquared>=0.0001f;
    internal static float Linear(float distance, float min, float max) => max <= min ? (distance <= min ? 1f : 0f) : Math.Max(0f, Math.Min(1f, (max - distance) / (max - min)));
    // Unity logarithmic rolloff plateaus at maxDistance. This is a prototype estimate.
    internal static float Logarithmic(float distance, float min, float max) => Math.Max(0.001f, min) / Math.Max(Math.Max(0.001f, min), Math.Min(distance, Math.Max(min, max)));
    internal static float Gain(float sourceVolume, float oneShotScale, float spatialBlend, float distanceGain, float gameDecibels)
        => Math.Max(0, sourceVolume) * Math.Max(0, oneShotScale) * ((1 - spatialBlend) + spatialBlend * Math.Max(0, distanceGain)) * (float)Math.Pow(10, Math.Min(0, gameDecibels) / 20);
}
