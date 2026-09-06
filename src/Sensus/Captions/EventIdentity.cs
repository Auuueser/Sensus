namespace Sensus.Captions;
// A detailed recording and its shared fallback describe one heard event.
internal static class EventIdentity
{
    internal static Cue Display(Cue prior,Cue current,float gap) => gap<=0.8f &&
        CreatureSoundCatalog.Basis(prior)==CreatureSoundCatalog.Basis(current) &&
        CreatureSoundCatalog.Shape(prior)!=CreatureShape.None && CreatureSoundCatalog.Shape(current)==CreatureShape.None ? prior : current;
}
