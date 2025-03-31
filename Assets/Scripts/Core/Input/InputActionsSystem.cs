using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Core.Input
{
    internal struct InputActionState : IComponentData { }
    
    public interface IInputActionState : IComponentData
    {
        bool IsActiveByDefault => false;
        string[] StateSets { get; }
        InputActionMap StateFor(InputActions asset) => null;
        InputAction ToggledByHotkey(InputActions actions) => null;
    }
    
    internal class StateDefinition
    {
        public Entity Entity;
        public IInputActionState State;
        public bool Enabled { get; private set; }
        
        public void Disable(EntityCommandBuffer ecb)
        {
            ecb.AddComponent<Disabled>(Entity);
            State.StateFor(InputActions.Instance)?.Disable();
            Enabled = false;
        }
        
        public void Enable(EntityCommandBuffer ecb)
        {
            ecb.RemoveComponent<Disabled>(Entity);
            State.StateFor(InputActions.Instance)?.Enable();
            Enabled = true;
        }
    }
    internal class InputStateSet
    {
        public StateDefinition[] StatesInSet;
        public int Active;
        public int Default;

        public void Reset(EntityCommandBuffer ecb)
        {
            Active = Default;
            SwitchToActive(ecb);
        }

        public void Enable(IInputActionState state, EntityCommandBuffer ecb)
        {
            for (int i = 0; i < StatesInSet.Length; i++)
                if(state.GetType() == StatesInSet[i].State.GetType())
                    Active = i;
            SwitchToActive(ecb);
        }
        
        void SwitchToActive(EntityCommandBuffer ecb)
        {
            for (int i = 0; i < StatesInSet.Length; i++)
                if(i == Active) 
                    StatesInSet[i].Enable(ecb);
                else 
                    StatesInSet[i].Disable(ecb);
        }

        public void EnforceValidState(EntityCommandBuffer ecb)
        {
            var shouldReset = false;
            for (int i = 0; i < StatesInSet.Length; i++)
                shouldReset |= StatesInSet[i].Enabled != (Active == i);
            if(shouldReset)
                Reset(ecb);
        }
    }
    
    [UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
    public partial class InputActionsSystem : SystemBase
    {
        public static bool IsPointerOverUI { get; private set; }
        
        Dictionary<FixedString32Bytes, InputStateSet> _stateSets;
        List<StateDefinition> allDefs;
        
        protected override void OnStartRunning()
        {
            InputActions.Instance = new InputActions();
            InputActions.Instance.Enable();
            _stateSets = new();
            allDefs = new();
            var ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>()
                               .CreateCommandBuffer(World.Unmanaged);
            var inputAsm = typeof(IInputActionState).Assembly;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()
                                         .Where(x => x.GetReferencedAssemblies()
                                                      .Any(a => a.FullName == inputAsm.GetName().FullName) || x == inputAsm))
            foreach (var type in asm.GetTypes().Where(x => x.IsValueType && typeof(IInputActionState).IsAssignableFrom(x)))
            {
                var state = (IInputActionState)Activator.CreateInstance(type);
                var e = EntityManager.CreateEntity(type, typeof(InputActionState), typeof(Disabled));
                var stateDef = new StateDefinition{ Entity = e, State = state };
                allDefs.Add(stateDef);
                foreach (var set in state.StateSets)
                {
                    if(!_stateSets.ContainsKey(set))
                        _stateSets[set] = new InputStateSet
                        {
                            StatesInSet = Array.Empty<StateDefinition>(),
                            Default = -1,
                            Active = -1
                        };
                    var s = _stateSets[set];
                    if (state.IsActiveByDefault)
                    {
                        if(s.Default != -1)
                            Debug.LogError($"Multiple InputStates are defined as default for {set}");
                        else
                        {
                            s.Default = s.StatesInSet.Length;
                        }
                    }
                    Array.Resize(ref s.StatesInSet, s.StatesInSet.Length + 1);
                    s.StatesInSet[^1] = stateDef;
                    _stateSets[set] = s;
                }

                if (!state.StateSets.Any())
                    stateDef.Enable(ecb);
                else if(state.ToggledByHotkey(InputActions.Instance) != null)
                    state.ToggledByHotkey(InputActions.Instance).performed += _ => SwitchInput(state);
            }
            
            foreach(var set in _stateSets)
                set.Value.Reset(ecb);
        }

        public void SwitchInput(IInputActionState state, EntityCommandBuffer ecb = default)
        {
            if(!ecb.IsCreated)
                ecb = SystemAPI.GetSingleton<BeginInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);
            var isOn = false;
            foreach(var def in allDefs)
                if(state.GetType() == def.State.GetType())
                    isOn = def.Enabled;
            foreach (var set in state.StateSets)
            {
                if(!isOn)
                    _stateSets[set].Enable(state, ecb);
                else
                    _stateSets[set].Reset(ecb);
                
            }

            foreach (var set in _stateSets)
                set.Value.EnforceValidState(ecb);
        }

        protected override void OnStopRunning()
        {
            InputActions.Instance.Disable();
        }

        protected override void OnUpdate()
        {
            var mousePosition = new Vector2(UnityEngine.Input.mousePosition.x, Screen.height - UnityEngine.Input.mousePosition.y);
            IsPointerOverUI = false;
            foreach (var ui in GameObject.FindObjectsByType<UIDocument>(0))
            {
                IsPointerOverUI |= ui?.rootVisualElement?.panel.Pick(
                    RuntimePanelUtils.ScreenToPanel(ui.rootVisualElement?.panel, mousePosition)) != null;
            }
            
        }
    }
}