using Units;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

partial class LogicChipUISystem : UISystem
{
    class LogicChipUI : VisualElement
    {
        Label StateInfo;
        Label Edit;
        public LogicChipUI()
        {
            Add(StateInfo = new Label(){name = "Status"});
            Add(Edit = new Label("Edit"){name = "Edit"});
            
            Edit.RegisterCallback<MouseDownEvent>(OnEdit);
        }

        void OnEdit(MouseDownEvent evt)
        {
            if(LogicChip == null)
                return;
            
            UISystemGroup.ShowMenu<ChipEditorElement>().Import(LogicChip);
        }

        public ChipState ChipState
        {
            set => StateInfo.text = $"{value.NextLine} // {value.OpResult}";
        }

        public LogicChip LogicChip;
    }
    
    ChipEditorElement editor;
    
    protected override void OnCreate()
    {
        editor = new ChipEditorElement();
        editor.styleSheets.Add(Addressables.LoadAssetAsync<StyleSheet>("UI/Stylesheets/ChipEditor.uss").WaitForCompletion());
        editor.OnSaveScript = OnScriptSaved;
        UISystemGroup.AddOverlayWithMenuEntry("ChipEditor", editor);
    }

    LogicChip editedScript;
    void OnScriptSaved(LogicChip obj)
    {
        editedScript = obj;   
    }

    protected override void OnUpdate()
    {
        foreach (var (chip, state, ui, e) in SystemAPI.Query<LogicChip, ChipState, SelectionUI>().WithEntityAccess())
        {
            if(ui.UI is not EquippedItemActionsElement parent)
                continue;
            var window = parent.Q<LogicChipUI>();
            if (window == null)
            {
                window = new LogicChipUI();
                parent.Add(window);
            }
            window.LogicChip = chip;
            window.ChipState  = state;
        }
        
        if(editedScript == null)
            return;

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (chip, e) in SystemAPI.Query<LogicChip>().WithEntityAccess())
        {
            if(chip.Location != editedScript.Location)
                continue;
            if(SystemAPI.HasBuffer<ChipInstructions>(e))
                ecb.RemoveComponent<ChipInstructions>(e);
        }
        ecb.Playback(EntityManager);
        editedScript = null;
        
    } 
}