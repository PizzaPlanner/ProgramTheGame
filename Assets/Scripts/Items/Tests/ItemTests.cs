using NUnit.Framework;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Items.Tests
{
    public class ItemTests
    {
        const long SomeStackableGUID = 755564084;
        
        [UnityTest]
        public IEnumerator TestTransfer()
        {
            SceneManager.LoadScene(0, LoadSceneMode.Additive);
            var world = World.DefaultGameObjectInjectionWorld;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            var e = world.EntityManager.CreateEntity(typeof(Inventory), typeof(ReservedSpace));
            var inventory = world.EntityManager.GetBuffer<Inventory>(e);
            inventory.Add(new Inventory
            {
                Amount = 3,
                Type = ItemId.From(new Identifieable(SomeStackableGUID))
            });
            inventory.Add(default);
            ItemTransaction.Builder.Schedule(ecb).Transfer(e, 0, e, 1).Commit();
            ecb.Playback(world.EntityManager);
            yield return new WaitForSeconds(2);
            inventory = world.EntityManager.GetBuffer<Inventory>(e);
            Assert.AreEqual(3, inventory[1].Amount);
            Assert.AreEqual(0, inventory[0].Amount);
        }
    }
}