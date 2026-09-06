using System;
namespace Sensus.Captions;

internal static class ScanBudget
{
    // At 30 Hz: <= 8 ticks for 1024 flies; <= 16 ticks for 2048 sources.
    // This bounds work and revisit count, not Unity wall-clock frame duration.
    internal static int For(int count,int minimum) => count<=0 ? 0 : Math.Min(count,Math.Min(128,Math.Max(minimum,(count+7)/8)));
}
