using System.Text;
namespace Sensus.Captions;

internal static class StableText
{
    internal static string Reuse(StringBuilder text,string previous)
    {
        if(text.Length==previous.Length)
        {
            int i=0; while(i<text.Length && text[i]==previous[i]) i++;
            if(i==text.Length) return previous;
        }
        return text.ToString();
    }
}
