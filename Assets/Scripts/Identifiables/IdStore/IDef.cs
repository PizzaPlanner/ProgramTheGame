using System;
using System.Reflection;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.LowLevel.Unsafe;
using Unity.Transforms;
using UnityEngine;

namespace Identifiables
{
    public interface IDefRef<T> : IComponentData where T: unmanaged, IDef
    {
        public BlobAssetReference<T> Def { get; }
    }

    public interface IDef { }
}