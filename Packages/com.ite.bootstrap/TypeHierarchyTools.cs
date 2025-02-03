using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Entities;

namespace WorldManagement
{
    static class TypeHierarchyTools
    {
        internal static Dictionary<Type, List<Type>> GenerateSystemGrouping(IEnumerable<Type> systems)
        {
            var systemGrouping = new Dictionary<Type, List<Type>>();

            foreach (var system in systems)
            {
                if (system.IsAbstract || IsRootSystemGroup(system))
                    continue;

                var updateInGroups = system.GetCustomAttributes<UpdateInGroupAttribute>(true).ToArray();
                foreach (var updateAttribute in updateInGroups)
                {
                    var group = updateAttribute?.GroupType ?? typeof(SimulationSystemGroup);
                    if (!typeof(ComponentSystemGroup).IsAssignableFrom(group))
                        continue;

                    AddToGroup(system, group);
                }

                // Not marked system goes to SimulationSystemGroup by default
                if (!updateInGroups.Any())
                    AddToGroup(system, typeof(SimulationSystemGroup));
            }

            void AddToGroup(Type systemType, Type groupType)
            {
                if (!systemGrouping.ContainsKey(groupType))
                    systemGrouping[groupType] = new List<Type>();
                systemGrouping[groupType].Add(systemType);
            }

            return systemGrouping;
        }

        public static Type GetParentSystemGroup(Type systemType) =>
            IsRootSystemGroup(systemType)
                ? systemType
                : TypeManager.GetSystemAttributes(systemType, typeof(UpdateInGroupAttribute))
                             .Cast<UpdateInGroupAttribute>()
                             .FirstOrDefault()?.GroupType ?? typeof(SimulationSystemGroup);

        internal static bool IsRootSystemGroup(Type t) => t == typeof(PresentationSystemGroup) ||
                                                          t == typeof(SimulationSystemGroup) ||
                                                          t == typeof(InitializationSystemGroup);

        internal static void AddToUpdateList(ComponentSystemGroup group, Type type)
        {
            if (TypeManager.IsSystemManaged(type))
                group.AddSystemToUpdateList(group.World.GetExistingSystemManaged(type));
            else
                group.AddSystemToUpdateList(group.World.GetExistingSystem(type));
        }
    }
}