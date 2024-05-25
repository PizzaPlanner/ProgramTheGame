using System.Linq;
using UnityEngine.UIElements;

public class VariableRefElement : VisualElement, IMakeChipCode
{
    public bool IsLeftSide
    {
        set
        {
            this.Q<SignalButton>().AssignableTo = value
                ? IdTagManager.ToTag("Equipment", "ControlFlow")
                : IdTag.All;
            _isLeft = value;
        }
        get => _isLeft;
    }
    bool _isLeft;
    
    SignalButton _signal;
    
    
    public VariableRefElement()
    {
        _signal = new SignalButton() {AssignableTo = IdTag.All};
        _signal.OnValueChanged = OnIdChanged;
        Add(_signal);
        Add(new DropdownField(){name = "Value"});
        OnIdChanged(_signal.Target);
    }

    void OnIdChanged(Identifieable id)
    {
        if (id == default)
            AddToClassList("id-unset");
        else
        {
            this.Q<DropdownField>().choices = new[]{"No Parameter"}
                                              .Concat(LogicParametersMappingSystem
                                                      .GetAllParameters(id).Where(x => !_isLeft || x.Access == LogicAccessType.ReadWrite)
                                                      .Select(x => x.Name.Value)).ToList();
            this.Q<DropdownField>().value = "";
            if(this.Q<DropdownField>().choices.Count == 1)
                AddToClassList("id-unset");
            else
                RemoveFromClassList("id-unset");
        }
        
    }

    public string Code => _signal.Target != default 
        ? ClassListContains("id-unset") 
            ? _signal.Target.DefaultName.Value
            : $"{_signal.Target.DefaultName}.{this.Q<DropdownField>().value}"
        : _signal.Value.ToString();
}