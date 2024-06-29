using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Lookups
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public abstract partial class LibraryAssetBase : SystemBase
    {
        protected static NativeArray<T> FromBlob<T>(ref BlobArray<T> blob)
            where T : unmanaged
        {
            ref var rewards = ref blob;
            var array = new NativeArray<T>(rewards.Length, Allocator.Temp);
            for (int i = 0; i < array.Length; i++)
                array[i] = rewards[i];
            return array;
        }
    }


    public abstract partial class LibraryAsset<TLookup> : LibraryAssetBase
        where TLookup : unmanaged
    {
        public struct TLibrary
        {
            public BlobArray<TLookup> Data;
        }

        public struct TComponent : IComponentData
        {
            public BlobAssetReference<TLibrary> Data;
        }

        public Hash128 Hash { get; private set; }

        protected static ref TLookup Get(ushort id) => ref Library.Data.Value.Data[id];

        protected static ref TLookup Get(WorldUnmanaged world, ushort id)
        {
#if UNITY_EDITOR
            return ref LibraryPerWorldLookup.Data[world.EntityManager].Value.Data[id];
#else
            return ref Get(id);
#endif
        }

        public static ushort Length => (ushort)Library.Data.Value.Data.Length;

        public static ushort LengthIn(WorldUnmanaged world)
        {
#if UNITY_EDITOR
            return (ushort)LibraryPerWorldLookup.Data[world.EntityManager].Value.Data.Length;
#else
            return Length;
#endif
        }

        internal static TComponent MakeComponent(BlobAssetReference<TLibrary> data)
        {
            return new TComponent { Data = data };
        }

        // NOTE: This variable is NOT available during baking!
        // Not even if LoadLibraryFromAsset etc. is called in the baker that is using it.
        // The Authoring data must be used instead
        static readonly SharedStatic<BlobAssetReference<TLibrary>> Library =
            SharedStatic<BlobAssetReference<TLibrary>>
                .GetOrCreate<TLibrary>();
        
#if UNITY_EDITOR
        static readonly SharedStatic<NativeHashMap<EntityManager, BlobAssetReference<TLibrary>>> LibraryPerWorldLookup =
            SharedStatic<NativeHashMap<EntityManager, BlobAssetReference<TLibrary>>>
                .GetOrCreate<TLookup>();
#endif

        protected virtual Action OnLoaded { get; }

        protected virtual bool IncludeWorldSpecificData(ref BlobAssetReference<TLibrary> blobAsset)
        {
            return true;
        }
        
        public static TLookup[] Data => Library.Data.IsCreated? Library.Data.Value.Data.ToArray() : Array.Empty<TLookup>();

        protected override void OnDestroy()
        {
            if(Library.Data.IsCreated)
                Library.Data.Dispose();
#if UNITY_EDITOR
            if (LibraryPerWorldLookup.Data.IsCreated)
                LibraryPerWorldLookup.Data.Dispose();
#endif
        }

        protected override void OnUpdate()
        {
            if (!SystemAPI.TryGetSingleton<TComponent>(out var asset))
                return;
            if (!IncludeWorldSpecificData(ref asset.Data))
                return;

            Library.Data = asset.Data;

#if UNITY_EDITOR
            if (!LibraryPerWorldLookup.Data.IsCreated)
                LibraryPerWorldLookup.Data =
                    new NativeHashMap<EntityManager, BlobAssetReference<TLibrary>>(0, Allocator.Persistent);
            LibraryPerWorldLookup.Data[World.Unmanaged.EntityManager] = asset.Data;
#endif


            Enabled = false;
            OnLoaded?.Invoke();
        }
        
        public static BlobBuilder MakeBuilder(int elements, out BlobBuilderArray<TLookup> arrayBuilder)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref builder.ConstructRoot<TLibrary>();
            arrayBuilder = builder.Allocate(ref blob.Data, elements);
            return builder;
        }
    }
}