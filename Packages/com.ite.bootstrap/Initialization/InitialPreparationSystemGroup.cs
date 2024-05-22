using Unity.Entities;
using UnityEngine;

namespace Initialization
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class InitialPreparationSystemGroup : ComponentSystemGroup
    {
        public struct State : IComponentData
        {
            public int Required;
            public int Done;
        }

        public struct Loaded : IComponentData { }

        protected override void OnStartRunning()
        {
            EntityManager.CreateSingleton<State>();
            if (SystemAPI.TryGetSingletonEntity<Loaded>(out var loaded))
                EntityManager.DestroyEntity(loaded);
        }

        protected override void OnStopRunning()
        {
            EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<State>());
            EntityManager.CreateSingleton<Loaded>();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            var state = SystemAPI.GetSingleton<State>();
            if (state.Required == 0)
            {
                Enabled = false;
                return;
            }

            Debug.Log($"Init {World.Name}: {state.Done}/{state.Required}");
            if (state.Done >= state.Required)
                Enabled = false;
        }
    }
}