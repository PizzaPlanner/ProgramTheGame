using Unity.Mathematics;
using UnityEngine;

public class MoveCommandIndicator : MonoBehaviour
{
    static GameObject _instance;
    void Awake()
    {
        if (_instance == null)
        {
            _instance = gameObject;
            _instance.SetActive(false);
        }
        
    }

    float timer;
    public static void Play(float3 position)
    {
        var obj = GameObject.Instantiate(_instance);
        obj.transform.position = position;
        obj.SetActive(true);
    }

    void Update()
    {
        if(gameObject == _instance)
            return;
        
        timer += Time.deltaTime;
        if(timer > 5)
            Destroy(gameObject);
    }
    
    
}
