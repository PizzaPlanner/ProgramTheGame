using Unity.Collections;
using Unity.Entities;
using UnityEngine;

partial class ChipCompilationSystem : SystemBase
{
    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (chip, state, id, e) in SystemAPI.Query<LogicChip, RefRW<ChipState>, Identifieable>()
                                                           .WithNone<ChipInstructions>().WithEntityAccess())
        {
            var error = ChipScriptParser.GetFrom(chip.ManagedScript.Value, id, out var results);
            if (error != ChipScriptParser.Error.NoError)
                continue;
        
            var instructions = ecb.AddBuffer<ChipInstructions>(e);
            instructions.AddRange(results);
            state.ValueRW = default;
        }
        ecb.Playback(EntityManager);
    }
}