using Unity.Collections;
using Unity.Entities;
using UnityEngine.UIElements;

namespace Identifiables.UI
{
    public class LandingPage : ScrollView
    {
        VisualElement quickNav, searches;
        Tab searchTab;
        Label searchHint;
        public LandingPage()
        {
            quickNav = new VisualElement() { name = "navigate" };
            Add(new Label($"Welcome to the Wiki"){name = "Title"});
            Add(new Label($"0 Entries loaded"){name = "EntriesHint"});
            Add(quickNav);
            
            searchTab = new Tab("Find");
            searches = new ScrollView();
            searchTab.Add(searches);
            searchHint = new Label();
            searches.Add(searchHint);
            searches.Add(new VisualElement(){ name = "navigate" });
        }

        public void Init(EntityQuery allIdsQuery)
        {
            var target = searches.Q("navigate");
            foreach (var id in allIdsQuery.ToComponentDataArray<Identifiable>(Allocator.Temp))
            {
                target.Add(new WikiSearchResultButton(id));
            }

            foreach (var tag in TagDef.Id.All)
            {
                quickNav.Add(new WikiSearchResultButton(tag));
            }
            GetFirstAncestorOfType<TabView>().activeTabChanged += (x,y)
                => {
                if(y != searchTab)
                    searchTab.RemoveFromHierarchy();
            };
            this.Q<Label>("EntriesHint").text = $"{allIdsQuery.CalculateEntityCount()} Entries loaded";
            ResetSearch();
        }

        public void ResetSearch()
        {
            searchTab.RemoveFromHierarchy();
            GetFirstAncestorOfType<TabView>().activeTab = GetFirstAncestorOfType<Tab>();
        }

        public void Search(string content)
        {
            GetFirstAncestorOfType<TabView>().Add(searchTab);
            GetFirstAncestorOfType<TabView>().activeTab = searchTab;
            searchHint.text = content.Length > 2 ? $"Searching for '{content}'" : "Search string is too short";
            searches.Query<WikiSearchResultButton>().ForEach(x => x.ApplySearch(content));
        }
    }
}