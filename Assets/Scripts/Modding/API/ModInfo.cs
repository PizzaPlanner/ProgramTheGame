using System;
using System.Reflection;
using UnityEngine;

namespace Modding.API
{
    /// <summary>
    /// Add a class or struct implementing this interface into any assembly to make the
    /// game load it as a mod
    /// </summary>
    public interface IMod { }
    

    public static class ModAssemblyCollector
    {
        public static void CollectMods()
        {
            Debug.Log("Collecin mods");
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var modData = asm.GetType("ModGeneratedData", false);
                if(modData == null)
                    continue;
                var nameField = modData.GetField("ModName", BindingFlags.Public | BindingFlags.Static);
                var hashField = modData.GetField("ModHash", BindingFlags.Public | BindingFlags.Static);
                if(nameField == null || hashField == null)
                    continue;
                Debug.Log($"Found mod {nameField.GetRawConstantValue()}");
            }
        }
    }

}
namespace Modding.API.Generated
{ }
