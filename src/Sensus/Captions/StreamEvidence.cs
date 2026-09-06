using System;
namespace Sensus.Captions;

internal static class StreamEvidence
{
    // Unity output samples, not the Flatline clip's asset name or PTT state.
    // Reject constant carriers and silence. Nothing is retained or transcribed.
    internal static bool HasModulation(float[] samples)
    {
        if(samples.Length<16) return false;
        double sum=0, squares=0;
        foreach(float value in samples)
        {
            if(float.IsNaN(value) || float.IsInfinity(value)) return false;
            sum+=value; squares+=(double)value*value;
        }
        double mean=sum/samples.Length, energy=squares/samples.Length;
        double variance=Math.Max(0,energy-mean*mean);
        return variance>0.00000001 && mean*mean<variance*4;
    }
}
