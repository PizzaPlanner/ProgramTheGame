using Identifiables;
using Units.Research;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class ResearchBenchAuthoring : MonoBehaviour
{
    public TaskAuthoring[] ResearchTasks;
    class Baker : Baker<ResearchBenchAuthoring>
    {
        public override void Bake(ResearchBenchAuthoring authoring)
        {
            var inputs = new FixedList64Bytes<Identifiable>();
            inputs.Length = authoring.ResearchTasks.Length;
            for (int i = 0; i < authoring.ResearchTasks.Length; i++)
                inputs[i] = authoring.ResearchTasks[i].GetComponent<IdentifieableAuthoring>().Id;
            AddComponent(GetEntity(TransformUsageFlags.Renderable), new ResearchBench
            {
                AvailableResearchTasks = inputs
            });
        }
    }
}