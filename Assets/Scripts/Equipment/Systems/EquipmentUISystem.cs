using Items.UI;
using Units;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Equipment
{
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [UpdateAfter(typeof(InventoryUISystem))]
    partial class EquipmentUISystem : SystemBase
    {
        EntityQuery _inventoryQuery, _danglingQuery, _unselectedQuery;
        protected override void OnCreate()
        {
            _danglingQuery = SystemAPI.QueryBuilder().WithAll<SelectionUI, Item>().WithNone<Equipped, Unit>().Build();
            _unselectedQuery = SystemAPI.QueryBuilder().WithAll<SelectionUI, Equipped>().Build();
            RequireForUpdate<SelectionUI>();
        }

        protected override void OnUpdate()
        {
            foreach(var e in _danglingQuery.ToEntityArray(Allocator.Temp))
                SelectionUI.ClearFrom(e, EntityManager);

            foreach (var e in _unselectedQuery.ToEntityArray(Allocator.Temp))
            {
                var wearer = SystemAPI.GetComponent<Equipped>(e).Wearer;
                if(!SystemAPI.ManagedAPI.HasComponent<SelectionUI>(wearer))
                    SelectionUI.ClearFrom(e, EntityManager);
            }
        }
    }
}