using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class BlueprintAuthoring : MonoBehaviour
{
    public TextAsset DefinitionYaml;

    class BPBaker : Baker<BlueprintAuthoring>
    {
        public override void Bake(BlueprintAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);
            if(!BlueprintUtils.DeserializeInto(authoring.DefinitionYaml.text, Allocator.Persistent, out var bpBlob))
                return;
            AddComponent(e, new Blueprint
            {
                Data = bpBlob
            });
            AddBlobAsset(ref bpBlob, out _);
            
        }
    }
}