using Identifiables;
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Hash128 = Unity.Entities.Hash128;

[CustomPropertyDrawer(typeof(StateChangeDataAuthoring))]
public class StateChangeAuthoringEditor : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var elm = new VisualElement();
        var l1 = new VisualElement();
        l1.style.flexDirection = FlexDirection.Row;
        var reactionToggle = new DropdownField(new List<string> { "Phase Change", "Reaction" }, 
            property.FindPropertyRelative("Catalyst").objectReferenceValue != null ? "Reaction" : "Phase Change");
        l1.Add(reactionToggle);
        var belowAbove = new DropdownField(new List<string> { "Below", "Above" }, property.FindPropertyRelative("Below").boolValue ? "Below" : "Above");
        belowAbove.RegisterValueChangedCallback(x =>
        {
            property.serializedObject.Update();
            property.FindPropertyRelative("Below").boolValue = x.newValue == "Below";
            property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        });
        var temperature = new FloatField() { bindingPath = "Temperature" };
        l1.Add(belowAbove);
        l1.Add(temperature);
        l1.Add(new Label("\u00b0C"));
        elm.Add(l1);
        
        var reaction = new VisualElement();
        var r1 = new VisualElement();
        r1.style.marginLeft = 11;
        r1.style.flexDirection = FlexDirection.Row;
        r1.Add(new IntegerField() { bindingPath = nameof(StateChangeDataAuthoring.SelfMolesPerResultMole) });
        r1.Add(new ObjectField {
            objectType = typeof(ElementAuthoring), 
            enabledSelf = false, 
            value = property.serializedObject.targetObject
        });
        reaction.Add(r1);
        var r2 = new VisualElement();
        r2.style.flexDirection = FlexDirection.Row;
        r2.Add(new Label("+ "));
        r2.Add(new IntegerField() { bindingPath = nameof(StateChangeDataAuthoring.CatalystMolesPerResultMole) });
        r2.Add(new ObjectField{allowSceneObjects = false, bindingPath = nameof(StateChangeDataAuthoring.Catalyst), objectType = typeof(ElementAuthoring)});
        reaction.Add(r2);
        var r3 = new VisualElement();
        r3.style.flexDirection = FlexDirection.Row;
        r3.Add(new Label("= "));
        r3.Add(new IntegerField() { value = 1, enabledSelf = false } );
        r3.Add(new ObjectField{allowSceneObjects = false, bindingPath = nameof(StateChangeDataAuthoring.ToElement), objectType = typeof(ElementAuthoring)});
        r3.Add(new Label("+ "));
        r3.Add(new IntegerField() { bindingPath = nameof(StateChangeDataAuthoring.ReactionEnergy) } );
        r3.Add(new Label("kJ"));
        reaction.Add(r3);
        elm.Add(reaction);
        if (reactionToggle.value != "Reaction")
        {
            r2.style.display = DisplayStyle.None;
            r1.Q<IntegerField>().enabledSelf = false;
            property.serializedObject.Update();
            property.FindPropertyRelative(nameof(StateChangeDataAuthoring.SelfMolesPerResultMole)).intValue = 1;
            property.FindPropertyRelative(nameof(StateChangeDataAuthoring.Catalyst)).objectReferenceValue = null;
            property.serializedObject.ApplyModifiedProperties();
        }
        
        reactionToggle.RegisterValueChangedCallback(x =>
        {
            if (x.newValue == "Reaction")
            {
                r2.style.display = DisplayStyle.Flex;
                r1.Q<IntegerField>().enabledSelf = true;
            }
            else
            {
                r2.style.display = DisplayStyle.None;
                r1.Q<IntegerField>().enabledSelf = false;
                property.serializedObject.Update();
                property.FindPropertyRelative(nameof(StateChangeDataAuthoring.SelfMolesPerResultMole)).intValue = 1;
                property.FindPropertyRelative(nameof(StateChangeDataAuthoring.Catalyst)).objectReferenceValue = null;
                property.serializedObject.ApplyModifiedProperties();
            }
        });
        
        return elm;
    }
}

[Serializable]
public struct StateChangeDataAuthoring
{
    [Range(-273.15f, 5000f)]
    public float Temperature;
    public bool Below;
    public bool Above => !Below;
    public ElementAuthoring ToElement;
    [Header("Reaction")]
    public ElementAuthoring Catalyst;
    public int SelfMolesPerResultMole;
    public int CatalystMolesPerResultMole;
    public int ReactionEnergy;
}

public class ElementAuthoring : MonoBehaviour
{
    public StateChangeDataAuthoring[] StateChanges;
    
    // [Range(-273.15f, 5000f)]
    // public float DefaultTemperature;
    
    [Range(0, 1/Element.MAX_CONDUCTIVITY_INVERSE)]
    public float ThermalConductivity;
    public float SpecificHeatCapacity;

    public Color Color;
    
    public MassData ToMassData(Mass mass, Temperature temperature) => new MassData
    {
        ElementType = GetComponent<IdentifieableAuthoring>().Id,
        ElementData = AsCompact,
        Mass = mass,
        Temperature = temperature // Must be assigned after ElementData and Mass 
    };
    
    public ElementDef.Compact AsCompact
    {
        get
        {
            var tempRange = new half2(new half(0), new half(half.MaxValue));
            for (int i = 0; i < StateChanges.Length; i++)
            {
                var stateChange = StateChanges[i];
                if (stateChange.Below && stateChange.Temperature > tempRange.x)
                    tempRange.x = (half)stateChange.Temperature;
                if (!stateChange.Below && stateChange.Temperature < tempRange.y)
                    tempRange.y = (half)stateChange.Temperature;
            }
            var phase = ElementPhase.None;
            foreach (var tag in GetComponent<IdentifieableAuthoring>().Tags)
            {
                phase = tag.name switch
                {
                    "TagSolid" => ElementPhase.Solid,
                    "TagLiquid" => ElementPhase.Liquid,
                    "TagGas" => ElementPhase.Gas,
                    _ => phase
                };
            }

            return new ElementDef.Compact
            {
                ThermalConductivity = new half(ThermalConductivity),
                SpecificHeatCapacity = new half(SpecificHeatCapacity),
                StableTemperatureRangeKelvin = tempRange,
                PressureFromPhase = phase switch
                {
                    ElementPhase.Gas => 100,
                    ElementPhase.Liquid => 10,
                    ElementPhase.Solid => 1,
                    _ => throw new InvalidDataException($"Element {gameObject.name} has no phase tag assigned. Must be either TagSolid, TagLiquid, or TagGas.")
                }
            };
        }
    }
    
    class EBaker : IdTypeBaker<ElementAuthoring>
    {
        public override void Bake(ElementAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);
            if (!TryGetBlobDef<Element, ElementDef>(out var blob, out var blobHash))
            {
                var builder = new BlobBuilder(Allocator.Temp);
                ref var def = ref builder.ConstructRoot<ElementDef>();
                var arrayBuilder = builder.Allocate(ref def.Reactions, authoring.StateChanges.Length);
                for (int i = 0; i < arrayBuilder.Length; i++)
                {
                    arrayBuilder[i] = new StateChange
                    {
                        To = authoring.StateChanges[i].ToElement.GetComponent<IdentifieableAuthoring>().Id,
                        At = authoring.StateChanges[i].Temperature + 273.15f,
                        With = authoring.StateChanges[i].Catalyst != null 
                            ? authoring.StateChanges[i].Catalyst.GetComponent<IdentifieableAuthoring>().Id : default,
                        CheckBelow = authoring.StateChanges[i].Below,
                        SelfPerOne = (byte)math.max(1, authoring.StateChanges[i].SelfMolesPerResultMole),
                        WithPerOne = (byte)math.max(1, authoring.StateChanges[i].CatalystMolesPerResultMole),
                        ReactionEnergy = authoring.StateChanges[i].ReactionEnergy
                    };
                }
                def.CompactData = authoring.AsCompact; 
                blob = builder.CreateBlobAssetReference<ElementDef>(Allocator.Persistent);
                AddBlobAssetWithCustomHash(ref blob, blobHash);
            }
            AddComponent(e, new Element { Data = blob });
        }
    }
}