using Grids;
using Identifiables;
using Identifiables.UI;
using Tasks.UI;
using Units;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace Space.Systems
{
    partial class ShipUISystem : UISystem
    {
        Mesh boundsMesh;
        BatchMeshID bmesh;
        Material mat;
        CommandBuffer commandBuffer;
        Entity displayEntity;

        protected override void OnCreate()
        {
            RequireForUpdate(SystemAPI.QueryBuilder().WithAll<Selected, ShipController>().Build());
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            if (displayEntity != default)
            {
                EntityManager.RemoveComponent<Disabled>(displayEntity);
                return;
            }
            
            var graphicsSystem = World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
            boundsMesh = new Mesh();
            mat = Addressables.LoadAssetAsync<Material>("TestMat").WaitForCompletion();

            displayEntity = EntityManager.CreateEntity(typeof(Static));
            #if UNITY_EDITOR
            EntityManager.SetName(displayEntity, "BoundsVisuals");
            #endif
            var filterSettings = RenderFilterSettings.Default;
            filterSettings.MotionMode = MotionVectorGenerationMode.ForceNoMotion;
            filterSettings.ReceiveShadows = false;
            filterSettings.ShadowCastingMode = ShadowCastingMode.Off;
            RenderMeshUtility.AddComponents(displayEntity, EntityManager, new RenderMeshDescription
            {
                FilterSettings  = filterSettings
            }, new MaterialMeshInfo
            {
                MeshID  = graphicsSystem.RegisterMesh(boundsMesh),
                MaterialID = graphicsSystem.RegisterMaterial(mat)
            });
        }

        protected override void OnStopRunning()
        {
            EntityManager.AddComponent<Disabled>(displayEntity);
        }

        protected override void OnUpdate()
        {
            if(!UISystemGroup.TryGet<SelectedUnitElement>(out var ui))
                return;
            
            foreach (var (controller, baked, inShip, taskProviders, e) in 
                     SystemAPI.Query<ShipController, BakedIntoRoomMap, DynamicBuffer<BelongsToShip>, DynamicBuffer<TaskProvider>>()
                              .WithAll<Selected>()
                              .WithNone<Ghost>().WithEntityAccess())
            {
                var stats = ui.GetOrCreate<ShipUI>("Ship");
                CellMesh.GenerateChunkMesh(inShip.Reinterpret<int2>().AsNativeArray(), boundsMesh);
                stats.Q<Label>().text = $"Size: {inShip.Length}";
                var travelBtn = ui.GetOrCreateActionButton(Core.ID.TravelTask, EntityManager, true);
                travelBtn.SetUpdateFunc(OnUpdateTravelUI);
                foreach(var plan in travelBtn.Query<ConfigPanel>().Where(x => x.Q<TravelUI>() == null).Build())
                    plan.Add(new TravelUI(EntityManager, baked.Grid, e));
                SystemAPI.SetComponent(displayEntity, SystemAPI.GetComponent<LocalToWorld>(baked.Grid));
                var grid = SystemAPI.GetComponent<CGrid>(baked.Grid);
                var scale = new float3(grid.BoundsWithBorder.Scale.x, 1000, grid.BoundsWithBorder.Scale.y);
                SystemAPI.SetComponent(displayEntity, new RenderBounds()
                {
                    Value = new AABB
                    {
                        Center = grid.BoundsWithBorder.Center,
                        Extents = scale
                    }
                });
            } 
        }

        void OnUpdateTravelUI(EntityManager em, Task task, ConfigPanel panel)
        {
            panel.Q<TravelUI>().Update(em, task, panel.TaskEntity);
        }
    }

    class TravelUI : VisualElement
    {
        Label Status, Checklist;
        VisualElement PlanUI;
        
        public TravelUI(EntityManager mgr, Entity currentLocation, Entity controller)
        {
            Add(PlanUI = new VisualElement(){name = "Planner"});
            PlanUI.Add(new Label("Select Destination"));
            using var query = mgr.CreateEntityQuery(typeof(Sector), typeof(Identifiable));
            var ids = query.ToComponentDataArray<Identifiable>(Allocator.Temp);
            var entities = query.ToEntityArray(Allocator.Temp);
            for (int i = 0; i < entities.Length; ++i)
            {
                var btn = new IdentifiableButton(){Target = ids[i], enabledSelf = entities[i] != currentLocation};
                var target = entities[i];
                btn.RegisterCallback<MouseDownEvent>((_) =>
                {
                    TravelTask.Create(mgr, target, controller);
                });
                PlanUI.Add(btn);
            }
            Add(Status = new Label());
            Add(Checklist = new Label());
        }

        public void Update(EntityManager entityManager, Task task, Entity taskEntity)
        {
            if(taskEntity == default)
                return;
            var travelTask = entityManager.GetComponentData<TravelTask>(taskEntity);
            Status.text = travelTask.Stage.ToString();
            Checklist.text = $"All Conditions met: Yes";
        }
    }
    
    class ShipUI : VisualElement
    {
        public ShipUI()
        {
            Add(new Label("0"));
        }

        public int Room
        {
            set
            {
                this.Q<Label>().text = $"In Room {value}";
            }
        }
    }
}