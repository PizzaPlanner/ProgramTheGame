using System;
using System.Reflection;
using Unity.Entities;
using UnityEngine;

#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(TypeWrapper))]
public class TypeWrapperDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var requiredType = fieldInfo.GetCustomAttribute<RestrictToAttribute>()?.BaseType;
        var dropdown = new PopupField<Type>{label = property.name};
        dropdown.value = TypeWrapper.From(property.FindPropertyRelative("_hash").ulongValue);
        dropdown.choices = TypeManager.AllTypes
                                      .Select(x => TypeManager.GetType(x.TypeIndex))
                                      .Where(x => requiredType == null || requiredType.IsAssignableFrom(x))
                                      .ToList();
        dropdown.RegisterValueChangedCallback(change =>
        {
           property.serializedObject.Update();
           property.FindPropertyRelative("_hash").ulongValue = ((TypeWrapper)change.newValue).Value;
           property.serializedObject.ApplyModifiedProperties();
        });
        return dropdown;
    }
}
#endif

public class RestrictToAttribute : Attribute
{
    public Type BaseType;
    public RestrictToAttribute(Type type)
    {
        BaseType = type;
    }
}

[Serializable]
public class TypeWrapper
{
    [SerializeField] ulong _hash;
    public static implicit operator TypeWrapper(Type type) => new TypeWrapper{_hash = TypeHash.CalculateStableTypeHash(type)};
    public static implicit operator Type(TypeWrapper wrapper) => TypeManager.GetType((TypeIndex)wrapper);
    public static implicit operator TypeIndex(TypeWrapper wrapper) => TypeManager.GetTypeIndexFromStableTypeHash(wrapper._hash);
    
    public static TypeWrapper From(ulong typeHash) => new TypeWrapper{_hash = typeHash};
    public ulong Value => _hash;
}