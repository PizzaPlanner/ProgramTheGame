using Equipment;
using Identifiables;
using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace Units
{
    public class SelectionWheel : VisualElement
    {
        EntityManager _entityManager;
        Entity _origin, _target;

        public SelectionWheel()
        {
            focusable = true;
        }
        
        public static void ShowWheel(Entity target, Entity origin, ref SystemState state)
        {
            if(!state.EntityManager.HasBuffer<ToDos>(origin) 
               || !state.EntityManager.HasComponent<CanReceivePlayerOrders>(origin))
                return;
            
            var wheel = UISystemGroup.Show<SelectionWheel>();
            if (wheel == null)
            {
                wheel = new SelectionWheel();
                UISystemGroup.AddOverlay(wheel);
                UISystemGroup.Show(wheel);
                wheel.style.position = Position.Absolute;
                wheel.RegisterCallback<FocusOutEvent>(_ => { UISystemGroup.Hide(wheel); });
            }

            wheel.Clear();
            wheel.transform.position = RuntimePanelUtils.ScreenToPanel(wheel.panel, new float2(Input.mousePosition.x - 20, Screen.height - Input.mousePosition.y - 20));
            wheel._entityManager = state.EntityManager;
            wheel._origin = origin;
            wheel._target = target;
            if (state.EntityManager.HasComponent<Infestable>(target))
                wheel.Add(new SelectionWheelButton("Infest", wheel.OnClick));
            if (state.EntityManager.HasComponent<Mineable>(target))
                wheel.Add(new SelectionWheelButton("Mine", wheel.OnClick));
            if (state.EntityManager.HasComponent<UnderConstruction>(target))
                wheel.Add(new SelectionWheelButton("BuildTask", wheel.OnClick));
            wheel.Add(new SelectionWheelButton("Approach", wheel.OnClick));

            if (wheel.childCount == 0)
            {
                Debug.Log($"Schade, nothing to do on {target}");
                UISystemGroup.Hide(wheel);   
            }
            else
                wheel.Focus();
        }

        void OnClick(Identifieable id)
        {
            var todo = _entityManager.Instantiate(id.SpawnablePrefab);
            _entityManager.SetComponentData(todo, new Task
            {
                Priority = TaskPriority.DirectCommand,
                Target = _target,
                Executor = _origin,
                Origin = _origin,
                IsPersonalized = true
            });
            _entityManager.GetBuffer<ToDos>(_origin).Insert(0, new ToDos()
            {
                Status = TaskOutcome.Requested,
                Task = todo
            });
            UISystemGroup.Hide(this);
        }
    }

    public class SelectionWheelButton : Label
    {
        public SelectionWheelButton(string taskId, Action<Identifieable> onClick)
        {
            text = taskId;
            RegisterCallback<MouseDownEvent>(_ => onClick(Identifieable.FromName(taskId)));
        }
    }
}