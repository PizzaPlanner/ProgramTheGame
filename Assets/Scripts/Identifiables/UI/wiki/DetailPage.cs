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
            // header.Add(new VisualElement(){ name = "Tags" });
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
                
                var body = this.Q("Body");
                body.Q<Label>("IdRef").text = $"Id:   <link=\"id\">{value.StableId.ToString()}</link>\nName: <link=\"name\">{value.DefaultName.Value}</link>";
                body.Q<Label>("Description").text = "lorem ispum\nlorem ipsum\nHello World?";
                var header = this.Q("Header");
                header.Q<Label>("Title").text = $"{value.DefaultName.Value}";
                header.Q("Icon").style.backgroundImage = value.Icon;
                // foreach(var tag in Tag.AllTagsForId(value))
                //     header.Q("Tags").Add(new WikiSearchResultButton(tag.Id));
                    
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