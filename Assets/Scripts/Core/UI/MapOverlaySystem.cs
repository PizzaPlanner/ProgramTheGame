using Core.Input;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

namespace Core
{
    public abstract partial class MapOverlaySystem<T> : HotkeyToggleUISystem<VisualElement, T> where T : unmanaged, IInputActionState
    {
        static readonly int MarkerLocation = Shader.PropertyToID("_MarkerLocation");
        static readonly int BorderTime = Shader.PropertyToID("_BorderTime");
        static readonly int WireTime = Shader.PropertyToID("_WireTime");
        protected virtual string TextureProperty => "_Data0";

        GameObject _overlay;
        Texture2D _texture;
        GridBounds _lastGridBounds;
        Entity _lastGrid;
        bool _isBorderAnimDone;

        protected override void OnStopRunning()
        {
            base.OnStopRunning();
            _lastGrid = Entity.Null;
            GameObject.Destroy(_overlay);
        }

        void RecreateTexture(GridBounds bounds)
        {
            if (_overlay == null)
                _overlay = Addressables.InstantiateAsync("GasOverlayPlane").WaitForCompletion();
            _texture = new Texture2D(bounds.Scale.x, bounds.Scale.y, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };
            _lastGridBounds = bounds;
            _overlay.GetComponent<MeshRenderer>().material.SetTexture(TextureProperty, _texture);
            _overlay.transform.localScale = new Vector3(bounds.Scale.x * 0.1f, 1, bounds.Scale.y * 0.1f);
            _overlay.GetComponent<MeshRenderer>().materials[2].SetFloat(BorderTime, 0);
            _isBorderAnimDone = false;
        }

        protected void SetOverlayMarkerLocation(int2 location)
        {
            _overlay.GetComponent<MeshRenderer>().materials[1].SetVector(MarkerLocation, new Vector2(location.x, location.y));
        }
        
        protected GridBounds Bounds => _lastGridBounds;
        protected Entity GridEntity => _lastGrid;
        protected int2 CellAtCursor;
        
        protected override void OnUpdate()
        {
            var gridEntity = SystemAPI.GetSingleton<CurrentGrid>().Grid;
            if (!SystemAPI.HasComponent<GridStructure>(gridEntity))
            {
                _lastGrid = Entity.Null;
                return;
            }
            
            var bounds = SystemAPI.GetComponent<GridStructure>(gridEntity).LocalAABB;
            if(math.any(bounds.Scale != _lastGridBounds.Scale) || _lastGrid != gridEntity)
                RecreateTexture(bounds);
            _lastGrid = gridEntity;
            if (!_isBorderAnimDone)
            {
                var bTime = _overlay.GetComponent<MeshRenderer>().materials[2].GetFloat(BorderTime) + SystemAPI.Time.DeltaTime * 4;
                bTime = math.clamp(bTime, 0, 1);
                _overlay.GetComponent<MeshRenderer>().materials[2].SetFloat(BorderTime, bTime);
                _overlay.GetComponent<MeshRenderer>().materials[1].SetFloat(WireTime, bTime);
                if(bTime >= 1)
                    _isBorderAnimDone = true;
            }
                
            var ltw = SystemAPI.GetComponent<LocalToWorld>(gridEntity);
            _overlay.transform.position = GridStructure.LocalToWorld(ltw, bounds.Center);
            _overlay.transform.rotation = ltw.Rotation;
            
            SetPixels(gridEntity, _texture, bounds);
            _texture.Apply();
            
            CellAtCursor = GridStructure.CursorToCell(ltw);
            var mouseLocationRelative = CellAtCursor - bounds.Min;
            SetOverlayMarkerLocation(mouseLocationRelative);
        }
        
        protected abstract void SetPixels(Entity gridEntity, Texture2D texture, GridBounds bounds);
    }
}