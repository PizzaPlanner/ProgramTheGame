using Core;
using Grids;
using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[BurstCompile]
[WithNone(typeof(Ghost))]
[WithNone(typeof(Building))]
partial struct MassesUpdateJobUnits : IJobEntity
{
    public ComponentLookup<MassModel> massModelLookup;
    public EntityCommandBuffer ecb;
    
    [BurstCompile]
    void Execute(Entity e, in OnGrid onGrid, in LocalTransform transform, ref MassData massData)
    {
        if(!massModelLookup.TryGetComponent(onGrid.Grid, out var massModel))
            return;
        var localToCell = CGrid.LocalToCell(transform.Position);
        // Only happens on init, better way to wor around this?
        if(!massModel.IsInBounds(localToCell))
            return;
        ref var mainCell = ref massModel.AbsoluteCellRW(localToCell);
        
        // Dynamic Entities, exchange with surrounding atmosphere
        IMatterComposition.ExchangeTemperature(ref mainCell, ref massData);
        
        // Phase Change
        var temp = massData.Temperature;
        if(temp > massData.ElementData.StableTemperatureRangeKelvin.x 
           && temp < massData.ElementData.StableTemperatureRangeKelvin.y)
            return;

        ref var reactions = ref massData.ElementType.Def.Reactions;
        for (int i = 0; i < reactions.Length; i++)
        {
            if(reactions[i].With != default)
                continue;
            if ((reactions[i].CheckBelow && temp < reactions[i].At) || (!reactions[i].CheckBelow && temp > reactions[i].At)) 
            {
                ecb.DestroyEntity(e);
                mainCell.Add(MassData.From(reactions[i].To, massData.Mass, massData.Energy));
            }
        }
    }
}

[BurstCompile]
[WithNone(typeof(Ghost))]
[WithNone(typeof(Wall))]
[WithAll(typeof(Building))]
partial struct MassesUpdateJobBuildings : IJobEntity
{
    public ComponentLookup<MassModel> massModelLookup;
    public EntityCommandBuffer ecb;
    
    [BurstCompile]
    void Execute(Entity e, in LocalTransform transform, in BakedIntoRoomMap baked, ref MassData massData)
    {
        if(!massModelLookup.TryGetComponent(baked.Grid, out var massModel))
            return;
        
        // Exchange Temperature
        for (int x = baked.Area.Min.x; x <= baked.Area.Max.x; x++)
        for (int y = baked.Area.Min.y; y <= baked.Area.Max.y; y++)
        {
            ref var inCell = ref massModel.AbsoluteCellRW(new int2(x, y));
            IMatterComposition.ExchangeTemperature(ref inCell, ref massData);
        }
        
        // Phase Change
        var temp = massData.Temperature;
        if(temp > massData.ElementData.StableTemperatureRangeKelvin.x 
           && temp < massData.ElementData.StableTemperatureRangeKelvin.y)
            return;

        ref var reactions = ref massData.ElementType.Def.Reactions;
        for (int i = 0; i < reactions.Length; i++)
        {
            if(reactions[i].With != default)
                continue;
            if ((reactions[i].CheckBelow && temp < reactions[i].At) || (!reactions[i].CheckBelow && temp > reactions[i].At)) 
            {
                ecb.DestroyEntity(e);
                var localToCell = CGrid.LocalToCell(transform.Position);
                ref var mainCell = ref massModel.AbsoluteCellRW(localToCell);
                mainCell.Add(MassData.From(reactions[i].To, massData.Mass, massData.Energy));
                break;
            }
        }
    }
}

[BurstCompile]
[WithNone(typeof(Ghost))]
[WithAll(typeof(MassData))]
[WithAll(typeof(Wall))]
partial struct MassesUpdateJobWall : IJobEntity
{
    public ComponentLookup<MassModel> massModelLookup;
    public ComponentLookup<MassData> massDataLookup;
    public EntityCommandBuffer ecb;
    
    [BurstCompile]
    void Execute(Entity e, in LocalTransform transform, in DynamicBuffer<Neighbours> neighbours, in BakedIntoRoomMap baked)
    {
        if(!massModelLookup.TryGetComponent(baked.Grid, out var massModel))
            return;
        ref var massData = ref massDataLookup.GetRefRW(e).ValueRW;
        
        foreach (var nb in neighbours)
        {
            // Only update left to right, so pairs exchange only once per update
            if(math.any(nb.Direction < 0) || !massDataLookup.HasComponent(nb.Other))
                continue;
                    
            ref var other = ref massDataLookup.GetRefRW(nb.Other).ValueRW;
            IMatterComposition.ExchangeTemperature(ref massData, ref other);
        }
        
        var bounds = baked.Area;
        for (int x = bounds.Min.x; x <= bounds.Max.x; x++)
        {
            ref var top = ref massModel.AbsoluteCellRW(new int2(x, bounds.Max.y + 1));
            ref var bottom = ref massModel.AbsoluteCellRW(new int2(x, bounds.Min.y - 1));
            IMatterComposition.ExchangeTemperature(ref top, ref massData);
            IMatterComposition.ExchangeTemperature(ref bottom, ref massData);
        }
        for (int y = bounds.Min.y; y <= bounds.Max.y; y++)
        {
            ref var left = ref massModel.AbsoluteCellRW(new int2(bounds.Min.x - 1, y));
            ref var right = ref massModel.AbsoluteCellRW(new int2(bounds.Max.x + 1, y));
            IMatterComposition.ExchangeTemperature(ref left, ref massData);
            IMatterComposition.ExchangeTemperature(ref right, ref massData);
        }
        
        // Phase Change
        var temp = massData.Temperature;
        if(temp > massData.ElementData.StableTemperatureRangeKelvin.x 
           && temp < massData.ElementData.StableTemperatureRangeKelvin.y)
            return;

        ref var reactions = ref massData.ElementType.Def.Reactions;
        for (int i = 0; i < reactions.Length; i++)
        {
            if(reactions[i].With != default)
                continue;
            if ((reactions[i].CheckBelow && temp < reactions[i].At) || (!reactions[i].CheckBelow && temp > reactions[i].At)) 
            {
                ecb.DestroyEntity(e);
                var localToCell = CGrid.LocalToCell(transform.Position);
                ref var mainCell = ref massModel.AbsoluteCellRW(localToCell);
                mainCell.Add(MassData.From(reactions[i].To, massData.Mass, massData.Energy));
                break;
            }
        }
    }
}

[UpdateInGroup(typeof(TickBasedSystemGroup))]
public partial struct MassUpdateSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
                           .CreateCommandBuffer(state.WorldUnmanaged);
        
        foreach (var model in SystemAPI.Query<RefRW<MassModel>>())
        {
            state.Dependency = JobHandle.CombineDependencies(state.Dependency, model.ValueRW.RunMixJob(state.Dependency));
        }
        
        state.Dependency = new MassesUpdateJobWall()
        {
            ecb = ecb,
            massModelLookup = SystemAPI.GetComponentLookup<MassModel>(false),
            massDataLookup = SystemAPI.GetComponentLookup<MassData>(false)
        }.Schedule(state.Dependency);
        
        state.Dependency = new MassesUpdateJobBuildings()
        {
            ecb = ecb,
            massModelLookup = SystemAPI.GetComponentLookup<MassModel>(false),
        }.Schedule(state.Dependency);
        
        state.Dependency = new MassesUpdateJobUnits()
        {
            ecb = ecb,
            massModelLookup = SystemAPI.GetComponentLookup<MassModel>(false),
        }.Schedule(state.Dependency);
    }
}