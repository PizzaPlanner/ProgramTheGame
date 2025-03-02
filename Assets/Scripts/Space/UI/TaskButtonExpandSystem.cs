using Identifiables;
using System;
using System.Collections;
using System.Collections.Generic;
using Tasks.UI;
using Units;
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

            foreach (var (taskProviders, e) in
                     SystemAPI
                         .Query<DynamicBuffer<TaskProvider>>()
                         .WithAll<Selected>().WithEntityAccess())
            {
                var actions = ui.Q<Foldout>("Actions");
                if(actions == null)
                    continue;

                foreach (var button in actions.Query<CreateTaskButton>().Build())
                {
                    button.UpdateConfigs(EntityManager, taskProviders);
                }
            }
        }
    }
}