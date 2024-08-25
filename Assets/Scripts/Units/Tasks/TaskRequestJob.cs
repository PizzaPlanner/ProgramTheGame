using Core.Grid;
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
            if (!storageLookup.Exists(task.Origin))
            {
                Debug.Log($"Remove dangling task {t} of destroyed {task.Origin}");
                ecb.DestroyEntity(sortKey, t);
                return;
            }

            for (var i = 0; i < dependencies.Length; i++)
            {
                var dependency = dependencies[i];
                if (storageLookup.Exists(dependency.Task))
                    return;
                dependencies.RemoveAt(i--);
            }

            if (task.Executor == Entity.Null)
            {
                if(!crewLookup.TryGetBuffer(task.CrewProvider, out var crew))
                    return;
                
                foreach (var member in crew)
                {
                    var todos = todoLookup[member.Member];
                    if (IsTaskTopToDo(todos, t))
                    {
                        task.Executor = member.Member;
                        break;
                    }

                    if (Request(t, todos))
                    {
                        todos.Add(new ToDos
                        {
                            Task = t,
                            Status = TaskOutcome.Requested
                        });
                        radio.Add(new BroadcastStatus{BroadcastedTo = member.Member});
                    }
                }
            }

            if (task.IsPersonalized)
               return; 

            if (VerifyExecutor(t, task))
            {
                foreach (var entity in radio)
                {
                    if(entity.BroadcastedTo == task.Executor)
                        continue;
                    
                    var todos = todoLookup[entity.BroadcastedTo];
                    for (int i = 0; i < todos.Length; i++)
                        if (todos[i].Task == t)
                        {
                            todos.RemoveAt(i--);
                        }
                }
            }
            else 
                task.Executor = Entity.Null;
        }

        bool VerifyExecutor(Entity t, Task task)
        {
            if(!todoLookup.TryGetBuffer(task.Executor, out var todos))
                return false;
            return IsTaskTopToDo(todos, t);
        }

        bool IsTaskTopToDo(DynamicBuffer<ToDos> todos, Entity task) => todos.Length > 0 
                                                                       && todos[0].Task == task 
                                                                       && todos[0].Status is TaskOutcome.Approaching or TaskOutcome.Working;

        bool Request(Entity t, DynamicBuffer<ToDos> todos)
        {
            var tdna = todos.AsNativeArray();
            foreach(var entry in tdna)
                if(entry.Task == t)
                    return false;
            return true;
        }
    }
}