using Identifiables;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Profiling;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;
using YamlDotNet.Serialization.NamingConventions;

namespace Grid.Blueprints
{
    internal class BlueprintV4DataStructure
    {
        
        public string Name;
        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitDefaults)]
        public IList<Identifiable> Tags = new List<Identifiable>();
        [YamlMember(DefaultValuesHandling = DefaultValuesHandling.OmitDefaults, ScalarStyle = ScalarStyle.Folded)]
        public IDictionary<Identifiable, IList<PartData>> Spawns = new Dictionary<Identifiable, IList<PartData>>();

        internal struct PartData
        {
            public int2 At;
            public Mass Mass;
            public Identifiable Matter;
            public Temperature Temperature;
            // public List<InventoryData> Inventory;
        }

        internal class InventoryData
        {
            public Identifiable Item;
            public Mass Mass;
            public Temperature Temperature;
        }
    }

    public struct BlueprintParserV4 : IBlueprintParser
    {
        public string Save(Entity grid, EntityManager em)
        {
            var data = new BlueprintV4DataStructure();
            
            var children = em.GetBuffer<Child>(grid, true);
            foreach (var child in children)
                HandleChild(child, em, data);
            if(em.HasComponent<MassModel>(grid))
                HandleAtmosphere(em.GetComponentData<MassModel>(grid), data);
            
            var builder = new SerializerBuilder()
                          .WithNamingConvention(PascalCaseNamingConvention.Instance)
                          .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults |
                                                          DefaultValuesHandling.OmitEmptyCollections)
                          .WithEventEmitter(x => new FlowArraysEmitter(x))
                          .WithTypeConverter(new MassConverter())
                          .WithTypeConverter(new TemperatureConverter())
                          .WithTypeConverter(new IdentifiableConverter())
                          .WithTypeConverter(new Vec2Converter());
            return builder.Build().Serialize(data);
        }

        void HandleAtmosphere(MassModel massModel, BlueprintV4DataStructure data)
        {
            for (int x = massModel.Min.x; x <= massModel.Max.x; x++)
            for (int y = massModel.Min.y; y <= massModel.Max.y; y++)
                foreach (var mass in massModel.AbsoluteCell(new int2(x, y)).Masses)
                {
                    if(!data.Spawns.ContainsKey(mass.ElementType))
                        data.Spawns[mass.ElementType] = new List<BlueprintV4DataStructure.PartData>();
                    data.Spawns[mass.ElementType].Add(new BlueprintV4DataStructure.PartData
                    {
                        Temperature = mass.Temperature,
                        At = new int2(x, y),
                        Mass = mass.Mass,
                    });
                    
                }
        }

        void HandleChild(Child child, EntityManager em, BlueprintV4DataStructure data)
        {
            if (!em.HasComponent<BuildingDef>(child.Value) 
                || em.HasComponent<Ghost>(child.Value) 
                || em.HasComponent<UnderConstruction>(child.Value))
                return;
            var id = em.GetComponentData<Identifiable>(child.Value);
            var mass = em.GetComponentData<MassData>(child.Value);
            var bounds = em.GetComponentData<BakedIntoRoomMap>(child.Value);
            var defaultMass = em.GetComponentData<MassData>(id.SpawnableDef());
            
            if(!data.Spawns.ContainsKey(id))
                data.Spawns[id] = new List<BlueprintV4DataStructure.PartData>();
            var partToAdd = new BlueprintV4DataStructure.PartData
            {
                At = bounds.Offset,
                Temperature = !Mathf.Approximately(mass.Temperature, defaultMass.Temperature) ? mass.Temperature : default,
                Mass = mass.Mass != defaultMass.Mass ? mass.Mass : default,
                Matter = mass.ElementType != defaultMass.ElementType ? mass.ElementType : default 
            };
            data.Spawns[id].Add(partToAdd);
        }

        public void Load(string data, NativeList<BlueprintContents> content)
        {
            var builder = new DeserializerBuilder()
                          .WithNamingConvention(PascalCaseNamingConvention.Instance)
                          .WithTypeConverter(new MassConverter())
                          .WithTypeConverter(new TemperatureConverter())
                          .WithTypeConverter(new IdentifiableConverter())
                          .WithTypeConverter(new Vec2Converter());

            Profiler.BeginSample($"Deserialize Blueprint");
            // TODO: This is super costly for long files
            var blueprint = builder.Build().Deserialize<BlueprintV4DataStructure>(data);
            Profiler.EndSample();
            Profiler.BeginSample("Load Blueprint");
            foreach (var cell in blueprint.Spawns)
                foreach(var part in cell.Value)
                {
                    content.Add(new BlueprintContents
                    {
                        Prefab = cell.Key,
                        Location = part.At,
                        Matter = new DefaultMatterOverride
                        {
                            Id = part.Matter,
                            Temperature =  part.Temperature,
                            Mass =  part.Mass
                        },
                        
                    });
                }
            Profiler.EndSample();
        }

        public string VersionIdentifier => "# Blueprint Version 4";
    }
}

public class FlowArraysEmitter : ChainedEventEmitter
{
    public FlowArraysEmitter(IEventEmitter nextEmitter) : base(nextEmitter) { }

    bool ShouldFlow(Type type) => false;

    bool ShouldBeImplicit(Type type) => false;

    public override void Emit(ScalarEventInfo eventInfo, IEmitter emitter)
    {
        if(ShouldFlow(eventInfo.Source.Type))
            eventInfo.Style = ScalarStyle.Plain;
        if (ShouldBeImplicit(eventInfo.Source.Type))
            eventInfo.IsPlainImplicit = true;
        base.Emit(eventInfo, emitter);
    }

    public override void Emit(MappingStartEventInfo eventInfo, IEmitter emitter)
    {
        if(ShouldFlow(eventInfo.Source.Type))
            eventInfo.Style = MappingStyle.Flow;
        if (ShouldBeImplicit(eventInfo.Source.Type))
            eventInfo.IsImplicit = true;
        base.Emit(eventInfo, emitter);
    }

    public override void Emit(SequenceStartEventInfo eventInfo, IEmitter emitter)
    {
        if(ShouldFlow(eventInfo.Source.Type))
            eventInfo.Style = SequenceStyle.Flow;
        if (ShouldBeImplicit(eventInfo.Source.Type))
            eventInfo.IsImplicit = true;
        nextEmitter.Emit(eventInfo, emitter);
    }
}