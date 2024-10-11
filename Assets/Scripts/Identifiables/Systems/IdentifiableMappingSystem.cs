using Identifiables;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

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

    protected void TryRegisterDynamicPrefab(Entity prefab, FixedString64Bytes name, IEnumerable<Identifiable> tags)
    {
        var preId = new IdentifiableData
        {
            DefaultName = name,
            SpawnablePrefab = prefab
        };
        if (World.GetExistingSystemManaged<IdentifiablesMappingGroup>().RegisterAsIdentifiable(prefab, preId)) 
            return;
        
        Debug.LogError($"Failed to register {prefab} as {preId.DefaultName}");
        EntityManager.DestroyEntity(prefab);
    }
}