namespace Asv.IO
{
    public static class BitHelper
    {
        public static uint GetBitU(byte[] buff, uint pos, uint len)
        {
            uint bits = 0;
            uint i;
            for (i = pos; i < pos + len; i++)
                bits = (uint)((bits << 1) + ((buff[i / 8] >> (int)(7 - i % 8)) & 1u));
            return bits;
        }

        public static uint GetBitUReverse(byte[] buff, uint pos, uint len)
        {
            uint bits = 0;
            for (var i = (int)(pos + len) - 1; i >= pos; i--)
                bits = (uint)((bits << 1) + ((buff[i / 8] >> 7 - i % 8) & 1u));
            return bits;
        }

        public static void SetBitU(byte[] buff, uint pos, uint len, uint data)
        {
            var mask = 1u << (int)(len - 1);

            if (len <= 0 || 32 < len) return;

            for (var i = pos; i < pos + len; i++, mask >>= 1)
            {
                if ((data & mask) > 0)
                    buff[i / 8] |= (byte)(1u << (int)(7 - i % 8));
                else
                    buff[i / 8] &= (byte)(~(1u << (int)(7 - i % 8)));
            }
        }

        public static void SetBitUReverse(byte[] buff, uint pos, uint len, uint data)
        {
            var mask = 1u;

            if (len <= 0 || 32 < len) return;

            for (var i = pos; i < pos + len; i++, mask <<= 1)
            {
                if ((data & mask) > 0)
                    buff[i / 8] |= (byte)(1u << (int)(7 - i % 8));
                else
                    buff[i / 8] &= (byte)(~(1u << (int)(7 - i % 8)));
            }
        }

        public static void SetBitU(byte[] buff, uint pos, uint len, double data)
        {
            SetBitU(buff, pos, len, (uint)data);
        }

        public static void SetBitUReverse(byte[] buff, uint pos, uint len, double data)
        {
            SetBitUReverse(buff, pos, len, (uint)data);
        }

        public static int GetBitS(byte[] buff, uint pos, uint len)
        {
            var bits = GetBitU(buff, pos, len);
            if (len <= 0 || 32 <= len || (bits & (1u << (int)(len - 1))) == 0)
                return (int)bits;
            return (int)(bits | (~0u << (int)len)); /* extend sign */
        }

        public static int GetBitSReverse(byte[] buff, uint pos, uint len)
        {
            var bits = GetBitUReverse(buff, pos, len);
            if (len <= 0 || 32 <= len || (bits & (1u << (int)(len - 1))) == 0)
                return (int)bits;
            return (int)(bits | (~0u << (int)len)); /* extend sign */
        }

        public static void SetBitS(byte[] buff, uint pos, uint len, int data)
        {
            if (data < 0)
                data |= 1 << (int)(len - 1);
            else
                data &= ~(1 << (int)(len - 1)); /* set sign bit */
            SetBitU(buff, pos, len, (uint)data);
        }

        public static void SetBitSReverse(byte[] buff, uint pos, uint len, int data)
        {
            if (data < 0)
                data |= 1 << (int)(len - 1);
            else
                data &= ~(1 << (int)(len - 1)); /* set sign bit */
            SetBitUReverse(buff, pos, len, (uint)data);
        }

        public static void SetBitS(byte[] buff, uint pos, uint len, double data)
        {
            SetBitS(buff, pos, len, (int)data);
        }

        public static void SetBitSReverse(byte[] buff, uint pos, uint len, double data)
        {
            SetBitSReverse(buff, pos, len, (int)data);
        }


        public static byte Reverse(this byte src)
        {
            byte result = 0;
            for (byte i = 0; i < 8; i++)
            {
                result = (byte)(result + (((src >> i) & 0x1) << (7 - i)));
            }

            return result;
        }

        public static ushort Reverse(this ushort src)
        {
            ushort result = 0;
            for (byte i = 0; i < 16; i++)
            {
                result = (ushort)(result + (((src >> i) & 0x1) << (15 - i)));
            }

            return result;
        }

        public static uint Reverse(this uint src)
        {
            uint result = 0;
            for (byte i = 0; i < 32; i++)
            {
                result = result + (((src >> i) & 0x1) << (31 - i));
            }

            return result;
        }

        public static int Reverse(this int src)
        {
            var result = 0;
            for (byte i = 0; i < 32; i++)
            {
                result = result + (((src >> i) & 0x1) << (31 - i));
            }

            return result;
        }

        public static ulong Reverse(this ulong src)
        {
            ulong result = 0;
            for (byte i = 0; i < 64; i++)
            {
                result = result + (((src >> i) & 0x1) << (63 - i));
            }

            return result;
        }

    }
}
