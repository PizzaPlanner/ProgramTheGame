using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

partial struct ChipMemoryJob : IJobEntity
{
    public long CurrentTick;
    [ReadOnly] public BufferLookup<Inventory> InventoryLookup;
    public BufferLookup<ChipAccessibleData> LogicLookup;
    
    unsafe void Execute(Entity e, DynamicBuffer<ChipInstructions> cd, ref ChipState state, in Equipped equip, in Identifieable id)
    {
        if(cd.Length == 0)
            return;
        
        if(!LogicLookup.TryGetBuffer(e, out var chipData) || !chipData.Locate<ChipState>(id, out var data))
            return;
        
        ChipState.ApplyReadData(state, ref *data);
        
        var opsThisTick = 0;
        while (data->NextTick <= CurrentTick && opsThisTick++ < 10)
        {
            var line = cd[data->NextLine];
            
            var result = line.RightIsConstant ? line.Right.TypeParameterIndex : ReadFrom<int>(equip.Wearer, line.Right);
            
            if (line.WritesToSelf)
            {
                WriteToBuffer(chipData, new VariableAccess{ TypeId = id, TypeParameterIndex = line.Left.TypeParameterIndex }, result);
            }
            else 
            {
                data->NextTick++;
                WriteTo(equip.Wearer, line.Left, result);
            }
            
            HandleSelf(ref *data);
            data->NextLine %= cd.Length;
            if(data->NextLine == 0)
                break;
        }
        state = *data;
    }

    unsafe T ReadFrom<T>(Entity wearer, VariableAccess variable) where T: unmanaged
    {
        if(!FindEquipmentForType(wearer, variable.TypeId, out var equipData))
            return default;
        if(!ItemTypeLogicParametersMappingSystem.TryGetLogicParameter(variable.TypeId, variable.TypeParameterIndex, out var p1))
            return default;
        
        var location = (byte*)equipData.GetUnsafePtr() + p1.Offset;
        return *(T*)location;
    }
    
    unsafe void WriteTo<T>(Entity wearer, VariableAccess variable, T data) where T: unmanaged
    {
        if(!FindEquipmentForType(wearer, variable.TypeId, out var chipData))
            return;

        WriteToBuffer(chipData, variable, data);
    }

    unsafe void WriteToBuffer<T>(DynamicBuffer<ChipAccessibleData> buffer, VariableAccess variable, T data)
        where T : unmanaged
    {
        if (!ItemTypeLogicParametersMappingSystem.TryGetLogicParameter(variable.TypeId, variable.TypeParameterIndex,
                out var p1))
        {
            Debug.LogError($"Not a Property: {variable.TypeParameterIndex} on {variable.TypeId}");
            return;
        }

        if (p1.Access == LogicAccessType.Read)
        {
            Debug.LogError($"Cannot write ReadOnly Property {variable.TypeId}.{p1.Name}");
            return;
        }
        
        var location = (byte*)buffer.GetUnsafePtr() + p1.Offset;
        UnsafeUtility.CopyStructureToPtr(ref data, location);
    }

    bool FindEquipmentForType(Entity wearer, Identifieable id, out DynamicBuffer<ChipAccessibleData> data)
    {
        data = default;
        InventoryLookup.TryGetBuffer(wearer, out var inv);
        foreach (var item in inv)
        {
           if(!item.IsEquipmentSlot || item.Entity == Entity.Null || item.Type.BaseId != id)
               continue;
           if(LogicLookup.TryGetBuffer(item.Entity, out data))
               return true;
        }
        return false;
    }

    void HandleSelf(ref ChipState state)
    {
        state.OpResult = state.Operator switch
        {
            ChipOperation.Add => state.OpLhs + state.OpRhs,
            ChipOperation.Sub => state.OpLhs - state.OpRhs,
            ChipOperation.Mul => state.OpLhs * state.OpRhs,
            ChipOperation.Div => state.OpLhs / state.OpRhs,
            ChipOperation.Mod => state.OpLhs % state.OpRhs,
            ChipOperation.Equal => math.select(0, 1, state.OpLhs == state.OpRhs),
            ChipOperation.NotEqual => math.select(0, 1, state.OpLhs != state.OpRhs),
            ChipOperation.Less => math.select(0, 1, state.OpLhs < state.OpRhs),
            ChipOperation.LessEqual => math.select(0, 1, state.OpLhs <= state.OpRhs),
            ChipOperation.Greater => math.select(0, 1, state.OpLhs > state.OpRhs),
            ChipOperation.GreaterEqual => math.select(0, 1, state.OpLhs >= state.OpRhs),
            _ => state.OpResult
        };
        
        if(state.Operator == ChipOperation.Jump)
            state.NextLine = state.OpLhs;
        else state.NextLine++;

        state.Operator = ChipOperation.NoOp;
    }
}