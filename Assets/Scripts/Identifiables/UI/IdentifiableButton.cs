using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Identifiables.UI
{
    public class Tooltip : VisualElement
    {
        
    }
    
    [UxmlElement]
    public partial class IdentifiableButton : VisualElement
    {
        public IdentifiableButton()
        {
            Add(new VisualElement{name = "Icon"});
            Add(new VisualElement{name = "Frame"});
            Add(new Label());
            // RegisterCallback<MouseEnterEvent>(x =>
            // {
            //     RegisterCallback<KeyDownEvent>(ShowWiki, TrickleDown.TrickleDown);
            //     if(Target == default)
            //         return;
            //     // this.schedule.Execute(() =>
            //     // {
            //     //     
            //     // }).StartingIn(200);
            //     // focusable = true;
            //     // Focus();
            // });
            // RegisterCallback<MouseLeaveEvent>(x =>
            // {
            //     UnregisterCallback<KeyDownEvent>(ShowWiki);
            //     schedule.Execute(() =>
            //     {
            //         
            //     }).ExecuteLater(0);
            //     focusable = false;
            // });
            AddToClassList("id-button");
        }

        void ShowWiki(KeyDownEvent evt)
        {
            if(evt.keyCode != KeyCode.F1 || Target == default)
                return;
            UISystemGroup.ShowMenu<Wiki>().OpenTab(Target);
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