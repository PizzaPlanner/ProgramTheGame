using Unity.Entities;
using UnityEngine.UIElements;

[UxmlElement]
public partial class InventoryElement : VisualElement, IComponentData
{
    VisualElement Items, Payload;
    public InventoryElement()
    {
        pickingMode = PickingMode.Ignore;
        Add(Items = new VisualElement(){name = "Items"});
        Add(Payload = new VisualElement(){name = "Payload"});
    }

    public Inventory Inventory
    {
        set
        {
            var slots = value.GetItemsAsSlots();
            for (int i = 0; i < slots.Length; i++)
            {
                if(Items.childCount <= i)
                    Items.Add(new ItemButton());
                var btn = Items[i] as ItemButton;
                btn!.Value = slots[i];
            }
            while(Items.childCount > slots.Length)
                Items.RemoveAt(slots.Length);
            
            var payload = value.GetAllPayloads();
            for (var i = 0; i < payload.Values.Length; i++)
            {
                var v = payload.Values[i];
                if (Payload.childCount <= i)
                    Payload.Add(new ItemButton());
                var btn = Payload[i] as ItemButton;
                btn!.Value = v;
            }
            while(Payload.childCount > payload.Length)
                Payload.RemoveAt(payload.Length);
        }
    }
}