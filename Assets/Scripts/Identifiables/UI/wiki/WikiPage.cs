using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Identifiables.UI
{
    [UxmlElement]
    public partial class Wiki : VisualElement
    {
        public Wiki()
        {
            var landing = new LandingPage();
            var search = new TextField{name = "Search"};
            search.RegisterCallback<InputEvent>(x =>
            {
                landing.Search(x.newData.Trim());
            });
            var tabs = new TabView();
            var landingTab = new Tab("Home");
            landingTab.closeable = false;
            landingTab.Add(landing);
            tabs.Add(landingTab);
            Add(search);
            Add(tabs);
        }
        
        public void OpenTab(Identifieable id)
        {
            var openTabs = this.Q<TabView>().Query<DetailPage>().Where(x => x.Content == id);
            if (openTabs.ToList().Count == 0)
            {
                var tab = new Tab(id.DefaultName.Value);
                tab.closeable = true;
                tab.closed += (e) => e.RemoveFromHierarchy();
                var details = new DetailPage { Content = id };
                tab.Add(details);
                _onOpenDetails?.Invoke(details);
                this.Q<TabView>().Add(tab);
                this.Q<TabView>().activeTab = tab;
            }
            else
                this.Q<TabView>().activeTab = openTabs.First().GetFirstAncestorOfType<Tab>();
        }

        bool didInit;
        public void Init(World world)
        {
            if(didInit)
                return;
            
            _onOpenDetails = null;
            foreach(var mappingSystem in world.GetExistingSystemManaged<IdentifiablesMappingGroup>().ManagedSystems)
                if(mappingSystem is IdentifiableMappingSystem s)
                    _onOpenDetails += page =>
                    {
                        var details = s is IWikiProvider wp ? wp.ProvideWikiDetails(page.Content) : DefaultWikiDetails(page.Content, s.Relations);
                        if(details != null)
                            page.AddDetails(details);
                    };

            this.Q<LandingPage>().Init();
            didInit = Identifieable.All.Length > 0;
        }

        static VisualElement DefaultWikiDetails(Identifieable id, Dictionary<Identifieable, List<IdentifiableMappingSystem.IdentifiableLink>> Relations)
        {
            if(!Relations.TryGetValue(id, out var relations))
                return null;
            var perRelationLookup = new Dictionary<string, VisualElement>();
            var details = new VisualElement();
            foreach (var relation in relations)
            {
                if (!perRelationLookup.TryGetValue(relation.Relation, out var foldout))
                {
                    foldout = new Foldout{name = relation.Relation, text = relation.Relation};
                    perRelationLookup[relation.Relation] = foldout;
                    details.Add(foldout);
                }
                foldout.Add(new WikiSearchResultButton(relation.To));
            }
            return details;
        }

        Action<DetailPage> _onOpenDetails;
    }
}