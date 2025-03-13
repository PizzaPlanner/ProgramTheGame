using Units;
using Units.Research;
using Unity.Entities;
using UnityEngine;

public class ResearchTaskAuthoring : MonoBehaviour
{
    public ItemAuthoring Researchable; 
    public uint BaseDurationTicks;
    class Baker : Baker<ResearchTaskAuthoring>
    {
        public override void Bake(ResearchTaskAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);
            AddComponent(e, new ResearchTask
            {
                State = ResearchState.NoIngredients,
                Ingredient = authoring.Researchable.Id
            });
            AddComponent(e, new TaskProgress{RequiredTicks = authoring.BaseDurationTicks});
        }
    }
}