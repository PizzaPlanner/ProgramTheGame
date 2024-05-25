using System;
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
                        var details = s.ProvideWikiDetails(page.Content);
                        if(details != null)
                            page.AddDetails(details);
                    };

            this.Q<LandingPage>().Init();
            didInit = Identifieable.All.Length > 0;
        }
        
        Action<DetailPage> _onOpenDetails;
    }
}