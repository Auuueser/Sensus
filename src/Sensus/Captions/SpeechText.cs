using System.Text;
namespace Sensus.Captions;

internal static class SpeechText
{
    internal static bool Active(bool local,bool dead,bool speaking,bool muted,float volume,float amplitude) =>
        !local && !dead && speaking && !muted && volume>0 && amplitude>0.0005f;
    internal static string SafeName(string raw)
    {
        var text=new StringBuilder(32);
        foreach(char c in raw)
        {
            if(text.Length>=32) break;
            if(char.IsControl(c) || c is '<' or '>' || (c>='\u202a' && c<='\u202e') || (c>='\u2066' && c<='\u2069')) continue;
            text.Append(c);
        }
        return text.Length==0 ? "Player" : text.ToString();
    }
}
