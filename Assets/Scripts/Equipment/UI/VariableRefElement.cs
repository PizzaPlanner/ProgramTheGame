using Identifiables;
using Modding.API;
using Modding.API.Tags;
using System.Linq;
using UnityEngine.UIElements;

public class VariableRefElement : VisualElement, IMakeChipCode
{
    public bool IsLeftSide
    {
        set
        {
            this.Q<SignalButton>().AssignableTo = value
                ? TagList.From(Tag.FromName("TagEquipment"), Tag.FromName("TagControlFlow"))
                : TagList.MatchAll;
            _isLeft = value;
        }
        get => _isLeft;
    }
    bool _isLeft;
    
    public SignalButton Signal {get; private set;}
    
    
    public VariableRefElement()
    {
        Signal = new SignalButton() {AssignableTo = TagList.MatchAll};
        Signal.OnValueChanged = OnIdChanged;
        Add(Signal);
        Add(new DropdownField(){name = "Value"});
        // this.Q<DropdownField>().RegisterValueChangedCallback(OnDropdownValueChanged);
        OnIdChanged(Signal.Target);
    }

    // void OnDropdownValueChanged(ChangeEvent<string> evt)
    // {
    //     var p = LogicParametersMappingSystem.GetAllParameters(Signal.Target).First(x => x.Name == evt.newValue);
    // }

    void OnIdChanged(Identifieable id)
    {
        if (id == default)
            AddToClassList("id-unset");
        else
        {
            var dropdown = this.Q<DropdownField>();
            dropdown.choices = new[]{"No Parameter"}
                               .Concat(LogicParametersMappingSystem
                                       .ParametersOf(id).Where(x => !_isLeft || x.Access == LogicAccessType.ReadWrite)
                                       .Select(x => x.Name.Value)).ToList();
            dropdown.value = dropdown.choices.Contains(dropdown.value) ? dropdown.value : dropdown.choices[0];
            if(dropdown.choices.Count == 1)
                AddToClassList("id-unset");
            else
                RemoveFromClassList("id-unset");
        }
        
    }

    public void SetFrom(CSLineToken line)
    {
        throw new System.NotImplementedException();
    }

    public string Code => Signal.Target != default 
        ? ClassListContains("id-unset") || this.Q<DropdownField>().value == "Id"
            ? Signal.Target.DefaultName.Value
            : $"{Signal.Target.DefaultName}.{this.Q<DropdownField>().value}"
        : Signal.Value.ToString();

    public void SetFrom(CSToken csToken)
    {
        if (csToken.Type == CSToken.TokenType.Parameter)
        {
            var left = csToken.Value.Split('.')[0];
            var right = csToken.Value.Split('.')[1];
            Signal.Target = Identifieable.FromName(left);
            this.Q<DropdownField>().SetValueWithoutNotify(right);
        }
        else if (csToken.Type == CSToken.TokenType.Constant)
        {
            Signal.Value = int.TryParse(csToken.Value, out var l) ? l : 0;
            Signal.Target = default;
        }
        OnIdChanged(Signal.Target);
    }
}