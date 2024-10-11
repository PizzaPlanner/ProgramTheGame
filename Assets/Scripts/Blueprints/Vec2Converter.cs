using System;
using System.Globalization;
using Unity.Mathematics;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

public sealed class Vec2Converter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(float2) || type == typeof(int2);

    public object ReadYaml(IParser parser, Type type)
    {
        parser.Consume<SequenceStart>();
        object result = null;
        if (type == typeof(float2))
            result = new float2()
            {
                x = float.Parse(parser.Consume<Scalar>().Value),
                y = float.Parse(parser.Consume<Scalar>().Value)
            };
        else
            result = new int2()
            {
                x = int.Parse(parser.Consume<Scalar>().Value),
                y = int.Parse(parser.Consume<Scalar>().Value)
            };
        parser.Consume<SequenceEnd>();
        return result;
    }
    
    public void WriteYaml(IEmitter emitter, object value, Type type)
    {
        emitter.Emit(new SequenceStart(null, null, false, SequenceStyle.Flow));

        if (type == typeof(float2))
        {
            var v = (float2)value;
            emitter.Emit(new Scalar(v.x.ToString(CultureInfo.InvariantCulture)));
            emitter.Emit(new Scalar(v.y.ToString(CultureInfo.InvariantCulture)));
        }
        else
        {
            var v = (int2)value;
            emitter.Emit(new Scalar(v.x.ToString()));
            emitter.Emit(new Scalar(v.y.ToString()));
        }

        emitter.Emit(new SequenceEnd());
    }
}