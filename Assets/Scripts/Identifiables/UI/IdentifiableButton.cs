using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Identifiables.UI
{
    [UxmlElement]
    public partial class IdentifiableButton : VisualElement
    {
        public IdentifiableButton()
        {
            Add(new VisualElement{name = "Icon"});
            Add(new VisualElement{name = "Frame"});
            Add(new Label());
            RegisterCallback<MouseEnterEvent>(x => RegisterCallbackOnce<KeyDownEvent>(ShowWiki));
            RegisterCallback<MouseLeaveEvent>(x => UnregisterCallback<KeyDownEvent>(ShowWiki));
            AddToClassList("id-button");
        }

        void ShowWiki(KeyDownEvent evt)
        {
            if(evt.keyCode != KeyCode.F1 || Target == default)
                return;
            UISystemGroup.Show<Wiki>().OpenTab(Target);
        }

        Identifieable _id;
        public Identifieable Target
        {
            set
            {
                this.Q("Icon").style.backgroundImage = value.Icon;
                _id = value;
            } 
            get => _id;
        }
    }
}