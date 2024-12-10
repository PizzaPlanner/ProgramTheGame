using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace Power
{
    [UpdateInGroup(typeof(GridUpdateSystemGroup))]
    public partial struct PowerGridRecalculationSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            var cableLookup = SystemAPI.GetComponentLookup<PowerCable>(false);
            var visitedCables = new NativeHashSet<Entity>(16, Allocator.Temp);
            var cableQuery = SystemAPI.QueryBuilder().WithAll<PowerCable, Neighbours>().Build();
            foreach(var e in cableQuery.ToEntityArray(Allocator.Temp))
            {
                if(visitedCables.Contains(e))
                    continue;
                
                var connectedGroup = new NativeHashSet<Entity>(8, Allocator.Temp);
                var toVisit = new NativeQueue<Entity>(Allocator.Temp);
                toVisit.Enqueue(e);
                var bestGrid = Entity.Null;
                while (toVisit.TryDequeue(out var current))
                {
                    if(connectedGroup.Contains(current))
                        continue;
                    visitedCables.Add(current);
                    connectedGroup.Add(current);
                    foreach(var nb in SystemAPI.GetBuffer<Neighbours>(current))
                        toVisit.Enqueue(nb.Other);
                    var cable = cableLookup[current];
                    if(cable.Network == Entity.Null)
                        continue;
                    if(bestGrid == Entity.Null)
                        bestGrid = cable.Network;
                    bestGrid = cable.Network.Index < bestGrid.Index ? cable.Network : bestGrid;
                }

                if (bestGrid == Entity.Null)
                {
                    bestGrid = state.EntityManager.CreateEntity();
                    state.EntityManager.AddComponentData(bestGrid, PowerNetwork.Next);
                    cableLookup.Update(ref state);
                }
                foreach (var c in connectedGroup)
                    cableLookup[c] = new PowerCable { Network = bestGrid };
            }
            
            var powerGrids = new NativeParallelHashMap<Entity, int>(0, Allocator.TempJob);
            foreach(var network in SystemAPI.QueryBuilder().WithAll<PowerNetwork>().Build().ToEntityArray(Allocator.Temp))
                powerGrids[network] = 0;
            foreach(var cable in cableQuery.ToComponentDataArray<PowerCable>(Allocator.Temp))
                powerGrids[cable.Network]++;
            foreach(var entry in powerGrids)
                if(entry.Value <= 0)
                    ecb.DestroyEntity(entry.Key);
        }
    }
}