using System;
using System.Linq;
using Unity.Collections;
using UnityEngine.UIElements;

namespace Identifiables.UI
{
    public class WikiSearchResultButton : VisualElement
    {
        Identifiable _target;
        public WikiSearchResultButton(Identifiable target)
        {
            _target = target;
            var icon = new VisualElement{name = "Icon"};
            icon.style.backgroundImage = target.Icon;
            Add(icon);
            RegisterCallback<MouseDownEvent>(x => GetFirstAncestorOfType<Wiki>().OpenTab(target));
            Add(new Label{text = target.DefaultName.Value});
            foreach(var tag in Tag.AllTagsForId(target))
                AddToClassList(tag.Id.DefaultName.Value.ToLower());
        }

        public void ApplySearch(string text)
        {
            text = text.ToLower();
            var isIdSearch = text.StartsWith("id=");
            var isNormalSearch = !text.Contains("=");
            text = text.Trim().Split('=', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            var found = false;
            found |= isIdSearch && text?.Length > 2 && _target.DebugId.ToString().Contains(text);
            found |= isNormalSearch && text?.Length > 2 && _target.DefaultName.ToString().ToLower().Contains(text);
            style.display = found
                ? DisplayStyle.Flex 
                : DisplayStyle.None;
        }
    }
}