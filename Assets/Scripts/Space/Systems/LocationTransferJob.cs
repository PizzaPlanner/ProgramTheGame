using Navigation;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

namespace Space.Systems
{
    partial struct LocationTransferJob : IJobEntity
    {
        public EntityCommandBuffer ecb;
        public ComponentLookup<NavLayers> navLayersLookup;
        public ComponentLookup<NavTree> navTreeLookup;
        public ComponentLookup<MassModel> massLookup;
        [NativeDisableContainerSafetyRestriction]
        [NativeDisableParallelForRestriction]
        public ComponentLookup<RoomModel> roomLookup;
        
        void Execute(Entity e, in TransferToLocation toLocation, ref OnGrid onGrid, in BakedIntoRoomMap baked)
        {
            BuildUtils.Cleanup(e, baked, ecb, navLayersLookup, navTreeLookup, massLookup, roomLookup);
            onGrid.Grid = toLocation.TargetLocation;
            ecb.RemoveComponent<TransferToLocation>(e);
            ecb.SetComponent(e, new Parent { Value = toLocation.TargetLocation });
            ecb.AddComponent(e, new ToBeBakedIntoRoomMap
            {
                Area = baked.Area.WithOffset(toLocation.DeltaOffset),
                Grid = toLocation.TargetLocation,
                Layer = baked.Layer,
                Rotation = baked.Rotation
            });
        }
    }
}