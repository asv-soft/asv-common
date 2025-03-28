namespace Asv.Drones.Gui.Plugin.Afis;

public static class Tolerance
{
    public static Tolerance<double> DoubleNan { get; } = new(double.NaN, double.NaN);
}

public class Tolerance<T>(T lower, T upper)
    where T : struct
{
    public T Upper { get; init; } = upper;
    public T Lower { get; init; } = lower;

    public override string ToString()
    {
        return $"{Upper:F2}\u00f7{Lower:F2}";
    }
}
