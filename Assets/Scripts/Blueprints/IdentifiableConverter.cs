using Identifiables;
using System;
using System.Globalization;
using Unity.Mathematics;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

public sealed class IdentifiableConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(Identifiable);

    public object ReadYaml(IParser parser, Type type)
    {
        return Identifiable.FromName(parser.Consume<Scalar>().Value);
    }
    
    public void WriteYaml(IEmitter emitter, object value, Type type)
    {
        emitter.Emit(new Scalar(((Identifiable)value).DefaultName.Value));
    }
}