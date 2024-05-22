using Unity.Entities;
using UnityEngine;

partial class ChipCompilationSystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (chip, instructions, state, e) in SystemAPI.Query<LogicChip, DynamicBuffer<ChipInstructions>, RefRW<ChipState>>()
                                                           .WithAll<JustChangedLocation, Equipped>().WithEntityAccess())
        {
            if (!chip.ManagedScript.Value.Compile(e, ref CheckedStateRef))
            {
                Debug.LogError($"Failed script compilation");
                continue;
            }
            state.ValueRW = default;
        }
    }
}