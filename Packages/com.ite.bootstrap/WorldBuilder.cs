using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using static WorldManagement.TypeHierarchyTools;

namespace WorldManagement
{
    public class WorldBuilder
    {
        readonly List<Type> _systemsToInclude = new();
        readonly List<Type> _playerLoopRequiredSystems = new();
        readonly List<Type> _attributesToExclude = new();
        bool _forceIncludeBufferSystems;
        static Dictionary<Type, List<Type>> _systemGrouping;
        static WorldSystemFilterFlags _lastUsedFlags;
        static Type[] _bufferSystems;

        public WorldBuilder(WorldSystemFilterFlags flags = WorldSystemFilterFlags.Default)
        {
            if (_systemGrouping == null || _lastUsedFlags != flags)
            {
                TypeManager.Initialize();
                var allCollectedTypes = TypeManager.GetSystems(flags);
                _lastUsedFlags = flags;
                _systemGrouping = GenerateSystemGrouping(allCollectedTypes);
                _bufferSystems = allCollectedTypes
                                 .Where(x => typeof(EntityCommandBufferSystem).IsAssignableFrom(x))
                                 .ToArray();
            }
        }

        public static World BuildDefaultWorld(string worldName = "DefaultWorld")
        {
            return World.DefaultGameObjectInjectionWorld = new WorldBuilder()
                   .WithRecursive<InitializationSystemGroup>()
                   .WithRecursive<SimulationSystemGroup>()
                   .WithRecursive<PresentationSystemGroup>()
                   .Build(worldName, BuildFlags.AddToPlayerLoop);
        }

        public WorldBuilder WithRecursive<T>(bool ignoreDisableAutoCreateOnChildren = false)
        {
            return WithRecursive(typeof(T), ignoreDisableAutoCreateOnChildren);
        }

        // Include a system and all subsystems (that have UpdateInGroup(systemType))
        public WorldBuilder WithRecursive(Type systemType, bool ignoreDisableAutoCreateOnChildren = false)
        {
            With(systemType);
            if (_systemGrouping.TryGetValue(systemType, out var children))
                foreach (var child in children
                             .Where(x => ignoreDisableAutoCreateOnChildren ||
                                         x.GetCustomAttribute<DisableAutoCreationAttribute>() == null))
                    WithRecursive(child);
            return this;
        }

        // Force include buffer systems of all system groups the world will have.
        // Note that buffer systems are also included by default when adding a group via WithRecursive
        public WorldBuilder WithBufferSystems()
        {
            _forceIncludeBufferSystems = true;
            return this;
        }

        public WorldBuilder With<T>() => With(typeof(T));

        // Include one ore more system types to the world.
        // If the world is added to the player loop, also adds all parent systems that are required to run these systems
        // i.e. if type T has UpdateInGroup(MyGroupType), then MyGroupType will also be added into the world
        // This chain continues until hitting a root system group
        public WorldBuilder With(params Type[] types)
        {
            foreach (var t in types)
            {
                if (t == null)
                    continue;

                _systemsToInclude.Add(t);
                WithImplicit(t);
            }

            return this;
        }

        void WithImplicit(Type t)
        {
            _playerLoopRequiredSystems.Add(t);
            var parentGroup = GetParentSystemGroup(t);
            if (!IsRootSystemGroup(t) && !_playerLoopRequiredSystems.Contains(parentGroup))
                WithImplicit(parentGroup);
        }


        // Exclude all Systems that have this attribute from being added
        public WorldBuilder ExcludeAttribute<T>() where T : Attribute
        {
            return ExcludeAttribute(typeof(T));
        }

        // Exclude all Systems that have this attribute from being added
        public WorldBuilder ExcludeAttribute(Type t)
        {
            if (t != null)
                _attributesToExclude.Add(t);
            return this;
        }

        public World Build(string worldName, BuildFlags flags)
        {
            // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
            if (flags.HasFlag(BuildFlags.AddImplicitSystems))
                _systemsToInclude.AddRange(_playerLoopRequiredSystems);

            if (_forceIncludeBufferSystems)
                foreach (var bufferSystem in _bufferSystems)
                    if (_systemsToInclude.Contains(GetParentSystemGroup(bufferSystem)))
                        _systemsToInclude.Add(bufferSystem);

            var finalSystems = _systemsToInclude
                               .Distinct()
                               .Where(x => !x.GetCustomAttributes(true)
                                             .Any(y => _attributesToExclude.Contains(y.GetType())))
                               .ToList();

            // Make sure types are in correct order
            TypeManager.SortSystemTypesInCreationOrder(finalSystems);

            var world = new World(worldName);
            var systemTypeIndices = new NativeList<SystemTypeIndex>(Allocator.Temp);
            foreach (var sti in finalSystems.Select(TypeManager.GetSystemTypeIndex))
                systemTypeIndices.Add(sti);
            // Create all systems and call their OnCreate in order
            var createdSystems = world.GetOrCreateSystemsAndLogException(systemTypeIndices, AllocatorManager.Temp);
            if (createdSystems.Length != finalSystems.Count)
            {
                world.Dispose();
                Debug.LogError($"Could not create {worldName}: Some Systems could not be created");
                return default;
            }

            Debug.Log(
                $"Create {worldName} with {createdSystems.Length} Systems (in creation order):\n  {string.Join(",\n  ", finalSystems.Select(x => x.Name))}");

            // Build Update tree as defined in systemGrouping
            foreach (var (groupType, children) in _systemGrouping)
            {
                if (!finalSystems.Contains(groupType))
                    continue;

                var group = (world.GetExistingSystemManaged(groupType) as ComponentSystemGroup)!;
                if(flags.HasFlag(BuildFlags.RemoveRateManagers))
                    group.RateManager = null;
                foreach (var childType in children)
                    AddToUpdateList(group, childType);
            }

            // Add world to player loop
            // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
            if (flags.HasFlag(BuildFlags.AddToPlayerLoop))
                ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(world);

            return world;
        }
    }

    [Flags]
    public enum BuildFlags
    {
        None = 0,
        AddImplicitSystems = 1,
        AddToPlayerLoop = 2 | AddImplicitSystems,
        RemoveRateManagers = 4
    }
}