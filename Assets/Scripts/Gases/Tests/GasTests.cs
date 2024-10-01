// using NUnit.Framework;
// using Unity.Mathematics;
// using WorldManagement;
//
// namespace Gases.Tests
// {
//     public class GasTests
//     {
//         static float HALF_PRECISION_DELTA_FACTOR = 1e-3f;
//         
//         [Test]
//         [TestCase(1, 3)]
//         [TestCase(1, 999)]
//         [TestCase(7.3f, 2.7f)]
//         public void IsEnergyEqualized(float eLeft, float eRight)
//         {
//             using var world = WorldBuilder.Presets.TestWorldNoRateManagers(typeof(GasUpdateSystem));
//             
//             var left = world.EntityManager.CreateEntity(typeof(GasVolume), typeof(GridBounds));
//             var right = world.EntityManager.CreateEntity(typeof(GasVolume), typeof(GridBounds));
//             
//             world.EntityManager.SetComponentData(left, GasVolume.Empty.WithEnergy(eLeft).With(Gas.Hydrogen, 1));
//             world.EntityManager.SetComponentData(left, new GridBounds()
//             {
//                 Min = new int2(0, 0),
//                 Max = new int2(0, 0)
//             });
//             
//             world.EntityManager.SetComponentData(right, GasVolume.Empty.WithEnergy(eLeft).With(Gas.Hydrogen, 1));
//             world.EntityManager.SetComponentData(right, new GridBounds()
//             {
//                 Min = new int2(1, 0),
//                 Max = new int2(1, 0)
//             });
//
//             for (int i = 0; i < 10; i++)
//                 world.Update();
//             
//             var sum = eLeft + eRight;
//             var lGas = world.EntityManager.GetComponentData<GasVolume>(left);
//             var rGas = world.EntityManager.GetComponentData<GasVolume>(right);
//             Assert.AreEqual(sum/2, lGas.TotalEnergy, sum * HALF_PRECISION_DELTA_FACTOR);
//             Assert.AreEqual(sum/2, rGas.TotalEnergy, sum * HALF_PRECISION_DELTA_FACTOR);
//             Assert.AreEqual(sum, lGas.TotalEnergy + rGas.TotalEnergy, sum * HALF_PRECISION_DELTA_FACTOR);
//         }
//         
//         [Test]
//         [TestCase(1u, 3u)]
//         [TestCase(1u, 999u)]
//         [TestCase(7u, 2u)]
//         public void AreMolesEqualized(uint mLeft, uint mRight)
//         {
//             using var world = WorldBuilder.Presets.TestWorldNoRateManagers(typeof(GasUpdateSystem));
//             
//             var left = world.EntityManager.CreateEntity(typeof(GasVolume), typeof(GridBounds));
//             var right = world.EntityManager.CreateEntity(typeof(GasVolume), typeof(GridBounds));
//             
//             world.EntityManager.SetComponentData(left, GasVolume.Empty.WithEnergy(1).With(Gas.Hydrogen, mLeft));
//             world.EntityManager.SetComponentData(left, new GridBounds()
//             {
//                 Min = new int2(0, 0),
//                 Max = new int2(0, 0)
//             });
//             
//             world.EntityManager.SetComponentData(right, GasVolume.Empty.WithEnergy(1).With(Gas.Hydrogen, mRight));
//             world.EntityManager.SetComponentData(right, new GridBounds()
//             {
//                 Min = new int2(1, 0),
//                 Max = new int2(1, 0)
//             });
//
//             for (int i = 0; i < 10; i++)
//                 world.Update();
//             
//             var sum = mLeft + mRight;
//             var lGas = world.EntityManager.GetComponentData<GasVolume>(left);
//             var rGas = world.EntityManager.GetComponentData<GasVolume>(right);
//             Assert.AreEqual(sum/2f, lGas.TotalMoles, sum * HALF_PRECISION_DELTA_FACTOR);
//             Assert.AreEqual(sum/2f, rGas.TotalMoles, sum * HALF_PRECISION_DELTA_FACTOR);
//             Assert.AreEqual(sum, lGas.TotalMoles + rGas.TotalMoles, sum * HALF_PRECISION_DELTA_FACTOR);
//         }
//         
//         [Test]
//         [TestCase(1u, Gas.Hydrogen, 3u, Gas.Chlorine)]
//         [TestCase(1u, Gas.CarbonDioxide, 999u, Gas.Oxygen)]
//         [TestCase(7u, Gas.CarbonDioxide, 2u, Gas.Chlorine)]
//         public void CanDifferentGasTypesMix(uint mLeft, Gas gLeft, uint mRight, Gas gRight)
//         {
//             using var world = WorldBuilder.Presets.TestWorldNoRateManagers(typeof(GasUpdateSystem));
//             
//             var left = world.EntityManager.CreateEntity(typeof(GasVolume), typeof(GridBounds));
//             var right = world.EntityManager.CreateEntity(typeof(GasVolume), typeof(GridBounds));
//             
//             world.EntityManager.SetComponentData(left, GasVolume.Empty.WithEnergy(1).With(gLeft, mLeft));
//             world.EntityManager.SetComponentData(left, new GridBounds()
//             {
//                 Min = new int2(0, 0),
//                 Max = new int2(0, 0)
//             });
//             
//             world.EntityManager.SetComponentData(right, GasVolume.Empty.WithEnergy(1).With(gRight, mRight));
//             world.EntityManager.SetComponentData(right, new GridBounds()
//             {
//                 Min = new int2(1, 0),
//                 Max = new int2(1, 0)
//             });
//             
//             for (int i = 0; i < 10; i++)
//                 world.Update();
//             
//             var sum = mLeft + mRight;
//             var lGas = world.EntityManager.GetComponentData<GasVolume>(left);
//             var rGas = world.EntityManager.GetComponentData<GasVolume>(right);
//             Assert.AreEqual(sum/2f, lGas.TotalMoles, sum * HALF_PRECISION_DELTA_FACTOR);
//             Assert.AreEqual(sum/2f, rGas.TotalMoles, sum * HALF_PRECISION_DELTA_FACTOR);
//             Assert.AreEqual(sum, lGas.TotalMoles + rGas.TotalMoles, sum * HALF_PRECISION_DELTA_FACTOR);
//         }
//     }
// }