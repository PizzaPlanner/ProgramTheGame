using System;
using System.Globalization;
using UnityEngine;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;

public sealed class TemperatureConverter : IYamlTypeConverter
{
    public bool Accepts(Type type) => type == typeof(Temperature);

    public object ReadYaml(IParser parser, Type type)
    {
        var value = parser.Consume<Scalar>().Value;
        try
        {
            if (value.ToLower().EndsWith("k"))
                return new Temperature
                    { Kelvin = float.Parse(value[..^1], NumberStyles.Any, CultureInfo.InvariantCulture) };
            if (value.ToLower().EndsWith("c"))
                return new Temperature
                    { Celsius = float.Parse(value[..^1], NumberStyles.Any, CultureInfo.InvariantCulture) };
            if (value.ToLower().EndsWith("f"))
                return new Temperature
                    { Fahrenheit = float.Parse(value[..^1], NumberStyles.Any, CultureInfo.InvariantCulture) };
            return new Temperature { Kelvin = float.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture) };
        }
        catch (FormatException)
        {
            Debug.LogWarning($"Error parsing Blueprint: Expected Temperature, got '{value}'");
            return default(Temperature);
        }
    }
    
    public void WriteYaml(IEmitter emitter, object value, Type type)
    {
        emitter.Emit(new Scalar(((Temperature)value).ToString()));
    }
}