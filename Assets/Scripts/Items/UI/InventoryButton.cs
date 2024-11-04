using Identifiables.UI;
using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class InventoryButton : ItemButton
{
    public Entity Container;
    public int SlotId;
    
    public static Action<InventoryButton, InventoryButton> OnDragDrop;
    public static Action<InventoryButton> OnDragStart;
    public static Action OnDragStop;

    public InventoryButton() : base()
    {
        Add(new VisualElement(){name = "LockIcon"});
    }
    
    Entity _equipmentInSlot;
    public Inventory Slot
    {
        set
        {
            // Name = value.Type.BaseId.DefaultName.Value;
            Info = value.IsEmpty ? "" : value.IsEquipmentSlot ? "E" : value.Amount.ToString();
            Target = value.Type.BaseId;
            RemoveFromClassList($"{_slot.SlotFilter.ToString().ToLower()}");
            AddToClassList($"{value.SlotFilter.ToString().ToLower()}");
            IsEmpty = value.IsEmpty;
            _slot = value;
            EntityInSlotChanged = _equipmentInSlot != value.Entity;
            _equipmentInSlot = value.Entity;
            RegisterCallback<MouseUpEvent>(OnClickUp);
            RegisterCallback<MouseDownEvent>(OnClickDown);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);
            if(value.Transfer == Inventory.TransferDirection.In)
                AddToClassList("direction-in");
            else
                RemoveFromClassList("direction-in");
            if(value.Transfer == Inventory.TransferDirection.Out)
                AddToClassList("direction-out");
            else
                RemoveFromClassList("direction-out");
            if(value.Transfer == Inventory.TransferDirection.None)
                AddToClassList("direction-none");
            else
                RemoveFromClassList("direction-none");
                
        }
        get => _slot;
    }
    Inventory _slot;
    
    public bool EntityInSlotChanged {get;private set;}
    
    public static IdentifiableButton DragDropGhost;
    
    void OnMouseMove(MouseMoveEvent evt)
    {
        if (this.HasMouseCapture() && DragDropGhost != null)
        {
            var pointerDelta = evt.mousePosition;
            
            DragDropGhost.Target = this.Slot.Type.BaseId;

            DragDropGhost.style.display = DisplayStyle.Flex;
            DragDropGhost.BringToFront();
            DragDropGhost.transform.position = new Vector2(
                Mathf.Clamp(pointerDelta.x, 0, panel.visualTree.worldBound.width),
                Mathf.Clamp(pointerDelta.y, 0, panel.visualTree.worldBound.height));
        }
    }

    void OnClickDown(MouseDownEvent evt)
    {
        if (Slot.IsEmpty || !Slot.CanTransferOut)
            return;
        
        this.CaptureMouse();
        OnDragStart?.Invoke(this);
    }

    void OnClickUp(MouseUpEvent evt)
    {
        if(!this.HasMouseCapture())
            return;
        if(DragDropGhost != null)
            DragDropGhost.style.display = DisplayStyle.None;
        this.ReleaseMouse();
        var top = GetBaseRoot(this);
        var hitQuery = top.Query<InventoryButton>().Where(x => x.worldBound.Contains(evt.mousePosition));
        var hit = hitQuery.First();
        OnDragStop?.Invoke();
        if(hit != null && !hit.Slot.CanTransferInto)
            return;
        
        OnDragDrop(this, hitQuery.First());
    }
    
    VisualElement GetBaseRoot(VisualElement e)
    {
        while(e.parent != null)
            e = e.parent;
        return e;
    }

    const string RESERVED_PLUS_COLOR = "#44ff00";
    const string RESERVED_MINUS_COLOR = "#ff4400";
    
    public void ApplyReservations(DynamicBuffer<ReservedSpace> reservations)
    {
        var reservedType = Slot.Type;
        int total = 0;
        foreach (var res in reservations)
        {
            if(res.Slot != SlotId)
                continue;
            if(reservedType == default)
                reservedType = res.ReservedType;
            total += (int)res.Amount * (res.IsIncoming ? 1 : -1);
        }
        if(total != 0)
            Info = $"{Info} (<color={(total > 0 ? RESERVED_PLUS_COLOR : RESERVED_MINUS_COLOR)}>{(total > 0 ? '+' : "")}{total}</color>)";
        if(reservedType != Slot.Type)
            Target = reservedType.BaseId;
    }
}