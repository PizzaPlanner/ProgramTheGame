using Unity.Entities;
using UnityEngine.UIElements;

[UxmlElement]
public partial class InventoryElement : VisualElement, IComponentData
{
    public InventoryElement()
    {
        pickingMode = PickingMode.Ignore;
        Add(new VisualElement(){name = "Items"});
        Add(new VisualElement(){name = "Equipment"});
    }
    
    VisualElement[] _slotItems;
    InventoryButton[] _slotButtons;
    
    void Init(Entity container, DynamicBuffer<Inventory> buffer)
    {
        this.Q("Equipment").Clear();
        this.Q("Items").Clear();
        
        var parentInventory = GetFirstAncestorOfType<InventoryElement>();
        if (parentInventory != null)
        {
            AddToClassList("sub-inventory");
        }
        else
            RemoveFromClassList("sub-inventory");
        _slotItems = new VisualElement[buffer.Length];
        _slotButtons = new InventoryButton[buffer.Length];
        _lastEntity = container;
        for (int i = 0; i < buffer.Length; i++)
        {
            if (buffer[i].IsEquipmentSlot)
            {
                var wrapper = new VisualElement(){name = "EquipWrapper"};
                var button = new InventoryButton { Container = container, SlotId = i };
                wrapper.Add(button);
                this.Q("Equipment").Add(wrapper);
                _slotItems[i] = wrapper;
                _slotButtons[i] = button;
            }
            else
            {
                _slotButtons[i] = new InventoryButton { Container = container, SlotId = i };
                _slotItems[i] = _slotButtons[i];
                this.Q("Items").Add(_slotItems[i]);
            }
        }
    }
    
    Entity _lastEntity;
    
    public void Update(Entity e, ref SystemState state)
    {
        if (!state.EntityManager.HasBuffer<Inventory>(e))
        {
            style.display = DisplayStyle.None;
            return;
        }

        style.display = DisplayStyle.Flex;
        var inventory = state.EntityManager.GetBuffer<Inventory>(e, true);
        var reservations = state.EntityManager.GetBuffer<ReservedSpace>(e, true);
        if (e != _lastEntity || inventory.Length != _slotItems.Length)
            Init(e, inventory);
        
        foreach (var button in _slotButtons)
        {
            button.Slot = inventory[button.SlotId];
            button.ApplyReservations(reservations);
        }
    }

    public VisualElement GetSlotItem(int i)
    {
        if (_slotItems == null || i < 0 || i >= _slotItems.Length)
            return null;
        return _slotItems[i];
    }
}