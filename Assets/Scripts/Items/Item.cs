using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct Item : IComponentData
{
    public ItemCategory Category;
    public uint MaxStack;
}

public enum ItemCategory
{
    Any = 0,
    Material,
    Chip,
    Memory,
    Residue,
    Unit
}

public static class ItemCategoryExtensions
{
    public static bool IsEquipment(this ItemCategory category) => category switch
    {
        ItemCategory.Chip => true,
        ItemCategory.Memory => true,
        _ => false
    };
}