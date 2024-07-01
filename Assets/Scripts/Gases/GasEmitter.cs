using Unity.Entities;
using Unity.Transforms;

public struct GasEmitter : IComponentData
{
    public GasVolume Volume;
    public bool IsExhaustive;
}

partial struct GasEmitterSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (emitter, parent, bounds) in SystemAPI.Query<RefRW<GasEmitter>, Parent, GridBounds>()
                                                           .WithNone<UnderConstruction>())
        {
            if(!SystemAPI.HasComponent<GasModel>(parent.Value))
                continue;
            var gasModel = SystemAPI.GetComponent<GasModel>(parent.Value);
            var toCell = GridStructure.LocalToCell(bounds.Center);

            var atmosphere = gasModel.TileMap.TryGetValue(toCell, out var n) ? n : GasVolume.Empty;
            if (emitter.ValueRW.IsExhaustive)
            {
                var (myGas, nb, changed) = GasVolume.Mix(emitter.ValueRW.Volume, atmosphere);
                if (!changed) 
                    continue;
                
                gasModel.TileMap[toCell] = nb;
                emitter.ValueRW.Volume = myGas;
            }
            else
            {
                gasModel.TileMap[toCell] = atmosphere.Add(emitter.ValueRW.Volume);
            }
            gasModel.Changes.Add(toCell);
        }
    }
}