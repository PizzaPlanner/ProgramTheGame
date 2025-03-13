using Identifiables;
using System;
using System.Collections;
using System.Collections.Generic;
using Tasks.UI;
using Units;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Space.Systems
{
    partial class TaskButtonExpandSystem : UISystem
    {
        protected override void OnUpdate()
        {
            if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                return;

            var selectedQuery = GetEntityQuery(typeof(TaskProvider), typeof(Selected));
            foreach (var entity in selectedQuery.ToEntityArray(Allocator.Temp))
            {
                var taskProviders = SystemAPI.GetBuffer<TaskProvider>(entity);
                var actions = ui.Q<Foldout>("Actions");
                if(actions == null)
                    continue;

                foreach (var button in actions.Query<CreateTaskButton>().Build())
                    button.UpdateConfigs(EntityManager, taskProviders);
            }
        }
    }
}