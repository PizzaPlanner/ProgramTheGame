using Unity.Collections;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using UnityEngine;

namespace Space.Authoring
{
    public class MapGenNodeBlueprintAuthoring : MapGenerationNodeAuthoring<SpawnBlueprint>
    {
        public TextAsset Blueprint;
        
        protected override BlobAssetReference<SpawnBlueprint> Data => makeData();

        BlobAssetReference<SpawnBlueprint> makeData()
        {
            if(!BlueprintUtils.DeserializeInto(Blueprint.text, Allocator.Persistent, out var data))
                Debug.LogError($"Invalid Blueprint on {gameObject.name}", gameObject);
            return UnsafeUntypedBlobAssetReference.Create(data).Reinterpret<SpawnBlueprint>();
        }
        
        class Baker : MapGenBaker<MapGenNodeBlueprintAuthoring> { }
    }
}