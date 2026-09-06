using System;

namespace Sensus.Captions;

internal sealed class PlaybackClock
{
    private float lastSample;
    internal float Elapsed { get; private set; }
    internal PlaybackClock(float now) => lastSample = now;
    internal void Reset(float now) { lastSample=now; Elapsed=0; }
    internal void Sample(float now, float pitch, bool paused)
    {
        if (!paused) Elapsed += Math.Max(0, now - lastSample) * Math.Abs(pitch);
        lastSample = now;
    }
}
