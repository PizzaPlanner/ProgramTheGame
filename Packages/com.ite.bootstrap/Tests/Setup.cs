using System;
using Unity.Entities;
using WorldManagement;

namespace Bootstrap.Tests
{
    public static class Setup
    {
        /// A test world that only contains of only the passed systems,
        /// and the required parent system groups at any depth, as well as all buffer systems of all parent system groups
        /// <param name="systems">all the leaf systems to be created</param>
        public static World MakeTestWorld(params Type[] systems)
        {
            return new WorldManagement.WorldBuilder()
                   .WithBufferSystems()
                   .With(systems)
                   .Build("Test", WorldManagement.BuildFlags.AddImplicitSystems);
        }

        /// A test world that only contains of only the passed systems,
        /// and the required parent system groups at any depth, as well as all buffer systems of all parent system groups
        /// This world will remove the RateManagers of all system groups, allowing a repeated world.Update() without having to wait
        /// for rate limited groups to update. 
        /// <param name="systems">all the leaf systems to be created</param>
        public static World MakeTestWorldNoRateManagers(params Type[] systems)
        {
            return new WorldManagement.WorldBuilder()
                   .WithBufferSystems()
                   .With(systems)
                   .Build("Test", WorldManagement.BuildFlags.AddImplicitSystems | BuildFlags.RemoveRateManagers);
        }

        public static World WithEntity(this World world, params ComponentType[] t)
        {
            world.EntityManager.CreateEntity(t);
            return world;
        }
    }
}