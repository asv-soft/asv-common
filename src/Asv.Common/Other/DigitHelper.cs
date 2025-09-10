using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Asv.Common;

public static class DigitHelper
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountDecDigits(this int value)
    {
        return value switch
        {
            0 => 1,
            < 0 => CountDecDigits((uint)-value) + 1,
            _ => CountDecDigits((uint)value),
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountDecDigits(this uint value)
    {
        // Algorithm based on https://lemire.me/blog/2021/06/03/computing-the-number-of-digits-of-an-integer-even-faster.
        ReadOnlySpan<long> table =
        [
            4294967296,
            8589934582,
            8589934582,
            8589934582,
            12884901788,
            12884901788,
            12884901788,
            17179868184,
            17179868184,
            17179868184,
            21474826480,
            21474826480,
            21474826480,
            21474826480,
            25769703776,
            25769703776,
            25769703776,
            30063771072,
            30063771072,
            30063771072,
            34349738368,
            34349738368,
            34349738368,
            34349738368,
            38554705664,
            38554705664,
            38554705664,
            41949672960,
            41949672960,
            41949672960,
            42949672960,
            42949672960,
        ];
        Debug.Assert(
            table.Length == 32,
            "Every result of uint.Log2(value) needs a long entry in the table."
        );

        // TODO: Replace with table[uint.Log2(value)] once https://github.com/dotnet/runtime/issues/79257 is fixed
        var tableValue = Unsafe.Add(ref MemoryMarshal.GetReference(table), uint.Log2(value));
        return (int)((value + tableValue) >> 32);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountHexDigits(this int value)
    {
        return CountHexDigits((uint)value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CountHexDigits(this uint value)
    {
        if (value == 0)
        {
            return 1;
        }

        var bits = BitOperations.Log2(value) + 1;
        return (bits + 3) / 4;
    }
}
