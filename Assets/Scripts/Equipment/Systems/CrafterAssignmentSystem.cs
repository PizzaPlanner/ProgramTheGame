using Unity.Entities;

namespace Equipment
{
    partial struct CrafterAssignmentSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (crafter, equipped) in SystemAPI.Query<RefRW<Crafter>, Equipped>().WithAll<JustChangedLocation>())
            {
                if(crafter.ValueRW.InputInventory == Entity.Null)
                    crafter.ValueRW.InputInventory = equipped.Wearer;
                if(crafter.ValueRW.OutputInventory == Entity.Null)
                    crafter.ValueRW.OutputInventory = equipped.Wearer;
            }
            
            foreach (var (crafter, location) in SystemAPI.Query<RefRW<Crafter>, JustChangedLocation>()
                                                         .WithNone<Equipped>())
            {
                if(crafter.ValueRW.InputInventory == location.PreviousInventory)
                    crafter.ValueRW.InputInventory = Entity.Null;
                if(crafter.ValueRW.OutputInventory == location.PreviousInventory)
                    crafter.ValueRW.OutputInventory = Entity.Null;;
            }
                
        }
    }
}