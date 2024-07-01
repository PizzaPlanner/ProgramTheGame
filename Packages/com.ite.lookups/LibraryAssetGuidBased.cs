using Unity.Burst;
using Unity.Collections;

namespace Lookups
{
    public abstract partial class LibraryAssetGuidBased<TLookup> : LibraryAsset<TLookup>
        where TLookup : unmanaged
    {
        static readonly SharedStatic<NativeHashMap<ulong, ushort>> GuidToIdMap =
            SharedStatic<NativeHashMap<ulong, ushort>>
                .GetOrCreate<LibraryAssetGuidBased<TLookup>, ulong>();

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if(Enabled)
                return;
            
            GuidToIdMap.Data = new NativeHashMap<ulong, ushort>(Length, Allocator.Persistent);
            for (var i = (ushort)0; i < Data.Length; i++)
            {
                var id = Data[i];
                GuidToIdMap.Data.Add(GuidFromLookup(id), i);
            }
        }
        
        protected abstract ulong GuidFromLookup(TLookup lookup);

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if(GuidToIdMap.Data.IsCreated)
                GuidToIdMap.Data.Dispose();
        }

        protected static ushort FindId(ulong guid)
        {
            if(!GuidToIdMap.Data.IsCreated)
                return default;
            return GuidToIdMap.Data.TryGetValue(guid, out var id) ? id : default;
        }
    }
}