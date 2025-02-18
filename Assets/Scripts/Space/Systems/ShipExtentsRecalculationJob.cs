using Grids;
using Navigation;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Space.Systems
{
    [UpdateInGroup(typeof(GridUpdateSystemGroup))]
    partial struct ShipExtentsRecalculationSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            new ShipExtentsRecalculationJob()
            {
                navLookup = SystemAPI.GetComponentLookup<NavTree>(true)
            }.Run();
        }
    }
    
    [BurstCompile]
    public partial struct ShipExtentsRecalculationJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<NavTree> navLookup;
        
        [BurstCompile]
        void Execute(in BakedIntoRoomMap baked, in ShipController controller, DynamicBuffer<BelongsToShip> onShip)
        {
            if(!navLookup.TryGetComponent(baked.Grid, out var grid))
               return;
            
            onShip.Clear();
            var connectedCells = new NativeList<int2>(Allocator.Temp);
            grid.GetAllConnectedCells(baked.Area.Min, ref connectedCells);
            onShip.Reinterpret<int2>().AddRange(connectedCells.AsArray());
        }
    }
}