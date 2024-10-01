using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

namespace Identifiables.UI
{
    public class DetailPage : ScrollView
    {
        public DetailPage()
        {
            var header = new VisualElement{name = "Header"};
            header.Add(new VisualElement {name = "Icon"});
            header.Add(new Label{name = "Title"});
            Add(header);
            var body = new VisualElement{name = "Body"};
            body.Add(new Label{name = "Description"});
            body.Add(new Label{name = "IdRef"});
            body.Q("IdRef").RegisterCallback<PointerUpLinkTagEvent>(evt => GUIUtility.systemCopyBuffer = evt.linkText);
            Add(body);
            var refs  = new VisualElement{name = "References"};
            Add(refs);
        }
        
        Identifiable _content;
        public Identifiable Content
        {
            set
            {
                this.Q<Label>("Title").text = $"Translate({value.DefaultName.Value})";
                this.Q<Label>("Description").text = "lorem ispum\nlorem ipsum\nHello World?";
                this.Q<Label>("IdRef").text = $"Id:   <link=\"id\">{value.StableId.ToString()}</link>\nName: <link=\"name\">{value.DefaultName.Value}</link>";
                this.Q("Icon").style.backgroundImage = value.Icon;
                    
                _content = value;
            }
            get => _content;
        }

        public void AddDetails(VisualElement details)
        {
            this.Q("Body").Add(details);
        }
    }
}