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
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Scenes;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Hash128 = Unity.Entities.Hash128;

public class TestScene
{
    public TestScene(string name, World world, out Entity grid)
    {
        Blueprint.SpawnAsNewGrid(world, Identifiable.FromName(name), out grid);
    }
}

public class EquipmentTests
{
    const string PREFABS_SCENE_GUID = "b4b56c108a8ce4b428c7023956b53206";
    const string TEST_PREFABS_SCENE_GUID = "3b90cc877b1cd244a93fc1aed89ac24a";
    
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
        TestWorld.GetExistingSystemManaged<TickBasedSystemGroup>().RateManager = null;
        LoadSubScene(PREFABS_SCENE_GUID);
        LoadSubScene(TEST_PREFABS_SCENE_GUID);
        TestWorld.Update();
        TestWorld.Update();
        Debug.Log(TestWorld.EntityManager.UniversalQuery.CalculateEntityCount());
        using var modQuery = TestWorld.EntityManager.CreateEntityQuery(new EntityQueryBuilder(Allocator.Temp)
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
        // using var query = TestWorld.EntityManager.CreateEntityQuery(typeof(GridStructure), typeof(Child));
        // TestWorld.EntityManager.DestroyEntity(query);
    }

    Entity FindEntity<T>() where T : unmanaged, IComponentData
    {
        using var query = TestWorld.EntityManager.CreateEntityQuery(new EntityQueryBuilder(Allocator.Temp)
            .WithAll<T>());
        if(query.IsEmpty)
            return default;
        return query.ToEntityArray(Allocator.Temp)[0];
    }
    T FindComponent<T>() where T : unmanaged, IComponentData
    {
        using var query = TestWorld.EntityManager.CreateEntityQuery(new EntityQueryBuilder(Allocator.Temp)
                                                                       .WithAll<T>());
        if(query.IsEmpty)
            return default;
        return query.ToComponentDataArray<T>(Allocator.Temp)[0];
    }

    IEnumerator WaitForEntityWith<T>(int timeout = 2, bool successOnTimeout = false) where T : unmanaged, IComponentData
    {
        yield return new WaitUntil(() => FindEntity<T>() != default, TimeSpan.FromSeconds(timeout), 
            ()=> Assert.IsTrue(successOnTimeout, $"No Entity has {nameof(T)}"), WaitTimeoutMode.InGameTime);    
    }
    
    IEnumerator GUISelect(Entity e)
    {
        using var query = TestWorld.EntityManager.CreateEntityQuery(typeof(Selected));
        TestWorld.EntityManager.RemoveComponent<Selected>(query);
        TestWorld.EntityManager.AddComponent<Selected>(e);
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
    }
    
    [UnityTest]
    public IEnumerator CryoPodCreatesUnit()
    {
        new TestScene("LabSceneCryoPodOpener", TestWorld, out var grid);
        yield return WaitForEntityWith<UnitDispenser>();
        var dispenser = FindEntity<UnitDispenser>();
        yield return GUISelect(dispenser);
        Assert.IsTrue(UISystemGroup.TryGet<SelectedUnitElement>(out var ui));
        ui.Q<CreateTaskButton>("DispenseUnitTask").wantCreateTask = true;
        yield return WaitForEntityWith<Lungs>(10);
    }
    
    [UnityTest]
    public IEnumerator CryoPodRequiresSkillToOpen()
    {
        new TestScene("LabSceneCryoPodNoOpener", TestWorld, out var grid);
        yield return WaitForEntityWith<UnitDispenser>();
        var dispenser = FindEntity<UnitDispenser>();
        yield return GUISelect(dispenser);
        Assert.IsTrue(UISystemGroup.TryGet<SelectedUnitElement>(out var ui));
        ui.Q<CreateTaskButton>("DispenseUnitTask").wantCreateTask = true;
        yield return WaitForEntityWith<Lungs>(10, true);
        Assert.AreEqual(FindEntity<Lungs>(), default);
    }
}