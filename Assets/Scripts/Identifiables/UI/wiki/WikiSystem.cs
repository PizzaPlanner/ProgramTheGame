using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Identifiables.UI
{
    [UpdateInGroup(typeof(UISystemGroup))]
    public partial class WikiSystem : SystemBase
    {
        Wiki _wiki;

        protected override void OnUpdate()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                if (_wiki == null)
                {
                    _wiki = new Wiki();
                    _wiki.Init(World);
                }
                if(_wiki.parent == null)
                    UISystemGroup.Root.Add(_wiki);
                else
                    _wiki.RemoveFromHierarchy();
            }
        }
    }
}