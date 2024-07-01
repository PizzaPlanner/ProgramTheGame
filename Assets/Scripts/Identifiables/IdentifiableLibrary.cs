using Lookups;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Identifiables
{
    [UpdateInGroup(typeof(IdentifiablesMappingGroup), OrderFirst = true)]
    public partial class IdentifiableLibrary : LibraryAssetGuidBased<IdentifiableData>
    {
        protected override ulong GuidFromLookup(IdentifiableData lookup) => lookup.AssetGuid;

        protected override void OnUpdate()
        {
            base.OnUpdate();
            Enabled = true;
        }

        public static Id Find(ulong guid)
        {
            return Id.From(FindId(guid));
        }
    }
    
    public struct IdentifiableData
    {
        public Entity SpawnablePrefab;
        public ulong AssetGuid;
        public UnityObjectRef<Texture2D> Icon;
        public FixedString64Bytes DefaultName;
    }
}