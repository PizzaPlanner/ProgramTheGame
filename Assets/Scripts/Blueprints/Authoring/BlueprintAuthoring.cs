using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[BakingType]
struct BlueprintYaml : IComponentData
{
    public UnityObjectRef<TextAsset> YamlText;    
}

public class BlueprintAuthoring : MonoBehaviour
{
    public TextAsset DefinitionYaml;

    class BPBaker : Baker<BlueprintAuthoring>
    {
        public override void Bake(BlueprintAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);
            var bpBlob = BlueprintUtils.BuildString(authoring.DefinitionYaml.text);
            AddComponent(e, new Blueprint
            {
                Data = bpBlob
            });
            AddBlobAsset(ref bpBlob, out _);
            
        }
    }
}