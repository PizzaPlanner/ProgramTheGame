using Core.Status;
using Grids;
using Units;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace Core.Equipment
{
    public struct Mood : IComponentData
    {
        public int Fullness;
        public int Capacity;
        public int MaxChangePerTick;

        public int Level
        {
            get
            {
                if(Fullness < Capacity * -0.9f)
                    return -3;
                if(Fullness < Capacity * -0.6f)
                    return -2;
                if(Fullness < Capacity * -0.2f)
                    return -1;
                if(Fullness < Capacity * 0.2f)
                    return 0;
                if(Fullness < Capacity * 0.6f)
                    return 1;
                if(Fullness < Capacity * 0.9f)
                    return 2;
                return 3;
            }
        }
    }
    
     [UpdateInGroup(typeof(TickBasedSystemGroup))]
    partial struct MoodSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (mood, stats, e) in
                     SystemAPI.Query<RefRW<Mood>, DynamicBuffer<Statuses>>()
                              .WithNone<Dead>()
                              .WithEntityAccess())
            {
                var newMood = 0;
                foreach(var status in stats)
                    newMood += status.MoodChange;
                var sign = math.sign(newMood - mood.ValueRW.Fullness);
                mood.ValueRW.Fullness = math.clamp(mood.ValueRW.Fullness + sign * mood.ValueRW.MaxChangePerTick, -mood.ValueRW.Capacity, mood.ValueRW.Capacity);
            }
        }
    }
}