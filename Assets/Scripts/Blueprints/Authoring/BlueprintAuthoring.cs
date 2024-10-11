using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Content;
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
            if(authoring.DefinitionYaml != null)
                AddComponent(e, new BlueprintYaml(){ YamlText = authoring.DefinitionYaml });
            AddBuffer<BlueprintContents>(e);
            AddComponent<Blueprint>(e);
        }
    }
}

[WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
partial struct BlueprintBakingSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (yaml, e) in SystemAPI.Query<BlueprintYaml>()
                                           .WithAll<Blueprint, BlueprintContents>()
                                           .WithOptions(EntityQueryOptions.IncludePrefab)
                                           .WithEntityAccess())
        {
            if (BlueprintUtils.DeserializeInto(yaml.YamlText.Value.text, state.EntityManager, e)) 
                continue;
            
            Debug.LogError($"Could not deserialize blueprint {yaml.YamlText.Value.name}");
            ecb.DestroyEntity(e);
        }
    }
}