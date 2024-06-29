using Identifiables;
using Identifiables.UI;
using System;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Units
{
    [UxmlElement]
    public partial class UnitGroupElement : VisualElement
    {
        VisualElement groupPanel;

        public UnitGroupElement()
        {
            groupPanel = new VisualElement(){name = "Units"};
            Add(groupPanel);
        }
        
        public void SetGroup(DynamicBuffer<UnitGroup> selection, ref SystemState state)
        {
            if (groupPanel.childCount != selection.Length)
            {
                groupPanel.Clear();
                foreach (var e in selection)
                    groupPanel.Add(new IdentifiableButton());
            }
            
            style.display = selection.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            
            if (selection.Length > 1)
            {
                
                for (int i = 0; i < selection.Length; i++)
                {
                    var id = state.EntityManager.GetComponentData<Identifieable>(selection[i].Unit);
                    (groupPanel[i] as IdentifiableButton)!.Target = id;
                }
                groupPanel.style.display = DisplayStyle.Flex;
            }
            else
                groupPanel.style.display = DisplayStyle.None;
        }
    }
}