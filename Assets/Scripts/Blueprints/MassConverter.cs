using System;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

public sealed class MassConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(Mass);

    public object ReadYaml(IParser parser, Type type)
    {
        var value = parser.Consume<Scalar>().Value;
        if(value.ToLower().EndsWith("kg"))
            return (Mass)(uint.Parse(value[..^2]) * 1000);
        if(value.ToLower().EndsWith("t"))
            return (Mass)(uint.Parse(value[..^1]) * 1000 * 1000);
        if(value.ToLower().EndsWith("g"))
            return (Mass)(uint.Parse(value[..^1]));
        return (Mass)(uint.Parse(value));
    }
    
    public void WriteYaml(IEmitter emitter, object value, Type type)
    {
        emitter.Emit(new Scalar(((Mass)value).ToString()));
    }
}