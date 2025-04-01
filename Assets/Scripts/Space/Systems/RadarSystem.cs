using Grids;
using Identifiables;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Task = Units.Task;

namespace Space.Systems
{
    partial struct RadarSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            var galaxy = state.EntityManager.CreateSingleton(new Galaxy
            {
               Seed = Random.CreateFromIndex(1237819283).state,
               SectorMap = new (8, Allocator.Persistent)
            });
        }

        public void OnDestroy(ref SystemState state)
        {
            SystemAPI.GetSingleton<Galaxy>().SectorMap.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
            ref var galaxy = ref SystemAPI.GetSingletonRW<Galaxy>().ValueRW;
            // TODO: Set this properly in initialization
            if (galaxy.Generator == default(Identifiable))
            {
                if(GalaxyTypeDef.Id.All.Length == 0)
                    return;
                galaxy.Generator = GalaxyTypeDef.Id.All[0];
            }
            foreach(var (sector, e) in SystemAPI.Query<Sector>().WithChangeFilter<Sector>().WithNone<Task, ShipController>().WithEntityAccess())
                galaxy.SectorMap.TryAdd((int2)math.floor(sector.PositionInSpace), e);
            foreach (var (radar, onGrid) in SystemAPI.Query<Radar, OnGrid>().WithNone<Ghost>())
            {
                var inSector = SystemAPI.GetComponent<Sector>(onGrid.Grid);
                var min = (int2)math.round(inSector.PositionInSpace - radar.MaxRange);
                var max = (int2)math.round(inSector.PositionInSpace + radar.MaxRange);
                for (int x = min.x; x <= max.x; x++)
                for (int y = min.y; y <= max.y; y++)
                {
                    if(galaxy.SectorMap.ContainsKey(new int2(x,y)))
                        continue;
                    if(math.distance(new int2(x, y), inSector.PositionInSpace) > radar.MaxRange)
                        continue;
                    GenerateSector(ecb, galaxy, new int2(x,y));
                }
            }
        }

        void GenerateSector(EntityCommandBuffer ecb, Galaxy galaxy, int2 sector)
        {
            unchecked
            {
                var seed = math.hash(new int3((int)galaxy.Seed, sector.x, sector.y));   
                var random = new Random(seed);
                var mapType = random.NextFloat();
                var rootGen = galaxy.Generator.Def.SelectOne(mapType);
                var e = ecb.Instantiate(Core.ID.EmptyLocation);
                ecb.SetComponent(e, new Sector
                {
                    PositionInSpace = sector,
                    SolarIntensity = random.NextFloat(),
                    MainPOI = rootGen
                });
                ecb.AddComponent<Unvisited>(e);
                if(rootGen != default)
                    ecb.AddComponent(e, new MapGenerationSettings
                    {
                        RootGenerator = rootGen,
                        Seed = random.NextUInt()
                    });
            }
        }
    }
}