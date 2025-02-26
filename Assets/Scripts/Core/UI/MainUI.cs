using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Core
{
    [RequireComponent(typeof(UIDocument))]
    public class MainUI : MonoBehaviour
    {
        static UIDocument _instance;
        public static VisualElement Root => _instance != null ? _instance.rootVisualElement : null;
        void Awake()
        {
            _instance = GetComponent<UIDocument>();
        }

        void OnDisable()
        {
            if(_instance == GetComponent<UIDocument>())
                _instance = null;
        }
    }
}