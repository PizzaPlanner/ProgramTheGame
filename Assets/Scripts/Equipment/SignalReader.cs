using Programmable;
using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Equipment
{
    public struct SignalReader : IChipTargetable
    {
        public enum ReadMode
        {
            ReadFromTarget = 0,
            CountSignalsTypedFrom = 1
        }
        [LogicWritable]
        public ReadMode Mode;
        [LogicWritable]
        public Entity From;
        public Signal OutputSignal;
    }

    public struct SignalWriter : IChipTargetable
    {
        [LogicWritable]
        public Signal InputSignal;  
    }
    
    public struct Signal
    {
        public Entity Entity;
        public long Number;
    }

    public struct AllSignals : IComponentData
    {
        public NativeParallelHashMap<Entity, Signal> SignalsBySender;
        public NativeParallelHashMap<Entity, int> SignalCount;
    }
    partial struct SignalingSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponentData(state.SystemHandle, new AllSignals
            {
                SignalsBySender = new NativeParallelHashMap<Entity, Signal>(0, Allocator.Persistent),
                SignalCount = new NativeParallelHashMap<Entity, int>(0, Allocator.Persistent)
            });
        }

        public void OnDestroy(ref SystemState state)
        {
            state.EntityManager.GetComponentData<AllSignals>(state.SystemHandle).SignalsBySender.Dispose();
            state.EntityManager.GetComponentData<AllSignals>(state.SystemHandle).SignalCount.Dispose();
        }

        public void OnUpdate(ref SystemState state)
        {
            var allSignals = SystemAPI.GetComponentRW<AllSignals>(state.SystemHandle).ValueRW;
            var signals = allSignals.SignalsBySender;
            var signalCount = allSignals.SignalCount;
            signalCount.Clear();
            
            foreach(var (writer, e) in SystemAPI.Query<SignalWriter>()
                                                .WithNone<Equipped>()
                                                .WithAll<JustChangedLocation>()
                                                .WithChangeFilter<SignalWriter>().WithEntityAccess())
                signals.Remove(e);

            foreach (var (writer, e) in SystemAPI.Query<SignalWriter>()
                                                 .WithAll<Equipped>()
                                                 .WithChangeFilter<SignalWriter>().WithEntityAccess())
            {
                signalCount.TryAdd(writer.InputSignal.Entity, 0);
                signalCount[writer.InputSignal.Entity] += 1;
                signals[e] = writer.InputSignal;
            }

            foreach (var reader in SystemAPI.Query<RefRW<SignalReader>>()
                                            .WithAll<Equipped>()
                                            .WithChangeFilter<SignalReader>())
            {
                reader.ValueRW.OutputSignal = reader.ValueRW.Mode switch
                {
                    SignalReader.ReadMode.ReadFromTarget => signals.TryGetValue(reader.ValueRW.From, out var signal) ? signal : default,
                    SignalReader.ReadMode.CountSignalsTypedFrom => new Signal{Number = signalCount.TryGetValue(reader.ValueRW.From, out var count) ? count : 0},
                    _ => default
                };
            }
        }
    }
}