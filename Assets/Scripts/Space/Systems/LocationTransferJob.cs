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
        public BuildUtils.AllLookups lookups;
        
        void Execute(Entity e, in TransferToLocation toLocation, ref OnGrid onGrid, in BakedIntoRoomMap baked)
        {
            BuildUtils.Cleanup(e, baked, ecb, lookups);
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