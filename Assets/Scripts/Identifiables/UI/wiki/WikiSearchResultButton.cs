using System;
using System.Linq;
using Unity.Entities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using SerializedObject = UnityEditor.SerializedObject;

namespace Identifiables.UI
{
    public class WikiSearchResultButton : VisualElement
    {
        Identifiable _target;
        public WikiSearchResultButton(Identifiable target)
        {
            _target = target;
            var icon = new VisualElement{name = "Icon"};
            var texture = target.BaseDef().Icon;
            if(texture != default)
                icon.style.backgroundImage = texture.Value;
            Add(icon);
            RegisterCallback<MouseDownEvent>(x => GetFirstAncestorOfType<Wiki>().OpenTab(target));
            Add(new Label{text = target.DefaultName.Value});
            ref var tags = ref target.BaseDef().Tags;
            for (var i = 0; i < tags.Length; i++)
            {
                var tag = tags[i];
                AddToClassList(tag.DefaultName.Value.ToLower());
            }
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