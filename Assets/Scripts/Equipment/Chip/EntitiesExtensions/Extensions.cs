using Unity.Entities;

namespace Programmable.EntitiesExtensions
{
    // see https://forum.unity.com/threads/dynamically-add-componentdata-s-and-set-value-s-using-an-entitycommandbuffer.1292661/#post-8189499
    public static unsafe class Extensions
    {
        public static void CompleteReadAndWriteDependencies(this EntityManager manager, TypeIndex type)
        {
            var ecs = manager.GetCheckedEntityDataAccess();
            ecs->DependencyManager->CompleteReadAndWriteDependency(type);
        }

        public static byte* GetRawComponentDataWithTypeRW(this EntityManager em, Entity e, TypeIndex type)
        {
            var ecs = em.GetCheckedEntityDataAccess();
            return ecs->EntityComponentStore->GetComponentDataWithTypeRW(e, type, ecs->EntityComponentStore->GlobalSystemVersion);        
        }
    }
}