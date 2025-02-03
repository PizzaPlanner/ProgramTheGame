using Core;
using Core.Equipment;
using Equipment;
using Identifiables;
using NUnit.Framework;
using System;
using System.Collections;
using Tasks.UI;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Serialization;
using Unity.Mathematics;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Hash128 = Unity.Entities.Hash128;

public class EquipmentTests
{
    const string PREFABS_SCENE_GUID = "b4b56c108a8ce4b428c7023956b53206";
    const string TEST_PREFABS_SCENE_GUID = "3b90cc877b1cd244a93fc1aed89ac24a";
    
    static EntityManager EntityManager => TestWorld.EntityManager;
    static World TestWorld;

    static void LoadSubScene(string guid)
    {
        SceneSystem.LoadSceneAsync(TestWorld.Unmanaged, new EntitySceneReference(new Hash128(guid), 0), new SceneSystem.LoadParameters
        {
            AutoLoad = true, 
            Flags = SceneLoadFlags.BlockOnImport | SceneLoadFlags.BlockOnStreamIn
        });
    }
    
    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        TestWorld = World.DefaultGameObjectInjectionWorld;
        SceneManager.LoadScene(0, LoadSceneMode.Additive);
        LoadSubScene(PREFABS_SCENE_GUID);
        LoadSubScene(TEST_PREFABS_SCENE_GUID);
        TestWorld.Update();
        TestWorld.Update();
        using var tickQuery = EntityManager.CreateEntityQuery(typeof(SimulationState));
        tickQuery.GetSingletonRW<SimulationState>().ValueRW.RequestedTicksPerSecond = long.MaxValue;
        using var modQuery = EntityManager.CreateEntityQuery(new EntityQueryBuilder(Allocator.Temp)
                                                                       .WithAll<Identifiable, Prefab>().WithOptions(EntityQueryOptions.IncludePrefab));
        Assert.IsTrue(!modQuery.IsEmpty);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        
    }

    [SetUp]
    public void Setup()
    {

    }
    
    [TearDown]
    public void TearDown()
    {
        EntityManager.CompleteAllTrackedJobs();
        using var query = EntityManager.CreateEntityQuery(new EntityQueryBuilder(Allocator.Temp)
            .WithAny<Grids.CGrid, OnGrid, Identifiable>());
        var systemHandle = TestWorld.GetExistingSystemManaged<BeginInitializationEntityCommandBufferSystem>().SystemHandle;
        var ecb = EntityManager.GetComponentData<BeginInitializationEntityCommandBufferSystem.Singleton>(systemHandle)
                           .CreateCommandBuffer(TestWorld.Unmanaged);
        foreach (var e in query.ToEntityArray(Allocator.Temp))
        {
            ecb.DestroyEntity(e);
        }
        TestWorld.Update();
    }

    Entity FindEntity<T>() where T : unmanaged, IComponentData
    {
        using var query = EntityManager.CreateEntityQuery(new EntityQueryBuilder(Allocator.Temp)
            .WithAll<T>());
        if(query.IsEmpty)
            return default;
        return query.ToEntityArray(Allocator.Temp)[0];
    }
    T FindComponent<T>() where T : unmanaged, IComponentData
    {
        using var query = EntityManager.CreateEntityQuery(new EntityQueryBuilder(Allocator.Temp)
                                                                       .WithAll<T>());
        if(query.IsEmpty)
            return default;
        return query.ToComponentDataArray<T>(Allocator.Temp)[0];
    }

    IEnumerator TickWorld(int ticks)
    {
        using var query = EntityManager.CreateEntityQuery(typeof(SimulationState));
        var state = query.GetSingleton<SimulationState>();
        yield return new WaitUntil(() => query.GetSingleton<SimulationState>().LastSimulationTick >= state.LastSimulationTick + ticks, 
            TimeSpan.FromSeconds(ticks),
            () => Assert.Fail($"Timeout simulating {ticks} ticks"), 
            WaitTimeoutMode.Realtime);
    }

    IEnumerator WaitForEntityWith<T>(int timeout = 2, bool successOnTimeout = false) where T : unmanaged, IComponentData
    {
        yield return new WaitUntil(() => FindEntity<T>() != default, TimeSpan.FromSeconds(timeout), 
            ()=> Assert.IsTrue(successOnTimeout, $"No Entity has {typeof(T).Name}"), WaitTimeoutMode.InGameTime);    
    }
    
    IEnumerator GUISelect(Entity e)
    {
        using var query = EntityManager.CreateEntityQuery(typeof(Selected));
        EntityManager.RemoveComponent<Selected>(query);
        EntityManager.AddComponent<Selected>(e);
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
    }
    
    [UnityTest]
    public IEnumerator CryoPodCreatesUnit()
    {
        var grid = EntityManager.Instantiate(Testing.ID.LabSceneCryoPodOpener.SpawnablePrefab);
        yield return WaitForEntityWith<UnitDispenser>();
        var dispenser = FindEntity<UnitDispenser>();
        yield return GUISelect(dispenser);
        Assert.IsTrue(UISystemGroup.TryGet<SelectedUnitElement>(out var ui));
        ui.Q<CreateTaskButton>("DispenseUnitTask").wantCreateTask = true;
        yield return WaitForEntityWith<Lungs>(3);
    }
    
    [UnityTest]
    public IEnumerator CryoPodRequiresSkillToOpen()
    {
        var grid = EntityManager.Instantiate(Testing.ID.LabSceneCryoPodNoOpener.SpawnablePrefab);
        yield return WaitForEntityWith<UnitDispenser>();
        var dispenser = FindEntity<UnitDispenser>();
        yield return GUISelect(dispenser);
        Assert.IsTrue(UISystemGroup.TryGet<SelectedUnitElement>(out var ui));
        ui.Q<CreateTaskButton>("DispenseUnitTask").wantCreateTask = true;
        yield return WaitForEntityWith<Lungs>(3, true);
        Assert.AreEqual(FindEntity<Lungs>(), Entity.Null);
    }
    
    [UnityTest]
    public IEnumerator BuildOneTileFromFloorIsReachable()
    {
        var grid = EntityManager.Instantiate(Testing.ID.LabSceneForConstruction.SpawnablePrefab);
        var plan = EntityManager.CreateEntity(typeof(PlannedConstruction));
        EntityManager.SetComponentData(plan, new PlannedConstruction
        {
            Prefab = Identifiable.FromName("Foundation"),
            Location = new int2(-1, 3),
            Mode = ConstructionMode.UnderConstruction,
            Parent = grid,
            Materials = EntityManager.GetComponentData<ConstructionVariant>(Identifiable.FromName("Foundation").SpawnablePrefab)
        });
        yield return WaitForEntityWith<ItemOrderTask>();
        yield return TickWorld(6);
        var order = FindComponent<ItemOrderTask>();
        Assert.AreEqual(OrderState.Assigned, order.State);
    }
    
    [UnityTest]
    public IEnumerator BuildFurtherThanOneTileFromFloorIsUnreachable()
    {
        var grid = EntityManager.Instantiate(Testing.ID.LabSceneForConstruction.SpawnablePrefab);
        var plan = EntityManager.CreateEntity(typeof(PlannedConstruction));
        EntityManager.SetComponentData(plan, new PlannedConstruction
        {
            Prefab = Identifiable.FromName("Foundation"),
            Location = new int2(-2, 3),
            Mode = ConstructionMode.UnderConstruction,
            Parent = grid,
            Materials = EntityManager.GetComponentData<ConstructionVariant>(Identifiable.FromName("Foundation").SpawnablePrefab)
        });
        yield return WaitForEntityWith<ItemOrderTask>();
        yield return TickWorld(6);
        var order = FindComponent<ItemOrderTask>();
        Assert.AreEqual(OrderState.Requested, order.State);
    }
}