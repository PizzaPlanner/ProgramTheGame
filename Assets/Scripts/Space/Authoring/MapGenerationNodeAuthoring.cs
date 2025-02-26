using Identifiables;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using UnityEngine;

namespace Space.Authoring
{
    [Serializable]
    public class WeightedChild
    {
        public IdentifieableAuthoring Child;
        public float Weight;
    }
    public class MapGenAuthoringBase : MonoBehaviour { }
    public abstract class MapGenerationNodeAuthoring<T2> : MapGenAuthoringBase where T2: unmanaged, IMapGenerationNode
    {
        protected abstract BlobAssetReference<T2> Data { get; }
        protected abstract class MapGenBaker<T> : Baker<T> 
            where T : MapGenerationNodeAuthoring<T2>
            // where TB: unmanaged, IMapGenerationNode
        {
            public override void Bake(T authoring)
            {
                var data = authoring.Data;
                var e = GetEntity(authoring, TransformUsageFlags.None);
                AddComponent(e,  new NodeSerializer
                {
                    Data = UnsafeUntypedBlobAssetReference.Create(data), 
                    TypeHash = TypeManager.GetTypeInfo<T2>().StableTypeHash // https://discussions.unity.com/t/entities-1-3-9-stabletypehash-broken/1594777
                });
                AddBlobAsset(ref data, out _);
            }
        }
    }
}