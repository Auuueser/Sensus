namespace Sensus.Captions;

// Two independently audible speakers, ranked stably. Empty timeline gaps never compete.
internal sealed class SpokenFrame
{
    private string first="", second="", cachedFirst="", cachedSecond="", cachedText="";
    private int firstRank,secondRank,firstId,secondId;
    internal void Clear() { first=second=""; firstRank=secondRank=0; }
    internal void Observe(int id,int rank,string text)
    {
        if(text.Length==0) return;
        if(first.Length>0 && id==firstId) { first=text; return; }
        if(second.Length>0 && id==secondId) { second=text; return; }
        if(first.Length==0 || rank>firstRank || (rank==firstRank && id<firstId))
        { second=first;secondRank=firstRank;secondId=firstId;first=text;firstRank=rank;firstId=id; }
        else if(second.Length==0 || rank>secondRank || (rank==secondRank && id<secondId))
        { second=text;secondRank=rank;secondId=id; }
    }
    internal string Text
    {
        get {
            if(first!=cachedFirst || second!=cachedSecond)
            { cachedFirst=first;cachedSecond=second;cachedText=second.Length==0 ? first : first+"\n"+second; }
            return cachedText;
        }
    }
}
