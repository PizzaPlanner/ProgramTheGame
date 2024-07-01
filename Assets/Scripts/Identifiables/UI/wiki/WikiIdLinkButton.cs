using UnityEngine.UIElements;

namespace Identifiables.UI
{
    public class WikiIdLinkButton : VisualElement
    {
        public WikiIdLinkButton(IdentifiableMappingSystem.IdentifiableLink link, bool withName)
        {
            if(withName)
                AddToClassList("inline");
            else
                Add(new Label{text = link.Relation.ToString(), name = "Relation"});
            Add(new WikiSearchResultButton(link.To));
        }
    }
}