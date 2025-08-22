using System;

namespace Asv.IO
{
    public  static partial class SpanBitHelper
    {
        

        public static uint GetBitU(ReadOnlySpan<byte> buff,ref int pos, int len)
        {
            uint bits = 0;
            int i;
            for (i = pos; i < pos + len; i++)
                bits = (uint)((bits << 1) + ((buff[(int)(i / 8)] >> (int)(7 - i % 8)) & 1u));
            pos += len;
            return bits;
        }

        public static uint GetBitUReverse(ReadOnlySpan<byte> buff, ref int pos, int len)
        {
            uint bits = 0;
            for (var i = (int)(pos + len) - 1; i >= pos; i--)
                bits = (uint)((bits << 1) + ((buff[i / 8] >> 7 - i % 8) & 1u));
            pos += len;
            return bits;
        }

        public static uint GetBitUReverse(Span<byte> buff, ref int pos, int len)
        {
            uint bits = 0;
            for (var i = (int)(pos + len) - 1; i >= pos; i--)
                bits = (uint)((bits << 1) + ((buff[i / 8] >> 7 - i % 8) & 1u));
            pos += len;
            return bits;
        }

        public static void SetBitU(Span<byte> buff, ref int pos, int len, uint data)
        {
            var mask = 1u << (int)(len - 1);

            if (len <= 0 || 32 < len) return;

            for (var i = pos; i < pos + len; i++, mask >>= 1)
            {
                if ((data & mask) > 0)
                    buff[(int)(i / 8)] |= (byte)(1u << (int)(7 - i % 8));
                else
                    buff[(int)(i / 8)] &= (byte)~(1u << (int)(7 - i % 8));
            }
            pos += len;
        }

        public static void SetBitUReverse(Span<byte> buff, ref int pos, int len, uint data)
        {
            var mask = 1u;

            if (len <= 0 || 32 < len) return;

            for (var i = pos; i < pos + len; i++, mask <<= 1)
            {
                if ((data & mask) > 0)
                    buff[(int)(i / 8)] |= (byte)(1u << (int)(7 - i % 8));
                else
                    buff[(int)(i / 8)] &= (byte)~(1u << (int)(7 - i % 8));
            }
            pos += len;
        }

        public static void SetBitU(Span<byte> buff,ref int pos, int len, double data)
        {
            SetBitU(buff,ref pos, len, (uint)data);
        }

        public static void SetBitUReverse(Span<byte> buff, ref int pos, int len, double data)
        {
            SetBitUReverse(buff, ref pos, len, (uint)data);
        }
       
        public static int GetBitS(ReadOnlySpan<byte> buff, ref int pos, int len)
        {
            var bits = GetBitU(buff,ref pos, len);
            if (len <= 0 || 32 <= len || (bits & (1u << (int)(len - 1))) == 0)
                return (int)bits;
            return (int)(bits | (~0u << (int)len)); /* extend sign */
        }

        public static int GetBitSReverse(ReadOnlySpan<byte> buff, ref int pos, int len)
        {
            var bits = GetBitUReverse(buff,ref  pos, len);
            if (len <= 0 || 32 <= len || (bits & (1u << (int)(len - 1))) == 0)
                return (int)bits;
            return (int)(bits | (~0u << (int)len)); /* extend sign */
        }

        public static void SetBitS(Span<byte> buff,ref int pos, int len, int data)
        {
            if (data < 0)
                data |= 1 << (int)(len - 1);
            else
                data &= ~(1 << (int)(len - 1)); /* set sign bit */
            SetBitU(buff, ref pos, len, (uint)data);
        }

        public static void SetBitSReverse(Span<byte> buff,ref int pos, int len, int data)
        {
            if (data < 0)
                data |= 1 << (int)(len - 1);
            else
                data &= ~(1 << (int)(len - 1)); /* set sign bit */
            SetBitUReverse(buff, ref pos, len, (uint)data);
        }

        public static void SetBitS(Span<byte> buff,ref int pos, int len, double data)
        {
            SetBitS(buff, ref pos, len, (int)data);
        }

        public static void SetBitSReverse(Span<byte> buff,ref int pos, int len, double data)
        {
            SetBitSReverse(buff, ref pos, len, (int)data);
        }

        
    }
}
