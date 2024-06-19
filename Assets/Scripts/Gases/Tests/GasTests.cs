using Bootstrap.Tests;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine;

namespace Gases.Tests
{
    public class GasTests
    {
        static float HALF_PRECISION_DELTA_FACTOR = 1e-3f;
        
        [Test]
        [TestCase(1, 3)]
        [TestCase(1, 999)]
        [TestCase(7.3f, 2.7f)]
        public void IsEnergyEqualized(float eLeft, float eRight)
        {
            using var world = Setup.MakeTestWorldNoRateManagers(typeof(GasUpdateSystem));
            var gasSystem = world.Unmanaged.GetExistingUnmanagedSystem<GasUpdateSystem>();
            var gasModel = world.EntityManager.GetComponentDataRW<GasModel>(gasSystem).ValueRW;
            
            var left = new int2(0,0);
            var right = new int2(0, 1);
            
            gasModel.Emitters.Add(new GasEmitter
            {
                Location = left,
                Output = GasTile.Empty.WithEnergy(eLeft).With(Gas.Hydrogen, 1)
            });
            gasModel.Emitters.Add(new GasEmitter
            {
                Location = right,
                Output = GasTile.Empty.WithEnergy(eRight).With(Gas.Hydrogen, 1)
            });
            world.Update();
            gasModel = world.EntityManager.GetComponentData<GasModel>(gasSystem);
            Debug.Log($"E {gasModel.Tiles[left].TotalEnergy} // {gasModel.Tiles[right].TotalEnergy}");

            for (int i = 0; i < 10; i++)
            {
                world.Update();
                Debug.Log($"E {gasModel.Tiles[left].TotalEnergy} // {gasModel.Tiles[right].TotalEnergy}");
            }
            
            var sum = eLeft + eRight;
            gasModel = world.EntityManager.GetComponentData<GasModel>(gasSystem);
            Assert.AreEqual(sum/2, gasModel.Tiles[left].TotalEnergy, sum * HALF_PRECISION_DELTA_FACTOR);
            Assert.AreEqual(sum/2, gasModel.Tiles[right].TotalEnergy, sum * HALF_PRECISION_DELTA_FACTOR);
            Assert.AreEqual(sum, gasModel.Tiles[left].TotalEnergy + gasModel.Tiles[right].TotalEnergy, sum * HALF_PRECISION_DELTA_FACTOR);
        }
        
        [Test]
        [TestCase(1u, 3u)]
        [TestCase(1u, 999u)]
        [TestCase(7u, 2u)]
        public void AreMolesEqualized(uint mLeft, uint mRight)
        {
            using var world = Setup.MakeTestWorldNoRateManagers(typeof(GasUpdateSystem));
            var gasSystem = world.Unmanaged.GetExistingUnmanagedSystem<GasUpdateSystem>();
            var gasModel = world.EntityManager.GetComponentDataRW<GasModel>(gasSystem).ValueRW;
            
            var left = new int2(0,0);
            var right = new int2(0, 1);
            
            gasModel.Emitters.Add(new GasEmitter
            {
                Location = left,
                Output = GasTile.Empty.WithEnergy(1).With(Gas.Hydrogen, mLeft)
            });
            gasModel.Emitters.Add(new GasEmitter
            {
                Location = right,
                Output = GasTile.Empty.WithEnergy(1).With(Gas.Hydrogen, mRight)
            });
            world.Update();
            gasModel = world.EntityManager.GetComponentData<GasModel>(gasSystem);
            Debug.Log($"M {gasModel.Tiles[left].TotalMoles} // {gasModel.Tiles[right].TotalMoles}");

            for (int i = 0; i < 20 && gasModel.TileChanges.Length > 0; i++)
            {
                world.Update();
                Debug.Log($"M {gasModel.Tiles[left].TotalMoles} // {gasModel.Tiles[right].TotalMoles}");
            }
            
            var sum = mLeft + mRight;
            gasModel = world.EntityManager.GetComponentData<GasModel>(gasSystem);
            Assert.AreEqual(sum/2f, gasModel.Tiles[left].TotalMoles, sum * HALF_PRECISION_DELTA_FACTOR);
            Assert.AreEqual(sum/2f, gasModel.Tiles[right].TotalMoles, sum * HALF_PRECISION_DELTA_FACTOR);
            Assert.AreEqual(sum, gasModel.Tiles[left].TotalMoles + gasModel.Tiles[right].TotalMoles, sum * HALF_PRECISION_DELTA_FACTOR);
        }
        
        [Test]
        [TestCase(1u, Gas.Hydrogen, 3u, Gas.Chlorine)]
        [TestCase(1u, Gas.CarbonDioxide, 999u, Gas.Oxygen)]
        [TestCase(7u, Gas.CarbonDioxide, 2u, Gas.Chlorine)]
        public void CanDifferentGasTypesMix(uint mLeft, Gas gLeft, uint mRight, Gas gRight)
        {
            using var world = Setup.MakeTestWorldNoRateManagers(typeof(GasUpdateSystem));
            var gasSystem = world.Unmanaged.GetExistingUnmanagedSystem<GasUpdateSystem>();
            var gasModel = world.EntityManager.GetComponentDataRW<GasModel>(gasSystem).ValueRW;
            
            var left = new int2(0,0);
            var right = new int2(0, 1);
            
            gasModel.Emitters.Add(new GasEmitter
            {
                Location = left,
                Output = GasTile.Empty.WithEnergy(1).With(gLeft, (ushort)mLeft)
            });
            gasModel.Emitters.Add(new GasEmitter
            {
                Location = right,
                Output = GasTile.Empty.WithEnergy(1).With(gRight, (ushort)mRight)
            });
            world.Update();
            gasModel = world.EntityManager.GetComponentData<GasModel>(gasSystem);
            Debug.Log($"M {gasModel.Tiles[left].TotalMoles} // {gasModel.Tiles[right].TotalMoles}");

            for (int i = 0; i < 20 && gasModel.TileChanges.Length > 0; i++)
            {
                world.Update();
                Debug.Log($"M {gasModel.Tiles[left].TotalMoles} // {gasModel.Tiles[right].TotalMoles}");
            }
            
            var sum = mLeft + mRight;
            gasModel = world.EntityManager.GetComponentData<GasModel>(gasSystem);
            Assert.AreEqual(sum/2f, gasModel.Tiles[left].TotalMoles, sum * HALF_PRECISION_DELTA_FACTOR);
            Assert.AreEqual(sum/2f, gasModel.Tiles[right].TotalMoles, sum * HALF_PRECISION_DELTA_FACTOR);
            Assert.AreEqual(sum, gasModel.Tiles[left].TotalMoles + gasModel.Tiles[right].TotalMoles, sum * HALF_PRECISION_DELTA_FACTOR);
        }
    }
}