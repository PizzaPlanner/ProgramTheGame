using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

#if UNITY_EDITOR
public class IdentifiableThingAsset : IdentifiableAsset
{
    [HideInInspector] public IdentifieableAuthoring Reference;

    void OnValidate()
    {
        if(name == Reference.gameObject.name)
            return;
        name = Reference.gameObject.name;
        AssetDatabase.Refresh();
    }
}
#endif