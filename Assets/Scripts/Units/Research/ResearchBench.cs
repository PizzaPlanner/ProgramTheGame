using Identifiables;
using Unity.Collections;
using Unity.Entities;

namespace Units.Research
{
    public struct ResearchBench : IComponentData
    {
        public FixedList64Bytes<Identifiable> AvailableResearchTasks;
    }
}