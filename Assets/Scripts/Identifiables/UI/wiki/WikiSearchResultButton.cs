using Modding.API;
using Modding.API.Tags;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Identifiables.UI
{
    public class WikiSearchResultButton : VisualElement
    {
        Identifieable _target;
        public WikiSearchResultButton(Identifieable target, bool allowRecurse)
        {
            _target = target;
            var icon = new VisualElement{name = "Icon"};
            icon.style.backgroundImage = target.Icon;
            Add(icon);
            RegisterCallback<MouseDownEvent>(x => GetFirstAncestorOfType<Wiki>().OpenTab(target));
            Add(new Label{text = target.DefaultName.Value});
        }

        public void ApplySearch(string text)
        {
            text = text.ToLower();
            var isIdSearch = text.StartsWith("id=");
            var isNormalSearch = !text.Contains("=");
            text = text.Trim().Split('=', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
            var found = false;
            found |= isIdSearch && text?.Length > 2 && _target.StableId.ToString().Contains(text);
            found |= isNormalSearch && text?.Length > 2 && _target.DefaultName.ToString().ToLower().Contains(text);
            style.display = found
                ? DisplayStyle.Flex 
                : DisplayStyle.None;
        }
    }
}