namespace Asv.IO;

public static class SimpleBinaryMixin
{
    public static int GetSize<T>(T value, bool skipUnknown = false) where T : IVisitable
    {
        var calculator = new SimpleBinarySizeCalculator(skipUnknown);
        value.Accept(calculator);
        return calculator.Size;
    }
    
}