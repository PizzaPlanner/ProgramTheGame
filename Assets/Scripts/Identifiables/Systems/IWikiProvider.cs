using UnityEngine.UIElements;

namespace Identifiables.UI
{
    public interface IWikiProvider
    {
        VisualElement ProvideWikiDetails(Identifiable id);
    }
}