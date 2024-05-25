using Identifiables.UI;
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
    
    public ChipScript EditedScript;
    
    public ChipEditorElement()
    {
        Suggestions = new ChipEditorSuggestionsView();
        Suggestions.style.position = Position.Absolute;
        Suggestions.style.top = 0;
        Suggestions.style.left = 0;
        var topRow = new VisualElement(){name = "Header"};
        Add(topRow);
        topRow.Add(new Button(() => Debug.Log(CollectPseudoCode())){text = "Debug print"});
        topRow.Add(new Button(() =>
        {
            if(EditedScript == null)
                return;
            
            EditedScript.Script = CollectPseudoCode();
            OnSaveScript?.Invoke(EditedScript);
        }){text = "Save"});
        topRow.Add(new Button(() => {  }){text = "Save As"});
        topRow.Add(new Button(() => UISystemGroup.Hide(this)){text = "x", name = "Close"});
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

    public Action<ChipScript> OnSaveScript;

    public void Import(ChipScript script)
    {
        var block = this.Q<ChipLineBlock>();
        block.Clear();
        var parser = new ChipScriptParser();
        parser.Parse(script);

        foreach (var line in parser.Token)
        {
            block.Add(ChipLineElement.FromLine(line));
        }
        EditedScript = script;
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

    public new void Clear()
    {
        this[2].Clear();
    }

    public string Code => $"{name}:\n{string.Join('\n', this[1].Children().OfType<IMakeChipCode>().Select(x => x.Code))}";
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
    VariableRefElement result, lhs, rhs;
    DropdownField comparison;
    public CompareElement()
    {
        Add(new Label("set"));
        Add(result = new VariableRefElement() { IsLeftSide = true});
        Add(new Label("to 1 if"));
        Add(lhs = new VariableRefElement());
        Add(comparison = new DropdownField() { choices = new[]
        {
            "==",
            "!=",
            ">",
            ">=",
            "<",
            "<="
        }.ToList()});
        Add(rhs = new VariableRefElement());
        Add(new Label("(or to 0 otherwise)"));
    }

    public string Code => @$"{result.Code} = {lhs.Code} {comparison.value} {rhs.Code}";
}

public class CalculationElement : VisualElement, IMakeChipCode
{
    VariableRefElement result, lhs, rhs;
    DropdownField operation;
    public CalculationElement()
    {
        Add(result = new VariableRefElement() { IsLeftSide = true});
        Add(new Label("="));
        Add(lhs = new VariableRefElement());
        Add(operation = new DropdownField() { choices = new[]
        {
            "+",
            "-",
            "*",
            "/",
            "%"
        }.ToList()});
        Add(rhs = new VariableRefElement());
    }

    public string Code => @$"{result.Code} = {lhs.Code} {operation.value} {rhs.Code}";
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