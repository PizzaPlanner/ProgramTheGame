using System;
using Unity.Entities;
using UnityEngine;

public class BlueprintSpawnerAuthoring : MonoBehaviour
{
    public BlueprintAuthoring Blueprint;

    class SpawnerBaker : Baker<BlueprintSpawnerAuthoring>
    {
        public override void Bake(BlueprintSpawnerAuthoring authoring)
        {
            var parent = GetEntity(GetComponentInParent<GridStructureAuthoring>(), TransformUsageFlags.None);
            if(parent == default)
                throw new Exception($"{nameof(BlueprintSpawnerAuthoring)} has no parent with {nameof(GridStructureAuthoring)}");
            
            var e = GetEntity(TransformUsageFlags.None);
            var offset = GridStructure.LocalToCell(authoring.transform.position);
            AddComponent(e, new Blueprint{BuildOn = parent, Offset = offset});
            AddBuffer<BlueprintContents>(e);
            AddComponent(e, new BlueprintYaml(){ YamlText = authoring.Blueprint.DefinitionYaml });
        }
    }
}