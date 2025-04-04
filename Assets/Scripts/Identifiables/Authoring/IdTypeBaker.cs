#if UNITY_EDITOR
using Identifiables;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

public abstract class IdTypeBaker<TAuth> : Baker<TAuth> where TAuth : MonoBehaviour
{
    public Identifiable Id => GetComponent<IdentifieableAuthoring>().Id;
    public unsafe Hash128 HashForIdBlob<T>() 
        where T : unmanaged, IComponentData
    {
        var typehash = TypeManager.GetFullNameHash(TypeManager.GetTypeIndex<T>());
        var input = new int4(Id.GetHashCode(), 0, 0, 0);
        UnsafeUtility.WriteArrayElement(UnsafeUtility.AddressOf(ref input), 1, typehash);
        var customHash = new Hash128(math.hashwide(input));
        return customHash;
    }

    public bool TryGetBlobDef<TComp, TDef>(out BlobAssetReference<TDef> blob, out Hash128 hash)
        where TComp: unmanaged, IDefRef<TDef>
        where TDef: unmanaged, IDef
    {
        hash = HashForIdBlob<TComp>();
        return TryGetBlobAssetReference(hash, out blob);
    }
}
#endif