public struct Temperature
{
    public float Kelvin;

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
        return $"{Kelvin:0.0}K";
    }
}