using Identifiables;
using System.Collections.Generic;
using Unity.Entities;

[UpdateInGroup(typeof(IdentifiablesMappingGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Default)]
public abstract partial class IdentifiableMappingSystem : SystemBase
{
    public struct IdentifiableLink
    {
        public Identifieable To;
        public string Relation;
    }

    public Dictionary<Identifieable, List<IdentifiableLink>> Relations = new();
    
    protected void AddRelation(Identifieable from, IdentifiableLink link)
    {
        if(!Relations.ContainsKey(from))
            Relations.Add(from, new List<IdentifiableLink>());
        Relations[from].Add(link);
    }
}