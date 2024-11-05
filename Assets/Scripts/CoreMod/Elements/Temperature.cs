using System.Globalization;
using Unity.Mathematics;

public struct Temperature
{
    float _value;

    public float Kelvin
    {
        get => _value;
        set => _value = math.max(0, value);
    }

    public float Celsius
    {
        get => Kelvin - 273.15f;
        set => Kelvin = value + 273.15f;
    }

    public float Fahrenheit
    {
        get => Celsius * 9 / 5 + 32;
        set => Celsius = (value - 32) * 5 / 9;
    }

    public static implicit operator Temperature(float kelvin) => new Temperature { Kelvin = kelvin };
    public static implicit operator float(Temperature t) => t.Kelvin;

    public override string ToString()
    {
        return $"{Celsius.ToString("F1", CultureInfo.InvariantCulture)}C";
    }
}