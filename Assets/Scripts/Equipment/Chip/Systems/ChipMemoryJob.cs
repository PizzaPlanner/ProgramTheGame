using Core;
using Programmable.EntitiesExtensions;
using System;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Assertions;

[BurstCompile]
unsafe partial struct ChipLogicJob : IJobChunk
{
    [NativeDisableContainerSafetyRestriction] 
    public NativeHashMap<TypeIndex, DynamicComponentTypeHandle> DynamicTypeHandles;
    
    [ReadOnly] public BufferLookup<Inventory> InventoryLookup;
    [ReadOnly] public ComponentTypeHandle<Equipped> equippedHandle;
    [ReadOnly] public ComponentTypeHandle<Identifieable> idHandle;
    [ReadOnly] public BufferTypeHandle<ChipInstructions> instructionsHandle;
    public ComponentTypeHandle<ChipState> chipStateHandle;
    public EntityStorageInfoLookup EntityLookup;
    public EntityTypeHandle entityHandle;
    public uint CurrentTick;
    public EntityManager EntityManager;
    
    [BurstCompile]
    public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
    {
        var chipStates = chunk.GetComponentDataPtrRW(ref chipStateHandle);
        var instructionsLookup = chunk.GetBufferAccessor(ref instructionsHandle);
        var equipLookup = chunk.GetComponentDataPtrRO(ref equippedHandle);
        var entityLookup = chunk.GetEntityDataPtrRO(entityHandle);
        var idLookup = chunk.GetComponentDataPtrRO(ref idHandle);

        for(int i = 0; i < chunk.Count; i++)
        {
            if(instructionsLookup[i].Length == 0)
                continue;
            while (chipStates[i].NextTick <= CurrentTick)
            {
                var line = instructionsLookup[i][chipStates[i].NextLine];
                if(!line.IsZeroTickInstruction)
                    chipStates[i].NextTick = CurrentTick + 1;
                if (!LogicParametersMappingSystem.TryGetLogicParameter(line.Left, out var p1))
                {
                    Debug.LogError($"Error in Chip script. Resetting...");
                    instructionsLookup[i].Clear();
                    break;
                }
                
                switch (p1.TypeSizeInBytes)
                {
                    case 1:
                        ProcessLine<byte>(line, i, equipLookup, entityLookup, idLookup);
                        break;
                    case 2:
                        ProcessLine<short>(line, i, equipLookup, entityLookup, idLookup);
                        break;
                    case 4:
                        ProcessLine<int>(line, i, equipLookup, entityLookup, idLookup);
                        break;
                    case 8:
                        ProcessLine<long>(line, i, equipLookup, entityLookup, idLookup);
                        break;
                    case 16:
                        ProcessLine<int4>(line, i, equipLookup, entityLookup, idLookup);
                        break;
                    // NOTE: VariableAccess.ConstantValue must have a type with size that matches the highest case number here 
                    default:
                        Debug.LogError($"ChipScript Compiler Error: Parameter {p1.Name} has invalid size {p1.TypeSizeInBytes}");
                        break;
                }
            
                HandleSelf(ref chipStates[i]);
                chipStates[i].NextLine %= instructionsLookup[i].Length;
                if(chipStates[i].NextLine == 0)
                    break;
            }
        }
    }

    void ProcessLine<T>(ChipInstructions line, int entityIndexInChunk, Equipped* equipLookup, Entity* entityLookup, Identifieable* idLookup) where T: unmanaged
    {
        T result = line.Right.IsConstant ?
            UnsafeUtility.As<long, T>(ref line.Right.Constant)
        : ReadFromInventory<T>(equipLookup[entityIndexInChunk].Wearer, line.Right.Parameter);
        
        if (line.WritesToSelf)
        {
            var self = entityLookup[entityIndexInChunk];
            WriteTo(self, new ParameterAccess{ TypeId = idLookup[entityIndexInChunk], TypeParameterIndex = line.Left.TypeParameterIndex }, result);
        }
        else 
        {
            WriteToInventory(equipLookup[entityIndexInChunk].Wearer, line.Left, result);
        }
    }

    T ReadFromInventory<T>(Entity wearer, ParameterAccess parameter) where T: unmanaged
    {
        var item = FindEquipmentForType(wearer, parameter.TypeId);
        if(item == Entity.Null)
            return default;
        if(!LogicParametersMappingSystem.TryGetLogicParameter(parameter.TypeId, parameter.TypeParameterIndex, out var p1))
            return default;
        if(!DynamicTypeHandles.TryGetValue(p1.TypeIndex, out var typeHandle))
            return default;
        if(!EntityLookup[item].Chunk.Has(ref typeHandle))
            return default;
        
        var location = EntityManager.GetRawComponentDataWithTypeRW(item, p1.TypeIndex) + p1.Offset;
        return *(T*)location;
        
    }
    
    void WriteToInventory<T>(Entity wearer, ParameterAccess parameter, T data) where T: unmanaged
    {
        var item = FindEquipmentForType(wearer, parameter.TypeId);
        if(item == Entity.Null)
            return;
        WriteTo(item, parameter, data);
    }

    void WriteTo<T>(Entity item, ParameterAccess parameter, T data)
        where T : unmanaged
    {
        if(!LogicParametersMappingSystem.TryGetLogicParameter(parameter.TypeId, parameter.TypeParameterIndex, out var p1))
            return;
        if(!DynamicTypeHandles.TryGetValue(p1.TypeIndex, out var typeHandle))
            return;
        if(!EntityLookup[item].Chunk.Has(ref typeHandle))
            return;
        
        var location = EntityManager.GetRawComponentDataWithTypeRW(item, p1.TypeIndex) + p1.Offset;
        ((T*)location)[0] = data;
    }

    Entity FindEquipmentForType(Entity wearer, Identifieable id)
    {
        if(!InventoryLookup.TryGetBuffer(wearer, out var inv))
            return Entity.Null;
        
        foreach (var item in inv)
        {
           if(!item.IsEquipmentSlot || item.Entity == Entity.Null || item.Type.BaseId != id)
               continue;
           return item.Entity;
        }
        return Entity.Null;
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
            ChipOperation.Negate => state.OpRhs == 0 ? 1 : 0,
            _ => state.OpResult
        };
        
        if(state.Operator == ChipOperation.Jump || (state.Operator == ChipOperation.JumpConditional && state.OpLhs != 0))
            state.NextLine = state.OpRhs;
        else state.NextLine++;

        state.Operator = ChipOperation.NoOp;
    }
}