using System;
using UnityEngine.UIElements;

public class ChipOperationDropdown : VisualElement
{
    public ChipOperationDropdown(Action<OperationType> onValueChange)
    {
        var enumFld = new EnumField(OperationType.Set);
        Add(enumFld);
        enumFld.RegisterValueChangedCallback(evt => onValueChange((OperationType)evt.newValue));
    }
    
    public OperationType Op
    {
        set
        {
            ClearClassList();
            AddToClassList($"operation-{value.ToString().ToLower()}");
            _op = value;
            this.Q<EnumField>().value = value;
        }
    }
    OperationType _op;
}