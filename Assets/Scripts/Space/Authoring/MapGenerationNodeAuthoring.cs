using Identifiables;
using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
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
    public abstract class MapGenerationNodeAuthoring<T2> : MapGenAuthoringBase where T2: IMapGenerationNode
    {
        protected abstract T2 Data { get; }
        protected abstract class MapGenBaker<T, TB> : Baker<T> 
            where T : MapGenerationNodeAuthoring<TB>
            where TB: IMapGenerationNode
        {
            public override void Bake(T authoring)
            {
                var data = authoring.Data;
                var e = GetEntity(authoring, TransformUsageFlags.None);
                AddComponent(e,  new NodeSerializer
                {
                    Data = UnsafeUtility.As<TB, FixedBytes510>(ref data), 
                    TypeHash = TypeManager.GetTypeInfo<T2>().StableTypeHash // https://discussions.unity.com/t/entities-1-3-9-stabletypehash-broken/1594777
                });
            }
        }
    }
}