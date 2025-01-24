using Core.Grid;
using Space;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace Units
{
    public partial struct TaskRequestJob : IJobEntity
    {
        public EntityStorageInfoLookup storageLookup;
        [NativeDisableParallelForRestriction]
        public BufferLookup<ToDos> todoLookup;
        public EntityCommandBuffer.ParallelWriter ecb;
        [ReadOnly] public BufferLookup<Crew> crewLookup;
        void Execute(Entity t, [ChunkIndexInQuery] int sortKey, ref Task task, ref DynamicBuffer<TaskProvider> dependencies, ref DynamicBuffer<BroadcastStatus> radio)
        {
            // Remove Tasks of destroyed Origin
            if (!storageLookup.Exists(task.Origin))
            {
                ecb.DestroyEntity(sortKey, t);
                return;
            }
            
            // Do not broadcast incomplete Task
            if(task.Target == Entity.Null)
                return;

            // All dependencies must be completed before requesting
            for (var i = 0; i < dependencies.Length; i++)
            {
                var dependency = dependencies[i];
                if (storageLookup.Exists(dependency.Task))
                    return;
                dependencies.RemoveAt(i--);
            }

            // Try assign new Executor
            if (task.Executor == Entity.Null)
            {
                if(!crewLookup.TryGetBuffer(task.CrewProvider, out var crew))
                    return;
                
                foreach (var member in crew)
                {
                    if(!todoLookup.TryGetBuffer(member.Member, out var todos))
                        continue;
                    if (IsTaskTopToDo(todos, t))
                    {
                        task.Executor = member.Member;
                        return;
                    }

                    if (IsRequested(t, todos)) 
                        continue;
                    
                    // Accessing other's buffers from parallel job; do not add to buffer directly! 
                    ecb.AppendToBuffer(sortKey, member.Member, new ToDos
                    {
                        Task = t,
                        Status = ToDoStatus.Requested
                    });
                    radio.Add(new BroadcastStatus{BroadcastedTo = member.Member});
                }
            }
            // Executor got distracted, reassign task next frame
            else if (!task.IsPersonalized && !IsTaskTopToDoForExecutor(t, task))
                task.Executor = Entity.Null;
            
        }

        bool IsTaskTopToDoForExecutor(Entity t, Task task) => todoLookup.TryGetBuffer(task.Executor, out var todos) && IsTaskTopToDo(todos, t);
        bool IsTaskTopToDo(DynamicBuffer<ToDos> todos, Entity task) => todos.Length > 0 
                                                                       && todos[0].Task == task 
                                                                       && todos[0].Status is ToDoStatus.Approaching or ToDoStatus.Working;

        bool IsRequested(Entity t, DynamicBuffer<ToDos> todos)
        {
            var tdna = todos.AsNativeArray();
            foreach(var entry in tdna)
                if(entry.Task == t)
                    return true;
            return false;
        }
    }
}