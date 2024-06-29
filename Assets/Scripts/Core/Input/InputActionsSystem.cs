using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Content;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Core.Input
{
    public interface IInputActionState : IComponentData
    {
        bool IsActiveByDefault => false;
        string[] StateSets { get; }
        InputActionMap StateFor(InputActions asset) => null;
        InputAction ToggledByHotkey(InputActions actions) => null;
    }
    
    internal struct InputActionState : IComponentData { }
    
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public partial class InputActionsSystem : SystemBase
    {
        NativeHashMap<TypeIndex, Entity> _entityLookup;
        NativeHashMap<FixedString32Bytes, Entity> _setDefaults;
        NativeHashMap<FixedString32Bytes, EntityQuery> _actionSets;
        protected override void OnStartRunning()
        {
            EntityManager.CreateSingleton<PointerOverUI>();
            
            InputActions.Instance = new InputActions();
            InputActions.Instance.Enable();
            _entityLookup = new NativeHashMap<TypeIndex, Entity>(0, Allocator.Persistent);
            _actionSets = new NativeHashMap<FixedString32Bytes, EntityQuery>(0, Allocator.Persistent);
            _setDefaults = new NativeHashMap<FixedString32Bytes, Entity>(0, Allocator.Persistent);
            var setBuilders = new Dictionary<FixedString32Bytes, NativeList<ComponentType>>();
            var setDefTypes = new Dictionary<FixedString32Bytes, IInputActionState>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            foreach (var type in asm.GetTypes().Where(x => x.IsValueType && typeof(IInputActionState).IsAssignableFrom(x)))
            {
                var state = (IInputActionState)Activator.CreateInstance(type);
                var e = EntityManager.CreateEntity(type, typeof(InputActionState), typeof(Disabled));
                if(!state.StateSets.Any())
                    Enable(e, state);
                foreach (var set in state.StateSets)
                {
                    if(!setBuilders.ContainsKey(set))
                        setBuilders[set] = new NativeList<ComponentType>(Allocator.Temp);
                    setBuilders[set].Add(ComponentType.FromTypeIndex(TypeManager.GetTypeIndex(type)));
                    if (state.IsActiveByDefault)
                    {
                        _setDefaults[set] = e;
                        setDefTypes[set] = state;
                    }
                }
                _entityLookup[TypeManager.GetTypeIndex(type)] = e;
                if(state.ToggledByHotkey(InputActions.Instance) != null)
                    state.ToggledByHotkey(InputActions.Instance).performed += _ => Toggle(state);
            }

            foreach (var key in setBuilders)
            {
                var components = key.Value;
                _actionSets.Add(key.Key, new EntityQueryBuilder(Allocator.Temp)
                                         .WithAny(ref components).WithAll<InputActionState>()
                                         .Build(this));
                if(_setDefaults.TryGetValue(key.Key, out var setDefault))
                    Enable(setDefault, setDefTypes[key.Key]);
            }
        }

        public void Toggle(IInputActionState state)
        {
            var e = _entityLookup[TypeManager.GetTypeIndex(state.GetType())];
            if(SystemAPI.HasComponent<Disabled>(e))
                Enable(e, state);
            else
                Disable(e, state);
        }

        public void Enable<T>() where T : unmanaged, IInputActionState
        {
            var e = _entityLookup[TypeManager.GetTypeIndex<T>()];
            var state = SystemAPI.GetComponent<T>(e);
            Enable(e, state);
        }
        
        public void Enable(Entity e, IInputActionState state)
        {            
            var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                                        .CreateCommandBuffer(World.Unmanaged);
            foreach(var set in state.StateSets)
                if(_actionSets.TryGetValue(set, out var query))
                    ecb.AddComponent<Disabled>(query, EntityQueryCaptureMode.AtPlayback);
            ecb.RemoveComponent<Disabled>(e);
            state.StateFor(InputActions.Instance)?.Enable();
        }

        public void Disable<T>() where T : unmanaged, IInputActionState
        {
            var e = _entityLookup[TypeManager.GetTypeIndex<T>()];
            var state = SystemAPI.GetComponent<T>(e);
            Disable(e, state);
        }
        
        public void Disable(Entity e, IInputActionState state)
        {
            var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                               .CreateCommandBuffer(World.Unmanaged);
            ecb.AddComponent<Disabled>(e);
            state.StateFor(InputActions.Instance)?.Disable();
            foreach(var set in state.StateSets)
                if(_setDefaults.TryGetValue(set, out var defaultActions))
                    ecb.RemoveComponent<Disabled>(defaultActions);
        }

        protected override void OnStopRunning()
        {
            _entityLookup.Dispose();
            _actionSets.Dispose();
        }

        protected override void OnUpdate()
        {
            var mousePosition = new Vector2(UnityEngine.Input.mousePosition.x, Screen.height - UnityEngine.Input.mousePosition.y);
            var isPointerOverUI = false;
            foreach (var ui in GameObject.FindObjectsByType<UIDocument>(0))
            {
                isPointerOverUI |= ui.rootVisualElement?.panel.Pick(
                    RuntimePanelUtils.ScreenToPanel(ui.rootVisualElement?.panel, mousePosition)) != null;
            }
            if(isPointerOverUI == SystemAPI.HasSingleton<PointerOverUI>())
                return;
            if (isPointerOverUI)
            {
                EntityManager.CreateSingleton<PointerOverUI>();
                EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<PointerNotOverUI>());
            }
            else
            {
                EntityManager.CreateSingleton<PointerNotOverUI>();
                EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<PointerOverUI>());
            }
                
        }
    }
    
    public struct PointerOverUI : IComponentData { }
    public struct PointerNotOverUI : IComponentData { }
}