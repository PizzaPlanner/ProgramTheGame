using Unity.Entities;

public struct Item : IComponentData
{
    public uint MaxStack;
}

public static class ItemCategoryExtensions
{
    public static bool IsEquipment(this IdTag category) => category.Contains(ItemMappingSystem.EquipmentTag);
}