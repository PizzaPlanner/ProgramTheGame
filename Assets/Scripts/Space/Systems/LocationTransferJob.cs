using Grids;
using Navigation;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Transforms;

namespace Space.Systems
{
    partial struct LocationTransferJob : IJobEntity
    {
        public EntityCommandBuffer ecb;
        public ComponentLookup<CGrid> gridLookup;
        public ComponentLookup<NavTree> navTreeLookup;
        public ComponentLookup<MassModel> massLookup;
        [NativeDisableContainerSafetyRestriction]
        [NativeDisableParallelForRestriction]
        public ComponentLookup<RoomModel> roomLookup;
        
        void Execute(Entity e, in TransferToLocation toLocation, ref OnGrid onGrid, in BakedIntoRoomMap baked)
        {
            BuildUtils.Cleanup(e, baked, ecb, gridLookup, navTreeLookup, massLookup, roomLookup);
            onGrid.Grid = toLocation.TargetLocation;
            ecb.RemoveComponent<TransferToLocation>(e);
            ecb.SetComponent(e, new Parent { Value = toLocation.TargetLocation });
            ecb.AddComponent(e, new ToBeBakedIntoRoomMap { Value = 
            {
                Area = baked.Area.WithOffset(toLocation.DeltaOffset),
                Grid = toLocation.TargetLocation,
                Layer = baked.Layer,
                Rotation = baked.Rotation,
                Offset = baked.Offset
            }});
        }
    }
}