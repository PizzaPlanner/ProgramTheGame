using Identifiables;
using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

public sealed class IdentifiableConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(Identifiable);

    public object ReadYaml(IParser parser, Type type, ObjectDeserializer od)
    {
        return Identifiable.FromName(parser.Consume<Scalar>().Value);
    }
    
    public void WriteYaml(IEmitter emitter, object value, Type type, ObjectSerializer od)
    {
        emitter.Emit(new Scalar(((Identifiable)value).DefaultName.Value));
    }
}