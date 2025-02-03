using Identifiables;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public struct DynamicIdentifiableDefinition
{
    public Entity Prefab;
    public bool IsSpawnable;
    public UnityObjectRef<Texture2D> Icon;
    public FixedString64Bytes Name;
    public IEnumerable<FixedString64Bytes> Tags;
}

[UpdateInGroup(typeof(IdentifiablesMappingGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Default)]
public abstract partial class IdentifiableMappingSystem : SystemBase
{
    public struct IdentifiableLink
    {
        public Identifiable To;
        public string Relation;
    }

    public Dictionary<Identifiable, List<IdentifiableLink>> Relations = new();
    
    protected void AddRelation(Identifiable from, IdentifiableLink link)
    {
        if(!Relations.ContainsKey(from))
            Relations.Add(from, new List<IdentifiableLink>());
        Relations[from].Add(link);
    }

    public Func<IEnumerable<DynamicIdentifiableDefinition>> RegisterAdditionalIds { get; protected set; } 
}