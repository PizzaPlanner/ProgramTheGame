using System;
using UnityEngine;
using UnityEngine.UIElements;

[UxmlElement]
public partial class ChipLineElement : VisualElement, IMakeChipCode
{
    public bool IsEmpty;
    TextField Inputter;
    public CurrentInputFor InputState;
    public ChipEditorElement Editor;

    ChipOperationDropdown Operation;
    
    public enum CurrentInputFor
    {
        Operation,
        LeftSide,
        RightSide
    }
    
    public int LineNumber
    {
        set
        {
            this.Q<Label>("LineNumber").text = $"{value}:";
            _lineNumber = value;
        }
        get => _lineNumber;
    }
    int _lineNumber;
    
    public ChipLineElement()
    {
        focusable = true;
        Add(new Label(){name = "LineNumber"});
        Add(Operation = new ChipOperationDropdown(OnOperationChanged));
        Add(new VisualElement(){name = "LineContent"});
        // Operation.style.display = DisplayStyle.None;
        // Add(Inputter = new TextField(){name = "Input", selectAllOnFocus = false, selectAllOnMouseUp = false});
        // RegisterCallback<FocusInEvent>(_ => Inputter.Focus());
        // Inputter.RegisterCallback<FocusInEvent>(_ => Editor.ShowSuggestions(Inputter, InputState));
        // Inputter.RegisterCallback<FocusOutEvent>(_ => Editor.HideSuggestions());
        // Inputter.RegisterCallback<InputEvent>(OnInput);
        OnOperationChanged(OperationType.Set);
        RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
    }

    void OnOperationChanged(OperationType evt)
    {
        var content = this.Q("LineContent");
        content.Clear();
        switch (evt)
        {
            case OperationType.Set:
                content.Add(new AssignElement());
                break;
            case OperationType.Compare:
                content.Add(new CompareElement());
                break;
            case OperationType.Calculate:
                content.Add(new CalculationElement());
                break;
            case OperationType.Branch:
                content.Add(new IfElement());
                break;
            case OperationType.Loop:
                content.Add(new LoopElement());
                break;
            case OperationType.Jump:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(evt), evt, null);
        }
    }

    void OnKeyDown(KeyDownEvent evt)
    {
        if (Editor.Suggestions.style.display == DisplayStyle.None)
            return;
        
        if (evt.keyCode is KeyCode.UpArrow)
        {
            Editor.SuggestPrevious();
            evt.StopPropagation();
        }
        if (evt.keyCode is KeyCode.DownArrow)
        {
            Editor.SuggestNext();
            evt.StopPropagation();
        }
        if (evt.keyCode is KeyCode.KeypadEnter or KeyCode.Space or KeyCode.Tab or KeyCode.Return)
        {
            ApplyInput();
            evt.StopPropagation();
        }
        
    }

    void ApplyInput()
    {
        switch (InputState)
        {
            case CurrentInputFor.Operation:
            {
                var selection = Editor.Suggestions.Selection;
                if(selection == -1)
                    return;
                var op = Enum.Parse<OperationType>((Editor.Suggestions[selection] as Label).text);
                Operation.style.display = DisplayStyle.Flex;
                Operation.Q<EnumField>().value = op;
                InputState = CurrentInputFor.LeftSide;
                break;
            }
            case CurrentInputFor.LeftSide:
                break;
            case CurrentInputFor.RightSide:
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        Inputter.value = "";
        Editor.ShowSuggestions(Inputter, InputState);
    }

    void OnInput(InputEvent evt)
    {
        Editor.ShowSuggestions(Inputter, InputState);
    }

    public string Code => this.Q("LineContent")?.childCount > 0 && this.Q("LineContent")[0] is IMakeChipCode chipCode ? chipCode.Code : "-";

    public static ChipLineElement FromLine(CSLineToken line)
    {
        var cle = new ChipLineElement();
        switch (line.LineType)
        {
            case CSLineToken.TokenType.LogicAssignment:
                cle.Operation.Op = OperationType.Compare;
                break;
            case CSLineToken.TokenType.MathAssignment:
                cle.Operation.Op = OperationType.Calculate;
                break;
            case CSLineToken.TokenType.SimpleAssignment:
                cle.Operation.Op = OperationType.Set;
                break;
            // case CSLineToken.TokenType.Label:
            //     block.Add(new ChipLineBlock(line.Token[0].Intermediate));
            //     break;   
        }
        return cle;
    }
}