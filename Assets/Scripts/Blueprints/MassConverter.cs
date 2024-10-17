using System;
using System.Globalization;
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
            return (Mass)(uint)(float.Parse(value[..^2], NumberStyles.Any, CultureInfo.InvariantCulture) * 1000u);
        if(value.ToLower().EndsWith("t"))
            return (Mass)(uint)(float.Parse(value[..^1], NumberStyles.Any, CultureInfo.InvariantCulture) * 1000u * 1000u);
        if(value.ToLower().EndsWith("g"))
            return (Mass)(uint)(float.Parse(value[..^1], NumberStyles.Any, CultureInfo.InvariantCulture));
        return (Mass)(uint)float.Parse(value);
    }
    
    public void WriteYaml(IEmitter emitter, object value, Type type)
    {
        emitter.Emit(new Scalar(((Mass)value).ToString()));
    }
}