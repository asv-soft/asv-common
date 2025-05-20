using System;
using System.Buffers;

namespace Asv.IO;

public static class SimpleBinaryMixin
{
    public static int GetSize<T>(T value, bool skipUnknown = false) where T : IVisitable
    {
        var calculator = new SimpleBinarySizeCalculator(skipUnknown);
        value.Accept(calculator);
        return calculator.Size;
    }
    
    public static void Serialize<T>(T value, IBufferWriter<byte> buffer, bool skipUnknown = false) where T : IVisitable
    {
        var calculator = new SimpleBinarySerialize(buffer, skipUnknown);
        value.Accept(calculator);
    }
    public static void Deserialize<T>(T value, ref ReadOnlyMemory<byte> buffer, bool skipUnknown = false) where T : IVisitable
    {
        var calculator = new SimpleBinaryDeserialize(buffer, skipUnknown);
        value.Accept(calculator);
        buffer = calculator.Memory;
    }
}