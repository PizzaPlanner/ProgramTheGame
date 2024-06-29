using Lookups;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public abstract class LibraryAssetAuthoring<TDefinition, TBakerElement> : MonoBehaviour
    where TDefinition : unmanaged
{
    public TBakerElement[] Elements;

    public bool TryGet(TBakerElement bakerElement, out ushort id)
    {
        var result = Array.IndexOf(Elements, bakerElement);
        id = (ushort)(result + 1);
        return result >= 0;
    }

    public ushort IdOf(TBakerElement bakerElement)
    {
        if (!TryGet(bakerElement, out var result))
            throw new KeyNotFoundException($"Not part of a loaded {GetType().Name}: {bakerElement.ToString()}");
        return result;
    }

    public void Bake(IBaker baker)
    {
        var blob = ToBlobData(Allocator.Persistent);
        baker.AddBlobAsset(ref blob, out _);
        var e = baker.GetEntity(TransformUsageFlags.None);
        baker.AddComponent(e, LibraryAsset<TDefinition>.MakeComponent(blob));
    }

    internal BlobAssetReference<LibraryAsset<TDefinition>.TLibrary> ToBlobData(Allocator allocator)
    {
        var builder = LibraryAsset<TDefinition>.MakeBuilder(Elements.Length + 1, out var arrayBuilder);
        arrayBuilder[0] = default;
        for (int i = 0; i < Elements.Length; i++)
            BakeElement(Elements[i], i + 1, arrayBuilder, builder);
        return builder.CreateBlobAssetReference<LibraryAsset<TDefinition>.TLibrary>(allocator);
    }

    protected abstract void BakeElement(TBakerElement element, int builderIdx,
                                        BlobBuilderArray<TDefinition> builderArray, BlobBuilder builder);
}