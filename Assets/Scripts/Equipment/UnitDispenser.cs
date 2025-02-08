using Identifiables;
using Unity.Entities;

namespace Equipment
{
    public struct UnitDispenser : IComponentData
    {
        public Identifiable Unit;
        public Identifiable TaskPrefab;
        public Identifiable StatusAddedOnDispensed;
        public bool IsDispensed;
    }
}