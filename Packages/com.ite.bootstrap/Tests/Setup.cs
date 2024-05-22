using System;
using Unity.Entities;

namespace Bootstrap.Tests
{
    public static class Setup
    {
        public static World MakeTestWorld(params Type[] systems)
        {
            return new WorldManagement.WorldBuilder()
                   .WithBufferSystems()
                   .With(systems)
                   .Build("Test", WorldManagement.BuildFlags.AddImplicitSystems);
        }

        public static World WithEntity(this World world, params ComponentType[] t)
        {
            world.EntityManager.CreateEntity(t);
            return world;
        }
    }
}