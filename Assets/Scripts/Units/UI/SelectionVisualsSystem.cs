using System;
using Units;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;
using UnityEngine.UIElements;

[UpdateInGroup(typeof(UISystemGroup))]
partial class SelectionVisualsSystem : SystemBase
{
    SelectionMarker _marker;
    Entity _lastTarget;
    
    protected override void OnCreate()
    {
        _marker = new SelectionMarker(); 
        RequireForUpdate<Selected>();
    }

    protected override void OnStartRunning()
    {
        UISystemGroup.AddOverlay(_marker);
        UISystemGroup.Show(_marker);
        _marker.SendToBack();
    }

    protected override void OnStopRunning()
    {
        _marker.RemoveFromHierarchy();
    }

    protected override void OnUpdate()
    {
        var selected = SystemAPI.GetSingletonEntity<Selected>();
        if (selected != _lastTarget)
        {
            _marker.Reset(selected);
            _lastTarget = selected;
        }
        var aabb = new MinMaxAABB { Min = float.MaxValue, Max = float.MinValue};
        foreach (var (linkedEntities, root)
                 in SystemAPI.Query<DynamicBuffer<LinkedEntityGroup>>().WithAll<Selected>().WithEntityAccess())
        {
            foreach (var link in linkedEntities)
            {
                var e = link.Value;
                if (!SystemAPI.HasComponent<WorldRenderBounds>(e) || SystemAPI.HasComponent<Disabled>(e))
                    continue;
                aabb.Encapsulate(SystemAPI.GetComponent<WorldRenderBounds>(e).Value);
            }
        }
        _marker.Update(aabb);
    }
}

public class SelectionMarker : VisualElement
{
    public SelectionMarker()
    {
        style.position = Position.Absolute;
        var box = new VisualElement { pickingMode = PickingMode.Ignore };
        Add(box);
        box.Add(new Label { name = "Down", pickingMode = PickingMode.Ignore });
        box.Add(new VisualElement { name = "Up", pickingMode = PickingMode.Ignore });
        pickingMode = PickingMode.Ignore;
    }

    public void Reset(Entity selected)
    {
        this[0].style.width = 0;
        this[0].style.height = 0;
        this.Q<Label>().text = $"ID_{selected.ToString().ToUpper()}";
    }
    
    public void Update(AABB aabb)
    {
        Span<float3> p = stackalloc[]
        {
            new float3(aabb.Min.x, aabb.Min.y, aabb.Min.z),
            new float3(aabb.Max.x, aabb.Min.y, aabb.Min.z),
            new float3(aabb.Min.x, aabb.Max.y, aabb.Min.z),
            new float3(aabb.Max.x, aabb.Max.y, aabb.Min.z),
            new float3(aabb.Min.x, aabb.Min.y, aabb.Max.z),
            new float3(aabb.Max.x, aabb.Min.y, aabb.Max.z),
            new float3(aabb.Min.x, aabb.Max.y, aabb.Max.z),
            new float3(aabb.Max.x, aabb.Max.y, aabb.Max.z)
        };
        
        var min = (float2)float.MaxValue;
        var max = (float2)float.MinValue;
        for (int i = 0; i < 8; i++)
        {
            var s = (float2)RuntimePanelUtils.CameraTransformWorldToPanel(panel, p[i], Camera.main); 
            min = math.min(min, s);
            max = math.max(max, s);
        }
            
        var center = (float2)RuntimePanelUtils.CameraTransformWorldToPanel(panel, aabb.Center, Camera.main);
        var panelSize = new float2(parent.resolvedStyle.width, parent.resolvedStyle.height);
        style.left = math.clamp(center.x, 0, panelSize.x);
        style.top  = math.clamp(center.y, 0, panelSize.y);
        var box = this[0];
        box.style.width = max.x - min.x + 5;
        box.style.height = math.lerp(box.style.height.value.value, max.y - min.y + 5, 0.1f);
    }
}