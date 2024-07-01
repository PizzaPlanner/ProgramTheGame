using Identifiables;
using Identifiables.UI;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.UIElements;

[UpdateInGroup(typeof(IdentifiablesMappingGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Default)]
public abstract partial class IdentifiableMappingSystem : SystemBase
{
    public struct IdentifiableLink
    {
        public Identifieable To;
        public string Relation;
    }
    
    protected Dictionary<Identifieable, List<IdentifiableLink>> Relations = new();
    
    protected void AddRelation(Identifieable from, IdentifiableLink link)
    {
        if(!Relations.ContainsKey(from))
            Relations.Add(from, new List<IdentifiableLink>());
        Relations[from].Add(link);
    }

    public virtual VisualElement ProvideWikiDetails(Identifieable id)
    {
        if(!Relations.TryGetValue(id, out var relations))
            return null;
        var perRelationLookup = new Dictionary<string, VisualElement>();
        var details = new VisualElement();
        foreach (var relation in relations)
        {
            if (!perRelationLookup.TryGetValue(relation.Relation, out var foldout))
            {
                foldout = new Foldout{name = relation.Relation, text = relation.Relation};
                perRelationLookup[relation.Relation] = foldout;
                details.Add(foldout);
            }
            foldout.Add(new WikiSearchResultButton(relation.To));
        }
        return details;
    }
}