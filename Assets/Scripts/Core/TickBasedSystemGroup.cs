using System.Diagnostics;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

namespace Core
{
    public class TickBasedRateManager : IRateManager
    {
        long TickRate { get; set; }

        long nextTimeStamp, currentTimeStamp, lastTimeStamp;
        
        public bool ShouldGroupUpdate(ComponentSystemGroup group)
        {
            currentTimeStamp = Stopwatch.GetTimestamp();
            var delta = nextTimeStamp - currentTimeStamp;
            if(delta > 0 || TickRate <= 0)
                return false;
            lastTimeStamp = nextTimeStamp;
            nextTimeStamp = math.max(nextTimeStamp + TickRate, currentTimeStamp);
            return true;
        }

        public float Timestep
        {
            get => TickRate * 1f / Stopwatch.Frequency;
            set
            {
                var newRate = !float.IsNormal(value) || value < 0 ? 0 
                    : (long)(Stopwatch.Frequency * value);
                if(newRate == TickRate)
                    return;
                if(newRate > 0)
                    nextTimeStamp = nextTimeStamp - TickRate + newRate;
                TickRate = newRate;
            }
        }
    }
    
    public struct SimulationState : IComponentData
    {
        public float RequestedTicksPerSecond;
        public uint LastSimulationTick;
        public uint SystemVersionAtLastTick;
        public float DeltaTime;
    }
    
    public partial class TickBasedSystemGroup : ComponentSystemGroup
    {
        public bool TickedThisFrame { get; private set; }
        protected override void OnCreate()
        {
            base.OnCreate();
            RateManager = new TickBasedRateManager();
            EntityManager.CreateSingleton(new SimulationState { RequestedTicksPerSecond = 10 });
            RequireForUpdate<WorldTypes.InGame>();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<SimulationState>());
        }

        protected override void OnStartRunning()
        {
            var op = SceneManager.LoadSceneAsync("InGame", LoadSceneMode.Additive);
            LoadingScreen.Load(op, "Enter Session");
            base.OnStartRunning();
        }

        protected override void OnStopRunning()
        {
            base.OnStopRunning();
            if(!SystemAPI.HasSingleton<WorldTypes.InGame>())
                return;
            Debug.Log($"Unloading InGame from World {World.Name}");
            var op = SceneManager.UnloadSceneAsync("InGame");
            LoadingScreen.Load(op, "Leaving Session");
        }

        protected override void OnUpdate()
        {
            ref var simState = ref SystemAPI.GetSingletonRW<SimulationState>().ValueRW;
            var proposedTimestep = 1f / simState.RequestedTicksPerSecond;
            var deltaTime = SystemAPI.Time.DeltaTime;
            RateManager.Timestep = math.max(proposedTimestep, deltaTime);
            simState.DeltaTime = RateManager.Timestep; 
            TickedThisFrame = RateManager.ShouldGroupUpdate(this);
            if(!TickedThisFrame)
                return;
            
            var rm = RateManager;
            RateManager = null;
            base.OnUpdate();
            RateManager = rm;
            simState.LastSimulationTick++;
            simState.SystemVersionAtLastTick = LastSystemVersion;
        }
    }
}