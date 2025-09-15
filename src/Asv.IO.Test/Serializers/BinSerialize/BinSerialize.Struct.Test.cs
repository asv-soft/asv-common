using System;
using System.Runtime.InteropServices;
using Xunit;

namespace Asv.IO.Test;

public partial class BinSerializeTest
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    private struct TestStruct
    {
        public int A;
        public float B;
        public byte C;
        public uint D;
    }

    [Fact]
    public void StructCanBeSerialized()
    {
        var testStruct = default(TestStruct);
        testStruct.A = 13337;
        testStruct.B = 1337f;
        testStruct.C = 137;
        testStruct.D = 17;

        var buffer = new byte[13];
        var writeSpan = new Span<byte>(buffer);
        BinSerialize.WriteStruct(ref writeSpan, testStruct);

        var readSpan = new ReadOnlySpan<byte>(buffer);
        var readStruct = BinSerialize.ReadStruct<TestStruct>(ref readSpan);

        Assert.Equal(testStruct.A, readStruct.A);
        Assert.Equal(testStruct.B, readStruct.B);
        Assert.Equal(testStruct.C, readStruct.C);
        Assert.Equal(testStruct.D, readStruct.D);
    }

    [Fact]
    public void StructCanBeReserved()
    {
        var buffer = new byte[13];
        var writeSpan = new Span<byte>(buffer);

        ref TestStruct reserved = ref BinSerialize.ReserveStruct<TestStruct>(ref writeSpan);
        reserved.A = 13337;
        reserved.B = 1337f;
        reserved.C = 137;
        reserved.D = 17;

        var readSpan = new ReadOnlySpan<byte>(buffer);
        var readStruct = BinSerialize.ReadStruct<TestStruct>(ref readSpan);

        Assert.Equal(reserved.A, readStruct.A);
        Assert.Equal(reserved.B, readStruct.B);
        Assert.Equal(reserved.C, readStruct.C);
        Assert.Equal(reserved.D, readStruct.D);
    }
}
