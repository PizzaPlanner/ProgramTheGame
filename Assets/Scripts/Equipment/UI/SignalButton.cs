using Identifiables.UI;
using System;
using Unity.Entities;
using UnityEngine.UIElements;

public class SignalButton : IdentifiableButton
{
    IdTag _assignableTo;
    public IdTag AssignableTo
    {
        set
        {
            if(_assignableTo.Value != 0)
                UnregisterCallback<MouseDownEvent>(OnClickAssign);
            if(value.Value != 0)
                RegisterCallback<MouseDownEvent>(OnClickAssign);
            _assignableTo = value;
        }
        get => _assignableTo;
    }

    public int Value
    {
        set
        {
            if(value == 0)
                RemoveFromClassList("with-value");
            else
                AddToClassList("with-value");
            _value = value;
            this.Q<Label>().text = value.ToString();
        }
        get => _value;
    }
    int _value;

    Entity _entity;
    Identifieable _value1;

    public Entity Entity
    {
        set
        {
            _entity = value;
            if(_entity == Entity.Null)
                RemoveFromClassList("shows-entity");
            else
                AddToClassList("shows-entity");
        }
        get => _entity;
    }

    void OnClickAssign(MouseDownEvent evt)
    {
        if(AssignableTo.Value == 0)
            return;
            
        SignalSelectionWindow.Show(this, x =>
        {
            if(Target != x.Target)
                OnValueChanged?.Invoke(x.Target);
            Target = x.Target;
            Entity = x.Entity;
            Value = x.Value;
        }, AssignableTo);
    }
    
    public Action<Identifieable> OnValueChanged;
}

public class SignalSelectionIdCollection : VisualElement
{
    public IdTag Tag;
    public Tab Tab;
}