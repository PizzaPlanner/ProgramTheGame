using Identifiables;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Units
{
    public partial struct TodoPriorityJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<Task> taskLookup;
        [ReadOnly] public ComponentLookup<TaskRequirement> requirementLookup;
        public ComponentLookup<WorkedOn> workedOnLookup;
        public Entity IdleTaskPrefab;
        struct PrioritySorter : IComparer<ToDos>
        {
            public int Compare(ToDos x, ToDos y) => y.PerceivedPriority.CompareTo(x.PerceivedPriority);
        }
        
        void Execute(ref DynamicBuffer<ToDos> todos, in DynamicBuffer<Skills> skills)
        {
            if (todos.Length == 0)
                return;
                        
            for (int i = 0; i < todos.Length; i++)
                todos[i] = todos[i].Prioritize(taskLookup, requirementLookup, skills);
            todos.AsNativeArray().Sort(new PrioritySorter());
            var best = todos[0];
            if (best.Status is TaskOutcome.Approaching or TaskOutcome.Working)
            {
                workedOnLookup.SetComponentEnabled(best.Task, best.Status == TaskOutcome.Working);
                return;
            }
            
            // Switched active ToDo
            if (best.PerceivedPriority > (int)TaskPriority.Idle)
            {
                best.Status = TaskOutcome.Approaching;
                todos[0] = best;
            }
            for (int i = 1; i < todos.Length; i++)
            {
                var td = todos[i];
                if (td.Status is TaskOutcome.Requested or TaskOutcome.Approaching or TaskOutcome.Working)
                {
                    workedOnLookup.SetComponentEnabled(todos[i].Task, false);
                    td.Status = TaskOutcome.UnderConsideration;
                    todos[i] = td;
                }
            }
        }
    }
}