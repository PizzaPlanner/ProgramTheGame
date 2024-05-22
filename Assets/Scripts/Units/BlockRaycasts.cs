using UnityEngine;
using UnityEngine.UIElements;

public class BlockRaycasts : MonoBehaviour
{
    
    public static bool BlockIt; 
    
    VisualElement root;
    void Update()
    {
        if (root == null)
        {
            if(!TryGetComponent<UIDocument>(out var doc))
                return;
            root = doc.rootVisualElement;
            root.RegisterCallback<MouseEnterEvent>(evt => BlockIt = true);
            root.RegisterCallback<MouseLeaveEvent>(evt => BlockIt = false); 
        }
    }
}
