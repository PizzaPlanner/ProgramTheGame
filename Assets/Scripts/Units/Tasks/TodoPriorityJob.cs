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
        [ReadOnly] public ComponentLookup<WorkedOn> workedOnLookup;
        
        struct PrioritySorter : IComparer<ToDos>
        {
            public int Compare(ToDos x, ToDos y) => y.PerceivedPriority.CompareTo(x.PerceivedPriority);
        }
        void Execute(ref DynamicBuffer<ToDos> todos) //, in TaskAffinities affinities)
        {
            for (int i = 0; i < todos.Length; i++)
                todos[i] = todos[i].Prioritize(taskLookup, default);
            todos.AsNativeArray().Sort(new PrioritySorter());
            var best = todos[0];
            if (best.Status is TaskOutcome.Approaching or TaskOutcome.Working) 
                return;
            
            // Switched active ToDo
            best.Status = TaskOutcome.Approaching;
            todos[0] = best;
            for (int i = 1; i < todos.Length; i++)
            {
                if(todos[i].Status == TaskOutcome.Working)
                    workedOnLookup.SetComponentEnabled(todos[i].Task, false);
                var td = todos[i];
                td.Status = TaskOutcome.UnderConsideration;
                todos[i] = td;
            }
        }
    }
}