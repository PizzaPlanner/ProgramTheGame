using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes;
using UnityEngine;
using static WorldManagement.TypeHierarchyTools;

namespace WorldManagement
{
    public partial class WorldBuilder
    {
        readonly List<Type> _systemsToInclude = new();
        readonly List<Type> _playerLoopRequiredSystems = new();
        readonly List<Type> _attributesToExclude = new();
        readonly List<SceneReference> _subscenesToPreload = new();
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

        /// <summary>
        /// Same as <see cref="WithRecursive"/>
        /// </summary>
        public WorldBuilder WithRecursive<T>(bool ignoreDisableAutoCreateOnChildren = false)
        {
            return WithRecursive(typeof(T), ignoreDisableAutoCreateOnChildren);
        }

        /// Include a system and all child systems (that have UpdateInGroup(systemType))
        /// <param name="ignoreDisableAutoCreateOnChildren">if set to true, all <see cref="DisableAutoCreationAttribute">[DisableAutoCreation]</see> on child systems are ignored </param>
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

        /// Force include buffer systems of all system groups the world will have.
        /// Note that buffer systems are also included by default in groups added via <see cref="WithRecursive"/>
        public WorldBuilder WithBufferSystems()
        {
            _forceIncludeBufferSystems = true;
            return this;
        }

        /// <summary>
        /// Same as <see cref="With"/>
        /// </summary>
        public WorldBuilder With<T>() => With(typeof(T));

        /// Include one or more system types to the world.
        /// If the world is added to the player loop, also adds all parent systems that are required to run these systems
        /// This chain continues until hitting a root system group
        /// <example>
        /// <code>
        /// partial class MySystemGroup: ComponentSystemGroup { }
        ///
        /// [UpdateInGroup(typeof(MySystemGroup))]
        /// partial struct MySystem : ISystem { }
        ///
        /// [UpdateInGroup(typeof(PresentationSystemGroup))]
        /// partial struct MyOtherSystem : ISystem { }
        /// 
        /// var playerLoopWorld = new WorldBuilder().With(typeof(MySystem), typeof(MyOtherSystem)).Build("InPlayerLoop", BuildFlags.AddToPlayerLoop);
        /// var manualWorld     = new WorldBuilder().With(typeof(MySystem), typeof(MyOtherSystem)).Build("Manual",       BuildFlags.None);
        /// </code>
        /// Considering these systems, <c>playerLoopWorld</c> will contain <c>SimulationSystemGroup, MySystemGroup, MySystem, PresentationSystemGroup, MyOtherSystem</c>,
        /// however <c>manualWorld</c> will only contain <c>MySystem, MyOtherSystem</c>
        /// </example>
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


        /// <summary>
        /// Same as <see cref="ExcludeAttribute"/>
        /// </summary>
        public WorldBuilder ExcludeAttribute<T>() where T : Attribute
        {
            return ExcludeAttribute(typeof(T));
        }

        /// Exclude all Systems that have this attribute from being added.
        /// This basically gives the attribute the same functionality for the resulting world that
        /// <see cref="DisableAutoCreationAttribute"/> has for all worlds.
        public WorldBuilder ExcludeAttribute(Type t)
        {
            if (t != null)
                _attributesToExclude.Add(t);
            return this;
        }

        /// <summary>
        /// Ensures that the given subscene is loaded before the world starts updating.
        /// Requires <see cref="SceneSystemGroup"/> and children to be present in the world 
        /// </summary>
        public WorldBuilder WithPreloadScene(SceneReference sceneRef)
        {
            _subscenesToPreload.Add(sceneRef);
            return this;    
        }

        public World Build(string worldName, BuildFlags flags, bool logCreatedSystems = false)
        {
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

            if(logCreatedSystems)
                Debug.Log($"Created {worldName} with {createdSystems.Length} Systems (in creation order):\n  {string.Join(",\n  ", finalSystems.Select(x => x.Name))}");

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
            
            // Preload all required subscenes
            if (_subscenesToPreload.Count > 0)
            {
                foreach(var scene in _subscenesToPreload)
                    SceneSystem.LoadSceneAsync(world.Unmanaged, scene.SceneGUID, new SceneSystem.LoadParameters
                    {
                        AutoLoad = true, 
                        Flags = SceneLoadFlags.BlockOnImport | SceneLoadFlags.BlockOnStreamIn
                    });
                var sceneGroup = world.GetExistingSystemManaged<SceneSystemGroup>();
                sceneGroup.Update();
            }
            
            // Add world to player loop
            if (flags.HasFlag(BuildFlags.AddToPlayerLoop))
                ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(world);
            
            return world;
        }
    }
}