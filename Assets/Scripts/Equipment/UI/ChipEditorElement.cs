using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

[UxmlElement]
public partial class ChipEditorElement : ScrollView
{
    public ChipEditorSuggestionsView Suggestions;
    const int MaxLines = 64;
    public static int NextId;
    public ChipEditorElement()
    {
        Suggestions = new ChipEditorSuggestionsView();
        Suggestions.style.position = Position.Absolute;
        Suggestions.style.top = 0;
        Suggestions.style.left = 0;
        Add(new Button(() => Debug.Log(CollectPseudoCode())){text = "Debug print"});
        var block = new ChipLineBlock("Start");
        Add(block);
        var playground = new VisualElement();
        Add(playground);
        playground.Add(new TextField(999, true, false, '*'));
        // playground.Add(new TextField(999, true, false, '*'){ isReadOnly = true});
        playground.Add(new Button(() =>
        {
            var parser = new ChipScriptParser();
            var script = new ChipScript{Script = playground.Q<TextField>().text};
            parser.Parse(script);
        }));
        Add(Suggestions);
        
        block.Focus();
    }

    string CollectPseudoCode()
    {
        return string.Join("\n", this.Query<ChipLineElement>().ToList().Select(line => 
            line.Code ));
    }

    public void ShowSuggestions(TextField input, ChipLineElement.CurrentInputFor lineState)
    {
        Suggestions.style.display = input.style.display;
        Suggestions.transform.position = RuntimePanelUtils.ScreenToPanel(parent.panel, input.worldBound.position + input.worldBound.size);
        Suggestions.Clear();
        switch (lineState)
        {
            case ChipLineElement.CurrentInputFor.Operation:
                foreach(var str in Enum.GetNames(typeof(OperationType)).Where(x => x.ToLower().Contains(input.text.ToLower())))
                    Suggestions.Add(new Label(str));
                break;
            case ChipLineElement.CurrentInputFor.LeftSide:
                Suggestions.Add(new Label("???"));
                break;
            case ChipLineElement.CurrentInputFor.RightSide:
                Suggestions.Add(new Label("???"));
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        Debug.Log($"Show suggestions for {lineState}: {Suggestions.childCount}");
        Suggestions.Selection = Suggestions.Selection;
    }

    public void HideSuggestions()
    {
        Suggestions.style.display = DisplayStyle.None;
        Debug.Log($"Hide suggestions");
    }

    public void SuggestPrevious()
    {
        Suggestions.Selection--;
    }
    
    public void SuggestNext()
    {
        Suggestions.Selection++;
    }
}

public interface IMakeChipCode
{
    public string Code {get;}
}

public partial class ChipEditorSuggestionsView : VisualElement
{
    public int Selection
    {
        set {
            foreach(var child in this.Children())
                child.RemoveFromClassList("selected");
            _selection = childCount == 0 ? -1 : value % childCount;
            if(_selection >= 0)
                this[_selection].AddToClassList("selected");
        }
        get => _selection;
    }
    int _selection;
}

public class LoopElement : ChipLineBlock
{
    public LoopElement() : base("from")
    {
        Header.Add(new VariableRefElement());
        Header.Add(new Label("to"));
        Header.Add(new VariableRefElement());
    }
    
    public new string Code => "TODO LOOP";
}

[UxmlElement]
public partial class ChipLineBlock : VisualElement, IMakeChipCode
{
    // string name;
    protected VisualElement Header;
    public ChipLineBlock() : this("Unnamed") { }
    public ChipLineBlock(string name)
    {
        this.name = $"{name}_{ChipEditorElement.NextId++}";
        AddToClassList("code-block");
        Add(Header = new VisualElement(){name = "Header"});
        Header.Add(new Label(name));
        var content = new VisualElement();
        Add(content);
        Add(new Button(() => content.Add(new ChipLineElement{Editor = GetFirstOfType<ChipEditorElement>()})){text = "+"});
    }

    public string Code => $"{name}:\n{string.Join('\n', this[1].Children().OfType<IMakeChipCode>().Select(x => x.Code))}";
}

public class VariableRefElement : VisualElement, IMakeChipCode
{
    public bool IsLeftSide
    {
        set => this.Q<SignalButton>().AssignableTo = value 
            ? IdTagManager.ToTag("Equipment") 
            : IdTag.All;
    }
    
    public VariableRefElement()
    {
        Add(new SignalButton());
        Add(new Label("."));
        Add(new TextField(){value = "Value"});
    }
    
    public string Code => "0";
}

public class AssignElement : VisualElement, IMakeChipCode
{
    VariableRefElement lhs, rhs;
    public AssignElement()
    {
        Add(lhs = new VariableRefElement() { IsLeftSide = true });
        Add(new Label("to"));
        Add(rhs = new VariableRefElement());
    }

    public string Code => $"{lhs.Code} = {rhs.Code}";
}

public class CompareElement : VisualElement, IMakeChipCode
{
    public CompareElement()
    {
        Add(new VariableRefElement());
        Add(new Label(">="));
        Add(new VariableRefElement());
    }

    public string Code => @$"self.OpLhs = {"TODOLHS"}
self.OpRhs = {"TODORHS"}
self.Operator = {">="}";
}

public class IfElement : VisualElement, IMakeChipCode
{
    ChipLineBlock Else, Then;
    int SelfId;
    public IfElement()
    {
        SelfId = ChipEditorElement.NextId++;
        Add(new CompareElement());
        Add(Then = new ChipLineBlock("Then"));
        var addElse = new Button(){text = "Add else-block"};
        Else = new ChipLineBlock("Else");
        Else.style.display = DisplayStyle.None;
        Add(addElse);
        Add(Else);
        
        addElse.clickable = new Clickable(_ =>
        {
            addElse.style.display = DisplayStyle.None;
            Else.style.display = DisplayStyle.Flex; 
        });
    }

    public string Code => @$"If_{SelfId}:
{this.Q<CompareElement>().Code}
self.NextLineConditional = @{Then.name}
self.NextLine = @{Else.name}
{Then.Code}
self.NextLine = @EndIf_{SelfId}
{Else.Code}
EndIf_{SelfId}:";
}