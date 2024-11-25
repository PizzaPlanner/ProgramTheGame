using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

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
        ElementData = ToElement(Allocator.Temp).AsCompact,
        Mass = mass,
        Temperature = temperature
    };

    public Element ToElement(Allocator allocator)
    {
            var tags = GetComponent<IdentifieableAuthoring>().Tags;
            var phase = ElementPhase.None;
            foreach (var tag in tags)
            {
                phase = tag.name switch
                {
                    "TagSolid" => ElementPhase.Solid,
                    "TagLiquid" => ElementPhase.Liquid,
                    "TagGas" => ElementPhase.Gas,
                    _ => phase
                };
            }
            if(phase == ElementPhase.None)
                throw new InvalidDataException($"Element {gameObject.name} has no phase tag assigned. Must be either TagSolid, TagLiquid, or TagGas.");
            
            var blobBuilder = new BlobBuilder(Allocator.Temp);
            ref var blob = ref blobBuilder.ConstructRoot<StateChangeData>();
            var arrayBuilder = blobBuilder.Allocate(ref blob.Reactions, StateChanges.Length);
            for (int i = 0; i < arrayBuilder.Length; i++)
            {
                arrayBuilder[i] = new Element.StateChange
                {
                    To = StateChanges[i].ToElement.GetComponent<IdentifieableAuthoring>().Id,
                    At = StateChanges[i].Temperature + 273.15f,
                    With = StateChanges[i].Catalyst != null 
                        ? StateChanges[i].Catalyst.GetComponent<IdentifieableAuthoring>().Id : default,
                    CheckBelow = StateChanges[i].Below,
                    SelfPerOne = (byte)math.max(1, StateChanges[i].SelfMolesPerResultMole),
                    WithPerOne = (byte)math.max(1, StateChanges[i].CatalystMolesPerResultMole),
                    ReactionEnergy = StateChanges[i].ReactionEnergy
                };
            }
            var blobAsset = blobBuilder.CreateBlobAssetReference<StateChangeData>(allocator);
            return new Element
            {
                ThermalConductivity = ThermalConductivity,
                SpecificHeatCapacity = SpecificHeatCapacity,
                StateChanges = blobAsset,
                Phase = phase
            };
    }
    
    class EBaker : Baker<ElementAuthoring>
    {
        public override void Bake(ElementAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.None);
            var elm = authoring.ToElement(Allocator.Persistent);
            AddComponent(e, elm);
            AddBlobAsset(ref elm.StateChanges, out _);
        }
    }
}