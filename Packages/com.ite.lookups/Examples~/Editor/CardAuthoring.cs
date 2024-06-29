using UnityEngine;

namespace Bakers
{
    // In this example the Authoring Data is a MonoBehaviour,
    // but it could also be a ScriptableObject or just a class with [System.Serializable]
    public class CardAuthoring : MonoBehaviour
    {
        public byte Cost;
        public int[] Values;
    }
}