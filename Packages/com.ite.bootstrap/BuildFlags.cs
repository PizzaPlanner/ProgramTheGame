using System;

namespace WorldManagement
{
    [Flags]
    public enum BuildFlags
    {
        None = 0,
        
        /// <summary>
        /// Add the required system groups for all leaf systems and up to the root system groups.
        /// This is required for the world to show up in the Systems and Entity Hierarchy windows,
        /// and to be usable in the Player Loop
        /// </summary>
        AddImplicitSystems = 1,
        
        /// <summary>
        /// Add world to the player loop, so Unity will update it automatically.
        /// </summary>
        AddToPlayerLoop = 2 | AddImplicitSystems,
        
        /// <summary>
        /// Remove all rate managers from system groups, so no waiting is required between manual <see cref="Unity.Entities.World.Update">World.Update()</see> calls
        /// </summary>
        RemoveRateManagers = 4
    }
}