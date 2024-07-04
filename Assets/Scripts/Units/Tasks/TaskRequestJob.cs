using Unity.Collections;
using Unity.Entities;

namespace Units
{
    public partial struct TaskRequestJob : IJobEntity
    {
        public NativeArray<Entity> upForWork;
        public EntityStorageInfoLookup storageLookup;
        [NativeDisableParallelForRestriction]
        public BufferLookup<ToDos> todoLookup;
        public EntityCommandBuffer.ParallelWriter ecb;
        void Execute(Entity t, [ChunkIndexInQuery] int sortKey, ref Task task, ref DynamicBuffer<TaskDependencies> dependencies)
        {
            if(!storageLookup.Exists(task.Origin))
                ecb.DestroyEntity(sortKey, t);
            foreach(var dpd in dependencies)
                if(storageLookup.Exists(dpd.Dependency))
                    return;
            
            if (task.Executor == Entity.Null)
            {
                foreach (var entity in upForWork)
                {
                    var todos = todoLookup[entity];
                    if (todos[0].Task == t)
                    {
                        task.Executor = entity;
                        break;
                    }
                    if(Request(t, todos))
                        todos.Add(new ToDos
                        {
                            Task = t,
                            Status = TaskOutcome.Requested
                        });
                }
            }
            
            if(task.IsPersonalized)
               return; 

            if (VerifyExecutor(t, task))
            {
                foreach (var entity in upForWork)
                {
                    if(entity == task.Executor)
                        continue;
                    
                    var todos = todoLookup[entity];
                    for (int i = 0; i < todos.Length; i++)
                        if (todos[i].Task == t)
                        {
                            todos.RemoveAt(i);
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
            return todos[0].Task == t;
        }

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