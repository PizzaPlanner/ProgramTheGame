using System;
using UnityEditor;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Search;
using UnityEngine.UIElements;

[Serializable]
class InventorySlotAuthoring
{
    public ItemAuthoring Item;
    public uint Amount;
    public TagAsset Filter;
    public Inventory.TransferDirection TransferDirection;
    public bool IsEquipmentSlot => IsAnyEquipment(Filter);

    bool IsAnyEquipment(TagAsset filter)
    {
        if(filter == null)
            return false;
        if(filter.name == "TagEquipment")
            return true;
        // foreach(var iFilter in filter.TypeImpliedTags)
        //     if(IsAnyEquipment(iFilter))
        //         return true;
        return false;
    }
}
//
// [CustomEditor(typeof(InventoryAuthoring))]
// class InventoryAuthoringEditor : Editor
// {
//     VisualElement root;
//     public override VisualElement CreateInspectorGUI()
//     {
//         var property = serializedObject.FindProperty("Slots");
//         root = new VisualElement();
//         root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Items/Authoring/InventoryAuthoring.uss"));
//         var inventory = new VisualElement() { name = "Inventory" };
//         RedrawInventory(inventory, property);
//         root.Add(inventory);
//         root.Add(new Button(() =>
//         {
//             serializedObject.Update();
//             property.arraySize++;
//             var newProp = property.GetArrayElementAtIndex(property.arraySize - 1);
//             newProp.FindPropertyRelative("Filter").objectReferenceValue = null;
//             serializedObject.ApplyModifiedProperties();
//             RedrawInventory(inventory, property);
//         }) { text = "+" });
//         root.Add(new Button(() =>
//         {
//             serializedObject.Update();
//             property.arraySize--;
//             serializedObject.ApplyModifiedProperties();
//             RedrawInventory(inventory, property);
//         }) { text = "-" });
//         return root;
//     }
//
//     void RedrawInventory(VisualElement inventory, SerializedProperty property)
//     {
//         // inventory.Clear();
//         // inventory.Add(new Label($"Inventory with {property.arraySize} slots"));
//         // for (int i = 0; i < property.arraySize; i++)
//         //     inventory.Add(new InventorySlotField(property.GetArrayElementAtIndex(i)));
//         // root.Bind(serializedObject);
//     }
// }
//
// class InventorySlotField : VisualElement
// {
//     // public InventorySlotField(SerializedProperty property)
//     // {
//     //     Add(new IconPropertyField(property.FindPropertyRelative("Item")){ name = "Icon" });
//     //     Add(new PropertyField(property.FindPropertyRelative("Amount")) { name = "Amount" });
//     //     Add(new PropertyField(property.FindPropertyRelative("TransferDirection")) { name = "TransferDirection" });
//     //     
//     //     var filter = new MaskField() { name = "Filter" };
//     //     filter.choices = IdTagLibrary.AllTags.ToList();
//     //     filter.RegisterValueChangedCallback(x => SelectionToFilter(x.newValue, property.FindPropertyRelative("Filter")));
//     //     Add(filter);
//     //     ApplyFilter(property);
//     // }
//     //
//     // void SelectionToFilter(int mask, SerializedProperty filterProp)
//     // {
//     //     filterProp.serializedObject.Update();
//     //     filterProp.arraySize = 0;
//     //     for (int i = 0; i < TagList.Names.Length; i++)
//     //     {
//     //         if ((mask & (1 << i)) != 0)
//     //         {
//     //             filterProp.arraySize++;
//     //             filterProp.GetArrayElementAtIndex(filterProp.arraySize - 1).stringValue = IdTagManager.Names[i];
//     //         }
//     //     }
//     //     filterProp.serializedObject.ApplyModifiedProperties();
//     //     ApplyFilter(filterProp);
//     // }
//     //
//     // void ApplyFilter(SerializedProperty property)
//     // {
//     //     foreach (var f2 in IdTagManager.Names)
//     //     {
//     //         RemoveFromClassList(f2.ToLower());
//     //     }
//     //     var filter = property.FindPropertyRelative("Filter").arraySize;
//     //     var mask = this.Q<MaskField>();
//     //     mask.value = 0;
//     //     for (int i = 0; i < filter; i++)
//     //     {
//     //         var f = property.FindPropertyRelative("Filter").GetArrayElementAtIndex(i);
//     //         AddToClassList(f.stringValue.ToLower());
//     //         this.Q<MaskField>().value |= (int)IdTagManager.ToTag(f.stringValue).Value;
//     //     }
//     // }
// }

class IconPropertyField : PropertyField
{
    public IconPropertyField(SerializedProperty property) : base(property)
    {
        RegisterCallback<SerializedPropertyChangeEvent>(Init);
        RegisterCallback<MouseDownEvent>(_ =>
        {
            var ctx = SearchService.CreateContext("asset", "t:ItemAuthoring t:IdentifieableAuthoring");
            var svs = new SearchViewState(ctx)
            {
                flags = SearchViewFlags.CompactView | SearchViewFlags.GridView | SearchViewFlags.DisableInspectorPreview,
                selectHandler = (item, b) =>
                {
                    property.serializedObject.Update();
                    property.objectReferenceValue = item.ToObject();
                    property.serializedObject.ApplyModifiedProperties();
                    Debug.Log($"Picked {item.description}");
                }
            };
            SearchService.ShowPicker(svs);
        });
    }

    void Init(SerializedPropertyChangeEvent evt)
    {
        UpdateSprite(evt.changedProperty.objectReferenceValue);
    }

    void UpdateSprite(UnityEngine.Object targetItem)
    {
        if(targetItem is not ItemAuthoring auth)
            style.backgroundImage = default;
        else
            style.backgroundImage = auth.GetComponent<IdentifieableAuthoring>().Icon;
    }
    
}