using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Identifiables.UI
{
    
    [UxmlElement]
    public partial class IdentifiableButton : VisualElement, ITooltipProvider
    {
        public IdentifiableButton()
        {
            Add(new VisualElement{name = "Icon", pickingMode = PickingMode.Ignore});
            Add(new VisualElement{name = "Frame", pickingMode = PickingMode.Ignore});
            RegisterCallback<MouseDownEvent>(x =>
            {
                if(x.button != 2 || Target == default)
                    return;
                UISystemGroup.ShowMenu<Wiki>().OpenTab(Target);
            });
            AddToClassList("id-button");
            ITooltipProvider.RegisterCallbacks(this);
        }

        Identifiable _id;
        public Identifiable Target
        {
            set
            {
                if(value == _id) return;
                this.Q("Icon").style.backgroundImage = value.BaseDef().Icon.Value;
                _id = value;
            } 
            get => _id;
        }

        public IEnumerable<VisualElement> TooltipContent => new[]
        {
            new TooltipTitle(Target.ToString())
        }.Concat(ClassListContains("cancelable") ? new[]{ new Label("Press LM to cancel")} : Array.Empty<VisualElement>());
    }
}