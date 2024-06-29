using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

namespace Core
{
    public abstract partial class MapOverlaySystem<T> : HotkeyToggleUISystem<VisualElement, T> where T : unmanaged, IMapOverlay
    {
        protected sealed override VisualElement MakeWindow() => null;
        protected virtual string TextureProperty => "_Data0";

        GameObject _overlay;
        Texture2D _texture;
        GridBounds _lastGridBounds;
        protected override void OnStartRunning()
        {
            _overlay = Addressables.InstantiateAsync("GasOverlayPlane").WaitForCompletion();
            var gridEntity = SystemAPI.GetSingletonEntity<CurrentGrid>();
            var bounds = SystemAPI.GetComponent<GridStructure>(gridEntity).LocalAABB; 
            RecreateTexture(bounds);
        }
        
        protected override void OnStopRunning()
        {
            GameObject.Destroy(_overlay);
        }

        void RecreateTexture(GridBounds bounds)
        {
            _texture = new Texture2D(bounds.Scale.x, bounds.Scale.y, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };
            _lastGridBounds = bounds;
            _overlay.GetComponent<MeshRenderer>().material.SetTexture(TextureProperty, _texture);
            _overlay.transform.localScale = new Vector3(bounds.Scale.x * 0.1f, 1, bounds.Scale.y * 0.1f);
        }
        
        protected override void OnUpdate()
        {
            var gridEntity = SystemAPI.GetSingletonEntity<CurrentGrid>();
            var bounds = SystemAPI.GetComponent<GridStructure>(gridEntity).LocalAABB;
            if(math.any(bounds.Scale != _lastGridBounds.Scale))
                RecreateTexture(bounds);
            var ltw = SystemAPI.GetComponent<LocalToWorld>(gridEntity);
            _overlay.transform.position = GridStructure.LocalToWorld(ltw, bounds.Center);
            _overlay.transform.rotation = ltw.Rotation;
            
            SetPixels(gridEntity, _texture, bounds);
            _texture.Apply();
        }
        
        protected abstract void SetPixels(Entity gridEntity, Texture2D texture, GridBounds bounds);
    }
}