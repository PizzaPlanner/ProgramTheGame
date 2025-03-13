using Identifiables;
using Unity.Entities;

namespace Units.Research
{
    public struct ResearchTask : IComponentData
    {
        public ResearchState State;
        public Identifiable Ingredient;
    }
    
    public enum ResearchState
    {
        NoIngredients,
        ResearchMismatch,
        OrderIngredients,
        Work
    }
}