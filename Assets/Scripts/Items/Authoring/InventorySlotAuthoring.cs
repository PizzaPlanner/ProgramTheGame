using System;
using UnityEditor;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Search;
using UnityEngine.UIElements;
using ObjectField = UnityEditor.UIElements.ObjectField;

[Serializable]
class InventorySlotAuthoring
{
    public ItemAuthoring Item;
    public uint Amount;
    public ItemCategory Filter;
    public Inventory.TransferDirection TransferDirection;
}

[CustomEditor(typeof(InventoryAuthoring))]
class InventoryAuthoringEditor : Editor
{
    VisualElement root;
    public override VisualElement CreateInspectorGUI()
    {
        var property = serializedObject.FindProperty("Slots");
        root = new VisualElement();
        root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Items/Authoring/InventoryAuthoring.uss"));
        var inventory = new VisualElement() { name = "Inventory" };
        RedrawInventory(inventory, property);
        root.Add(inventory);
        root.Add(new Button(() =>
        {
            serializedObject.Update();
            property.arraySize++;
            serializedObject.ApplyModifiedProperties();
            RedrawInventory(inventory, property);
        }) { text = "+" });
        root.Add(new Button(() =>
        {
            serializedObject.Update();
            property.arraySize--;
            serializedObject.ApplyModifiedProperties();
            RedrawInventory(inventory, property);
        }) { text = "-" });
        return root;
    }

    void RedrawInventory(VisualElement inventory, SerializedProperty property)
    {
        inventory.Clear();
        inventory.Add(new Label($"Inventory with {property.arraySize} slots"));
        for (int i = 0; i < property.arraySize; i++)
            inventory.Add(new InventorySlotField(property.GetArrayElementAtIndex(i)));
        root.Bind(serializedObject);
    }
}

class InventorySlotField : VisualElement
{
    public InventorySlotField(SerializedProperty property)
    {
        Add(new IconPropertyField(property.FindPropertyRelative("Item")){ name = "Icon" });
        Add(new PropertyField(property.FindPropertyRelative("Amount")) { name = "Amount" });
        Add(new PropertyField(property.FindPropertyRelative("Filter")) { name = "Filter" });
        Add(new PropertyField(property.FindPropertyRelative("TransferDirection")) { name = "TransferDirection" });
        ApplyFilter(property);
        this.Q<PropertyField>("Filter").RegisterValueChangeCallback(x =>ApplyFilter(property));
    }

    void ApplyFilter(SerializedProperty property)
    {
        foreach (var f2 in Enum.GetNames(typeof(ItemCategory)))
        {
            RemoveFromClassList($"category-{f2.ToLower()}");
        }
        var filter = (ItemCategory)property.FindPropertyRelative("Filter").intValue;
        AddToClassList($"category-{filter.ToString().ToLower()}");
    }
}

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