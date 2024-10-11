using Grid.Blueprints;
using Identifiables;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.EventEmitters;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.ObjectGraphVisitors;

namespace Grid.Blueprints
{
    internal class BlueprintV4DataStructure
    {
        public string Name;
        public IList<string> Tags = new List<string>();
        public IList<PartData> Buildings = new List<PartData>();
        public IList<AtmoData> Atmosphere = new List<AtmoData>();

        internal class PartData
        {
            public int2 At;
            public Identifiable Type;
            public Identifiable Material;
            public uint Mass;
            public float Temperature;
        }

        internal class AtmoData
        {
            public int2 At;
            public Identifiable Type;
            public uint Mass;
            public float Temperature;
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
                          .WithTypeConverter(new IdentifiableConverter())
                          .WithTypeConverter(new Vec2Converter());
            return builder.Build().Serialize(data);
        }

        void HandleAtmosphere(MassModel massModel, BlueprintV4DataStructure data)
        {
            for (int x = massModel.Min.x; x <= massModel.Max.x; x++)
            for (int y = massModel.Min.y; y <= massModel.Max.y; y++)
                foreach (var mass in massModel.AbsoluteCell(new int2(x, y)).Masses)
                    data.Atmosphere.Add(new BlueprintV4DataStructure.AtmoData()
                    {
                        At = new int2(x, y),
                        Temperature = mass.Temperature,
                        Type = mass.ElementType,
                        Mass = mass.Mass,
                    });
        }

        void HandleChild(Child child, EntityManager em, BlueprintV4DataStructure data)
        {
            if (!em.HasComponent<Building>(child.Value) 
                || em.HasComponent<Ghost>(child.Value) 
                || em.HasComponent<UnderConstruction>(child.Value))
                return;
            var id = em.GetComponentData<Identifiable>(child.Value);
            var mass = em.GetComponentData<MassData>(child.Value);
            var bounds = em.GetComponentData<GridBounds>(child.Value);
            
            var partToAdd = new BlueprintV4DataStructure.PartData
            {
                At = bounds.Min,
                Type = id,
                Material = mass.ElementType,
                Temperature = mass.Temperature
            };
            data.Buildings.Add(partToAdd);
        }

        public void Load(string data, EntityManager em, Entity prefab)
        {
            var builder = new DeserializerBuilder()
                          .WithNamingConvention(PascalCaseNamingConvention.Instance)
                          .WithTypeConverter(new IdentifiableConverter())
                          .WithTypeConverter(new Vec2Converter());

            var blueprint = builder.Build().Deserialize<BlueprintV4DataStructure>(data);
            var content = em.GetBuffer<BlueprintContents>(prefab);
            foreach (var entry in blueprint.Buildings)
            {
                content.Add(new BlueprintContents{ Data = new PlannedConstruction
                {
                    Prefab = entry.Type,
                    Location = entry.At,
                    Materials = new ConstructionVariant
                    {
                        BuildMaterials = new FixedList64Bytes<ConstructionVariant.Material>()
                        {
                            new() { Item = entry.Material, Amount = entry.Mass }
                        }
                    },
                }});
            }
        }

        public string VersionIdentifier => "# Blueprint Version 4";
    }
}

public class FlowArraysEmitter : ChainedEventEmitter
{
    public FlowArraysEmitter(IEventEmitter nextEmitter) : base(nextEmitter) { }

    bool ShouldFlow(Type type) => type == typeof(int[]) 
                                  || type == typeof(uint[]) 
                                  || type == typeof(byte[]);

    bool ShouldBeImplicit(Type type) => type == typeof(int2);

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