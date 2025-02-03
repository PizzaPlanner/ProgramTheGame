using Core.Status;
using Navigation;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace Units
{
    [WithNone(typeof(Dead))]
    [WithNone(typeof(Ghost))]
    public partial struct TodoPriorityJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<Task> taskLookup;
        [ReadOnly] public ComponentLookup<TaskRequirement> requirementLookup;
        [ReadOnly] public ComponentLookup<NavTree> navTreeLookup;
        public ComponentLookup<WorkedOn> workedOnLookup;
        public uint currentTick;
        struct PrioritySorter : IComparer<ToDos>
        {
            public int Compare(ToDos x, ToDos y) => y.PerceivedPriority.CompareTo(x.PerceivedPriority);
        }


        void Execute(Entity e, ref DynamicBuffer<ToDos> todos, in DynamicBuffer<Skills> skills, in OnGrid onGrid)
        {
            if (todos.Length == 0)
                return;

            var shouldResetUnreachableState = navTreeLookup[onGrid.Grid].LastUpdate >= currentTick;
            for (int i = 0; i < todos.Length; i++)
            {
                ref var todo = ref todos.ElementAt(i);
                if(CalculatePreconditions(e, ref todo, shouldResetUnreachableState))
                    todo.Prioritize(e, taskLookup, requirementLookup, skills);
            }
            todos.AsNativeArray().Sort(new PrioritySorter());
            var best = todos[0];
            if (best.Status is ToDoStatus.Approaching or ToDoStatus.Working)
                return;
            
            // Switched active ToDo
            if (best.PerceivedPriority > (int)TaskPriority.Idle)
            {
                best.Status = ToDoStatus.Approaching;
                todos[0] = best;
            }
            for (int i = 1; i < todos.Length; i++)
            {
                var td = todos[i];
                if (!td.IsIgnored)
                {
                    workedOnLookup.SetComponentEnabled(todos[i].Task, false);
                    td.Status = ToDoStatus.UnderConsideration;
                    todos[i] = td;
                }
            }
        }

        bool CalculatePreconditions(Entity e, ref ToDos todo, bool pathingChanged)
        {
            if(!taskLookup.TryGetComponent(todo.Task, out var task))
                return false;
            if(pathingChanged && todo.Status == ToDoStatus.Unreachable)
                todo.Status = ToDoStatus.Requested;
            return true;
        }
    }
}