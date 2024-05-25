using Unity.Entities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

namespace Identifiables.UI
{
    [UpdateInGroup(typeof(UISystemGroup))]
    public partial class WikiSystem : SystemBase
    {
        protected override void OnCreate()
        {
            var wiki = new Wiki();
            wiki.styleSheets.Add(Addressables.LoadAssetAsync<StyleSheet>("UI/Stylesheets/Wiki.uss").WaitForCompletion());
            wiki.Init(World);
            UISystemGroup.AddOverlayWithMenuEntry("Wiki", wiki);
            wiki.RegisterCallback<AttachToPanelEvent>(_ => wiki.Init(World));
        }

        protected override void OnUpdate() { }
    }
}