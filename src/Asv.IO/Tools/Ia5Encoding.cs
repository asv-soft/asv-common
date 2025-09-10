using System;
using System.Text;

namespace Asv.IO
{
    public static class Ia5Encoding
    {
        /// <summary>
        /// Code ID. 24 bit (up to 4 letters)
        /// https://en.wikipedia.org/wiki/T.50_(standard)
        /// </summary>
        /// <param name="data">Code ID</param>
        /// <returns></returns>
        public static double Encode(string data)
        {
            data ??= string.Empty;
            if (data.Length > 4)
                throw new ArgumentOutOfRangeException(
                    $"Param {nameof(data)} must be less than or equal to 4 letters. {nameof(data)} = {data.Length}"
                );

            var length = 4;

            data = data.Trim();
            var whSpCnt = length - data.Length < 0 ? 0 : length - data.Length;
            var buffer = new byte[length];

            var strBytes = Encoding.ASCII.GetBytes(data);

            for (var i = 0; i < whSpCnt; i++)
            {
                buffer[i] = 0x20;
            }

            for (var i = whSpCnt; i < length; i++)
            {
                buffer[i] = (byte)(strBytes[i - whSpCnt] & 0x3F);
            }

            uint result = 0;

            result |= buffer[buffer.Length - 1];
            for (var i = buffer.Length - 2; i >= 0; i--)
            {
                result <<= 6;
                result |= buffer[i];
            }

            return result;
        }
    }
}
