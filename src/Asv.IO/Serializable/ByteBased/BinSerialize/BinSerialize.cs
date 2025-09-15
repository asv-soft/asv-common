using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace Asv.IO
{
    public static partial class BinSerialize
    {
        private static readonly Encoding Uft8 = Encoding.UTF8;
        private const int MaxStackStringBytes = 128 * 1024; // 128 KiB.

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowForBigEndian()
        {
            if (!BitConverter.IsLittleEndian)
            {
                throw new NotSupportedException(
                    "BigEndian systems are not supported at this time."
                );
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ToZigZagEncoding(int val)
        {
            /* We encode the integer in such a way that the sign is on the least significant bit,
            known as zig-zag encoding:
            https://en.wikipedia.org/wiki/Variable-length_quantity#Zigzag_encoding */
            return (uint)((val << 1) ^ (val >> 31));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FromZigZagEncoding(uint zigzagged)
        {
            /* We encode integers in such a way that the sign is on the least significant bit,
            known as zig-zag encoding:
            https://en.wikipedia.org/wiki/Variable-length_quantity#Zigzag_encoding */
            return (int)(zigzagged >> 1) ^ -(int)(zigzagged & 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Interpolate(float min, float max, float frac) =>
            min + ((max - min) * Clamp01(frac));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Fraction(float min, float max, float val) =>
            min == max ? 0f : Clamp01((val - min) / (max - min));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Clamp01(float val) =>
            val < 0f ? 0f
            : val > 1f ? 1f
            : val;
    }
}
