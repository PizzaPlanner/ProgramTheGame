using UnityEngine.UIElements;

namespace Identifiables.UI
{
    public class WikiTagSearchButton : VisualElement
    {
        public WikiTagSearchButton(string tag)
        {
            var label  = new Label{text = tag};
            Add(label);
            RegisterCallback<MouseDownEvent>(x => GetFirstAncestorOfType<LandingPage>().Search($"tag={tag}"));
        }
    }
}