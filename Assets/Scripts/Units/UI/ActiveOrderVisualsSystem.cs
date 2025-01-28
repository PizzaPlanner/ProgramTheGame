using Identifiables;
using Identifiables.UI;
using System;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.UIElements;

[UpdateInGroup(typeof(UISystemGroup))]
partial class ActiveOrderVisualsSystem : SystemBase
{
    protected override void OnStopRunning()
    {
        base.OnStopRunning();
        EntityManager.RemoveComponent<AOMComp>(GetEntityQuery(typeof(AOMComp)));
    }

    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (linkedEntities, tasks, root)
                 in SystemAPI.Query<DynamicBuffer<LinkedEntityGroup>, DynamicBuffer<TaskProvider>>().WithAll<AOMComp>().WithEntityAccess())
        {
            if (tasks.Length == 0 || !SystemAPI.HasComponent<Identifiable>(tasks[0].Task))
            {
                ecb.RemoveComponent<AOMComp>(root);
                continue;
            }
            
            var aabb = new MinMaxAABB { Min = float.MaxValue, Max = float.MinValue };
            foreach (var link in linkedEntities)
            {
                var e = link.Value;
                if (!EntityManager.HasComponent<WorldRenderBounds>(e) || SystemAPI.HasComponent<Disabled>(e)) 
                    continue;
                aabb.Encapsulate(EntityManager.GetComponentData<WorldRenderBounds>(e).Value);
            }
            EntityManager.GetComponentObject<AOMComp>(root).Marker.Update(aabb, SystemAPI.GetComponent<Identifiable>(tasks[0].Task));
        }
        
        foreach (var (tasks, root)
                 in SystemAPI.Query<DynamicBuffer<TaskProvider>>().WithNone<AOMComp>().WithAll<LocalTransform>().WithEntityAccess())
        {
            if (tasks.Length == 0)
                continue;
            
            var marker = new ActiveOrderMarker();
            UISystemGroup.AddOverlay(marker);
            marker.style.display = DisplayStyle.Flex;
            ecb.AddComponent(root, new AOMComp { Marker = marker });
        }
        ecb.Playback(EntityManager);
    }
    
    class AOMComp : IComponentData, IDisposable
    {
        public ActiveOrderMarker Marker;
        public void Dispose()
        {
            Marker.RemoveFromHierarchy();
        }
    } 
}


public class ActiveOrderMarker : VisualElement
{
    IdentifiableButton idButton;
    public ActiveOrderMarker()
    {
        usageHints = UsageHints.DynamicTransform;
        style.position = Position.Absolute;
        idButton = new IdentifiableButton() { pickingMode = PickingMode.Ignore };
        Add(idButton);
        pickingMode = PickingMode.Ignore;
    }
    
    public void Update(AABB aabb, Identifiable task)
    {
        SendToBack();
        style.opacity = 1;
        style.alignItems = Align.Center;
        var center = (float2)RuntimePanelUtils.CameraTransformWorldToPanel(panel, aabb.Center, Camera.main);
        style.left = center.x;
        style.top  = center.y;
        idButton.Target = task;
    }
}