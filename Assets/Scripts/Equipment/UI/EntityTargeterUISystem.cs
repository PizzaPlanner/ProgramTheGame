using Equipment;
using Identifiables.UI;
using Units;
using Unity.Entities;
using UnityEngine.UIElements;

[UpdateInGroup(typeof(UISystemGroup))]
partial class EntityTargeterUISystem : SystemBase
{
    protected override void OnUpdate()
    {
        foreach (var (targeter, ui, e) in SystemAPI.Query<EntityTargeter, SelectionUI>().WithEntityAccess())
        {
            if(ui.UI is not EquippedItemActionsElement parent)
                continue;
            var window = parent.Q<EntityTargeterElement>();
            if (window == null)
            {
                window = new EntityTargeterElement();
                parent.Add(window);
            }
            window.Update(targeter.Target, EntityManager);
        }
    }
}

public class EntityTargeterElement : VisualElement
{
    public EntityTargeterElement()
    {
        Add(new SignalButton());
    }

    public void Update(Entity target, EntityManager em)
    {
        Target = target;
        this.Q<SignalButton>().Target = em.HasComponent<Identifieable>(target) ? em.GetComponentData<Identifieable>(target) : default;
        this.Q<SignalButton>().Entity = target;
    }
    
    Entity _target;
    public Entity Target
    {
        set
        {
            if(_target == value)
                return;
            _target = value;
        }
        get => _target;
    } 
}