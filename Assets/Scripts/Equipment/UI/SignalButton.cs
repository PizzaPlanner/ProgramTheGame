using Identifiables;
using Identifiables.UI;
using Modding.API;
using Modding.API.Tags;
using System;
using Unity.Entities;
using UnityEngine.UIElements;

public class SignalButton : IdentifiableButton
{
    TagList _assignableTo;
    public TagList AssignableTo
    {
        set
        {
            if(!_assignableTo.IsUnset)
                UnregisterCallback<MouseDownEvent>(OnClickAssign);
            if(!value.IsUnset)
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
        if(AssignableTo.IsUnset)
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
    public TagList Tag;
    public Tab Tab;
}