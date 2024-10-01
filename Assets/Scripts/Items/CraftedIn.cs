using Identifiables;
using Unity.Entities;

struct CraftedIn : IBufferElementData
{
    public Identifiable Crafter;
}