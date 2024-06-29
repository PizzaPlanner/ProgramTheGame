using System;
using Unity.Entities;

namespace WorldManagement
{
    public partial class WorldBuilder
    {
        public static class Presets
        {
            /// <summary>
            /// Creates the default world, like <see cref="DefaultWorldInitialization.Initialize">DefaultWorldInitialization</see> would do.
            /// <example>
            /// The code below is equivalent to having no ICustomBootstrap at all and mainly serves as an example
            /// of how to use WorldBuilder.
            /// <code>
            /// class Bootstrap : ICustomBootstrap
            /// {
            ///     public bool Initialize(string defaultWorldName)
            ///     {
            ///         return WorldManagement.WorldBuilder.Presets.DefaultWorld(defaultWorldName).IsCreated;
            ///     }
            /// }
            /// </code>
            /// </example>
            /// </summary>
            /// <param name="worldName">name of the world to create</param>
            public static World DefaultWorld(string worldName = "DefaultWorld")
            {
                return World.DefaultGameObjectInjectionWorld = new WorldBuilder()
                                                               .WithRecursive<InitializationSystemGroup>()
                                                               .WithRecursive<SimulationSystemGroup>()
                                                               .WithRecursive<PresentationSystemGroup>()
                                                               .Build(worldName, BuildFlags.AddToPlayerLoop);
            }
        
            /// A test world that only contains the passed systems,
            /// and the required parent system groups at any depth, as well as all buffer systems of included system groups
            /// <param name="systems">all the leaf systems to be created</param>
            public static World TestWorld(params Type[] systems)
            {
                return new WorldBuilder()
                       .WithBufferSystems()
                       .With(systems)
                       .Build("Test", BuildFlags.AddImplicitSystems);
            }

            /// A test world that only contains the passed systems,
            /// and the required parent system groups at any depth, as well as all buffer systems of included system groups
            /// This world will remove the RateManagers of all system groups, allowing a repeated world.Update() without having to wait
            /// for rate limited groups to update. 
            /// <param name="systems">all the leaf systems to be created</param>
            public static World TestWorldNoRateManagers(params Type[] systems)
            {
                return new WorldBuilder()
                       .WithBufferSystems()
                       .With(systems)
                       .Build("Test", BuildFlags.AddImplicitSystems | BuildFlags.RemoveRateManagers);
            }
        }
    }
}