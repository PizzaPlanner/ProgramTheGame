using Unity.Entities;
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