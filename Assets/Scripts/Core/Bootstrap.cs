using Unity.Entities;

namespace Core
{
    class Bootstrap : ICustomBootstrap
    {
        public bool Initialize(string defaultWorldName)
        {
            return WorldManagement.WorldBuilder.BuildDefaultWorld(defaultWorldName).IsCreated;
        }
    }
}