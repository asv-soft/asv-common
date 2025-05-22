using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Asv.IO
{
    public static unsafe class BinSerialize
    {
        private static readonly Encoding Uft8 = Encoding.UTF8;
        private static readonly Decoder Utf8decoder = Encoding.UTF8.GetDecoder();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ThrowForBigEndian()
        {
            if (!BitConverter.IsLittleEndian)
                throw new NotSupportedException("BigEndian systems are not supported at this time.");
        }

        #region Read a packed integer

        /// <summary>
        /// Read a packed integer.
        /// </summary>
        /// <param name="span">Span to read from.</param>
        /// <returns>Unpacked integer.</returns>
        public static int ReadPackedInteger(ref ReadOnlySpan<byte> span)
        {
            var zigzagged = ReadPackedUnsignedInteger(ref span);
            return FromZigZagEncoding(zigzagged);
        }
        
        /// <summary>
        /// Read a packed integer. https://en.wikipedia.org/wiki/Variable-length_quantity
        /// </summary>
        public static int ReadPackedInteger(ref ReadOnlyMemory<byte> span)
        {
            var zigzagged = ReadPackedUnsignedInteger(ref span);
            return FromZigZagEncoding(zigzagged);
        }
        
        /// <summary>
        /// Read a packed integer. https://en.wikipedia.org/wiki/Variable-length_quantity
        /// </summary>
        public static void ReadPackedInteger(ref ReadOnlySpan<byte> span, ref int value)
        {
            var zigzagged = ReadPackedUnsignedInteger(ref span);
            value = FromZigZagEncoding(zigzagged);
        }
        /// <summary>
        /// Read a packed integer. https://en.wikipedia.org/wiki/Variable-length_quantity
        /// </summary>
        public static void ReadPackedInteger(ref ReadOnlyMemory<byte> span, ref int value)
        {
            var zigzagged = ReadPackedUnsignedInteger(ref span);
            value = FromZigZagEncoding(zigzagged);
        }
        
        /// <summary>
        /// Read a packed integer. https://en.wikipedia.org/wiki/Variable-length_quantity
        /// </summary>
        public static bool ReadPackedInteger(ref SequenceReader<byte> reader, ref int value)
        {
            var zigzagged = 0U;
            if (TryReadPackedUnsignedInteger(ref reader, ref zigzagged) == false) return false;
            value = FromZigZagEncoding(zigzagged);
            return true;

        }

        #endregion

        #region Read Packed Unsigned Integer

        /// <summary>
        /// Read a packed unsigned integer. https://en.wikipedia.org/wiki/Variable-length_quantity
        /// </summary>
        public static uint ReadPackedUnsignedInteger(ref ReadOnlySpan<byte> span)
        {
            /* Read 7 bits of integer data and then the 8th bit indicates wether more data will follow.
            More info: https://en.wikipedia.org/wiki/Variable-length_quantity */

            uint result = 0;
            var resultBitOffset = 0;
            while (true)
            {
                var data = ReadByte(ref span);

                // Mask of the first 7 bits of the data and then 'apply' it to the result.
                result |= (uint)(data & 0b0111_1111) << resultBitOffset;

                // Check the last bit to see if this was the end.
                if ((data & 0b1000_0000) == 0)
                    break;

                // Increment the offset so the next iteration points at the next bits of the result.
                resultBitOffset += 7;
            }

            return result;
        }
        
        public static uint ReadPackedUnsignedInteger(ref ReadOnlyMemory<byte> span)
        {
            /* Read 7 bits of integer data and then the 8th bit indicates wether more data will follow.
            More info: https://en.wikipedia.org/wiki/Variable-length_quantity */

            uint result = 0;
            var resultBitOffset = 0;
            while (true)
            {
                var data = ReadByte(ref span);

                // Mask of the first 7 bits of the data and then 'apply' it to the result.
                result |= (uint)(data & 0b0111_1111) << resultBitOffset;

                // Check the last bit to see if this was the end.
                if ((data & 0b1000_0000) == 0)
                    break;

                // Increment the offset so the next iteration points at the next bits of the result.
                resultBitOffset += 7;
            }

            return result;
        }
        
        
        
        /// <summary>
        /// Read a packed unsigned integer. https://en.wikipedia.org/wiki/Variable-length_quantity
        /// </summary>
        public static void ReadPackedUnsignedInteger(ref ReadOnlySpan<byte> span, ref uint value)
        {
            /* Read 7 bits of integer data and then the 8th bit indicates wether more data will follow.
            More info: https://en.wikipedia.org/wiki/Variable-length_quantity */

            value = 0;
            var resultBitOffset = 0;
            while (true)
            {
                var data = ReadByte(ref span);

                // Mask of the first 7 bits of the data and then 'apply' it to the result.
                value |= (uint)(data & 0b0111_1111) << resultBitOffset;

                // Check the last bit to see if this was the end.
                if ((data & 0b1000_0000) == 0)
                    break;

                // Increment the offset so the next iteration points at the next bits of the result.
                resultBitOffset += 7;
            }
        }
        
        public static void ReadPackedUnsignedInteger(ref ReadOnlyMemory<byte> span, ref uint value)
        {
            /* Read 7 bits of integer data and then the 8th bit indicates wether more data will follow.
            More info: https://en.wikipedia.org/wiki/Variable-length_quantity */

            value = 0;
            var resultBitOffset = 0;
            while (true)
            {
                var data = ReadByte(ref span);

                // Mask of the first 7 bits of the data and then 'apply' it to the result.
                value |= (uint)(data & 0b0111_1111) << resultBitOffset;

                // Check the last bit to see if this was the end.
                if ((data & 0b1000_0000) == 0)
                    break;

                // Increment the offset so the next iteration points at the next bits of the result.
                resultBitOffset += 7;
            }
        }

        /// <summary>
        /// Read a packed unsigned integer. https://en.wikipedia.org/wiki/Variable-length_quantity
        /// </summary>
        public static bool TryReadPackedUnsignedInteger(ref SequenceReader<byte> reader, ref uint value, out int consumed)
        {
            value = 0;
            consumed = 0;
            var resultBitOffset = 0;
            while (true)
            {
                if (!reader.TryRead(out byte data))
                {
                    value = 0;
                    
                    reader.Rewind(consumed);
                    return false; // Not enough data to read the entire integer
                }

                consumed++;
                value |= (uint)(data & 0b0111_1111) << resultBitOffset;

                if ((data & 0b1000_0000) == 0)
                {
                    return true; // Successfully read the packed unsigned integer
                }

                resultBitOffset += 7;

                // Prevent reading more than 5 bytes (UInt32 max size)
                if (resultBitOffset >= 35)
                {
                    value = 0;
                    reader.Rewind(consumed);
                    return false; // Overflow or malformed data
                }
            }
        }
        /// <summary>
        /// Read a packed unsigned integer. https://en.wikipedia.org/wiki/Variable-length_quantity
        /// </summary>
        public static bool TryReadPackedUnsignedInteger(ref SequenceReader<byte> reader, ref uint value)
        {
            return TryReadPackedUnsignedInteger(ref reader, ref value, out _);
        }

        #endregion

        #region WritePackedInteger

        /// <summary>
        /// Check how many bytes it will take to write the given value as a packed integer.
        /// </summary>
        /// <remarks>
        /// See <see cref="WritePackedInteger"/> for more information (including a size-table).
        /// </remarks>
        /// <param name="value">Value to check.</param>
        /// <returns>Number of bytes it will take.</returns>
        public static int GetSizeForPackedInteger(int value)
        {
            var zigzagged = ToZigZagEncoding(value);
            return GetSizeForPackedUnsignedInteger(zigzagged);
        }

        /// <summary>
        /// Pack a integer and write it.
        /// Uses a variable-length encoding scheme.
        /// </summary>
        /// <remarks>
        /// Size table:
        /// less then -134217729 = 5 bytes
        /// -134217728 to -1048577 = 4 bytes
        /// -1048576 to -8193 = 3 bytes
        /// -8192 to -65 = 2 bytes
        /// -64 to 63 = 1 bytes
        /// 64 to 8191 = 2 bytes
        /// 8192 to 1048575 = 3 bytes
        /// 1048576 to 134217727 = 4 bytes
        /// more then 134217728 = 5 bytes
        /// </remarks>
        /// <param name="span">Span to write to.</param>
        /// <param name="value">Value to pack and write.</param>
        public static void WritePackedInteger(ref Span<byte> span, int value)
        {
            var zigzagged = ToZigZagEncoding(value);
            WritePackedUnsignedInteger(ref span, zigzagged);
        }
        
        public static void WritePackedInteger(ref Memory<byte> span, int value)
        {
            var zigzagged = ToZigZagEncoding(value);
            WritePackedUnsignedInteger(ref span, zigzagged);
        }

        #endregion

        #region WritePackedUnsignedInteger

        /// <summary>
        /// Check how many bytes it will take to write the given value as a packed unsigned integer.
        /// </summary>
        /// <remarks>
        /// See <see cref="WritePackedUnsignedInteger"/> for more information (including a size-table).
        /// </remarks>
        /// <param name="value">Value to check.</param>
        /// <returns>Number of bytes it will take.</returns>
        public static int GetSizeForPackedUnsignedInteger(uint value)
        {
            /* Check how many 7 bit values we need to store the integer, for more info see
            'WritePackedUnsignedInteger' implementation. */

            var bytes = 1;
            while (value > 0b0111_1111)
            {
                value >>= 7;
                bytes++;
            }

            return bytes;
        }

        

        /// <summary>
        /// Pack a unsigned integer and write it.
        /// Uses a variable-length encoding scheme.
        /// </summary>
        /// <remarks>
        /// Size table:
        /// 0 to 127 = 1 bytes
        /// 128 to 16383 = 2 bytes
        /// 16384 to 2097151 = 3 bytes
        /// 2097152 to 268435455 = 4 bytes
        /// more then 268435456 = 5 bytes
        /// </remarks>
        /// <param name="span">Span to write to.</param>
        /// <param name="value">Value to pack and write.</param>
        public static void WritePackedUnsignedInteger(ref Span<byte> span, uint value)
        {
            /* Write 7 bits of integer data and then the 8th bit indicates wether more data will follow.
            More info: https://en.wikipedia.org/wiki/Variable-length_quantity */

            // As long as we have more data left then we can fit into 7 bits we need to 'split' it up.
            while (value > 0b0111_1111)
            {
                // Write out the value and set the 8th bit to 1 to indicate more data will follow.
                WriteByte(ref span, (byte)(value | 0b1000_0000));

                // Shift the value by 7 to 'consume' the bits we've just written.
                value >>= 7;
            }

            // Write out the last data (the 8th bit will always be 0 here to indicate the end).
            WriteByte(ref span, (byte)value);
        }
        
        public static void WritePackedUnsignedInteger(ref Memory<byte> span, uint value)
        {
            /* Write 7 bits of integer data and then the 8th bit indicates wether more data will follow.
            More info: https://en.wikipedia.org/wiki/Variable-length_quantity */

            // As long as we have more data left then we can fit into 7 bits we need to 'split' it up.
            while (value > 0b0111_1111)
            {
                // Write out the value and set the 8th bit to 1 to indicate more data will follow.
                WriteByte(ref span, (byte)(value | 0b1000_0000));

                // Shift the value by 7 to 'consume' the bits we've just written.
                value >>= 7;
            }

            // Write out the last data (the 8th bit will always be 0 here to indicate the end).
            WriteByte(ref span, (byte)value);
        }
        
        public static void WritePackedUnsignedInteger(IBufferWriter<byte> wrt, uint value)
        {
            /* Write 7 bits of integer data and then the 8th bit indicates wether more data will follow.
            More info: https://en.wikipedia.org/wiki/Variable-length_quantity */

            // As long as we have more data left then we can fit into 7 bits we need to 'split' it up.
            while (value > 0b0111_1111)
            {
                // Write out the value and set the 8th bit to 1 to indicate more data will follow.
                WriteByte(wrt, (byte)(value | 0b1000_0000));

                // Shift the value by 7 to 'consume' the bits we've just written.
                value >>= 7;
            }

            // Write out the last data (the 8th bit will always be 0 here to indicate the end).
            WriteByte(wrt, (byte)value);
        }

        #endregion
        
        #region WriteString

        /// <summary>
        /// Check how many bytes it will take to write the given string value.
        /// </summary>
        /// <remarks>
        /// Size will be the length of the string as a 'packed unsigned integer' + the amount of
        /// bytes when the characters are utf-8 encoded.
        /// </remarks>
        /// <param name="val">Value to get the size for.</param>
        /// <returns>Number of bytes it will take.</returns>
        public static int GetSizeForString(string? val)
        {
            if (string.IsNullOrWhiteSpace(val)) return GetSizeForPackedUnsignedInteger(0);
            fixed (char* charPointer = val)
            {
                return GetSizeForString(charPointer, val.Length);
            }
        }

        /// <summary>
        /// Check how many bytes it will take to write the given string.
        /// </summary>
        /// <remarks>
        /// Size will be the length of the span as a 'packed unsigned integer' + the amount of
        /// bytes when the characters are utf-8 encoded.
        /// </remarks>
        /// <param name="val">Value to get the size for.</param>
        /// <returns>Number of bytes it will take.</returns>
        public static int GetSizeForString(ReadOnlySpan<char> val)
        {
            fixed (char* charPointer = val)
            {
                return GetSizeForString(charPointer, val.Length);
            }
        }

        /// <summary>
        /// Check how many bytes it will take to write the given string.
        /// Make sure the data behind the pointer is pinned before calling this.
        /// </summary>
        /// <remarks>
        /// Size will be the charCount as a 'packed unsigned integer' + the amount of
        /// bytes when the characters are utf-8 encoded.
        /// </remarks>
        /// <param name="charPointer">Pointer to the first character.</param>
        /// <param name="charCount">How many characters are in the string.</param>
        /// <returns>Number of bytes it will take.</returns>
        public static int GetSizeForString(char* charPointer,int charCount)
        {
            var headerSize = GetSizeForPackedUnsignedInteger((uint)charCount);
            var charsSize = Uft8.GetByteCount(charPointer, charCount);
            return headerSize + charsSize;
        }

        public static void WriteString(IBufferWriter<byte> span, string? val)
        {
            var size = GetSizeForString(val);
            var spanPointer = span.GetSpan(size);
            WriteString(ref spanPointer,val);
            span.Advance(size);
        }
        
        

        
        /// <summary>
        /// Write a string as utf8.
        /// </summary>
        /// <remarks>
        /// Prefixes the data with a 'packed unsigned integer' telling how many bytes will follow.
        /// Format will match that of a <see cref="System.IO.BinaryWriter"/> that is using utf8 encoding.
        /// </remarks>
        /// <param name="span">Span to write to.</param>
        /// <param name="val">Value to write.</param>
        public static void WriteString(ref Span<byte> span, string? val)
        {
            if (string.IsNullOrWhiteSpace(val))
            {
                WritePackedUnsignedInteger(ref span, 0);
                return;
            }
            fixed (char* charPointer = val)
            {
                WriteString(ref span, charPointer, val.Length);
            }
        }
        
        public static void WriteString(ref Memory<byte> span, string? val)
        {
            if (string.IsNullOrWhiteSpace(val))
            {
                WritePackedUnsignedInteger(ref span, 0);
                return;
            }
            fixed (char* charPointer = val)
            {
                WriteString(ref span, charPointer, val.Length);
            }
        }

        /// <summary>
        /// Write a string as utf8.
        /// </summary>
        /// <remarks>
        /// Prefixes the data with a 'packed unsigned integer' telling how many bytes will follow.
        /// Format will match that of a <see cref="System.IO.BinaryWriter"/> that is using utf8 encoding.
        /// </remarks>
        /// <param name="span">Span to write to.</param>
        /// <param name="val">Value to write.</param>
        public static void WriteString(ref Span<byte> span,in ReadOnlySpan<char> val)
        {
            fixed (char* charPointer = val)
            {
                WriteString(ref span, charPointer, val.Length);
            }
        }
        
        public static void WriteString(IBufferWriter<byte> span, in ReadOnlySpan<char> val)
        {
            var size = GetSizeForString(val);
            var spanPointer = span.GetSpan(size);
            WriteString(ref spanPointer,val);
            span.Advance(size);
        }
        
        public static void WriteString(ref Memory<byte> span,in ReadOnlySpan<char> val)
        {
            fixed (char* charPointer = val)
            {
                WriteString(ref span, charPointer, val.Length);
            }
        }
        

        /// <summary>
        /// Write a string as utf8.
        /// Make sure the data behind the pointer is pinned before calling this.
        /// </summary>
        /// <remarks>
        /// Prefixes the data with a 'packed unsigned integer' telling how many bytes will follow.
        /// Format will match that of a <see cref="System.IO.BinaryWriter"/> that is using utf8 encoding.
        /// </remarks>
        /// <param name="span">Span to write to.</param>
        /// <param name="charPointer">Pointer to the first character.</param>
        /// <param name="charCount">How many characters are in the string.</param>
        public static void WriteString(ref Span<byte> span, char* charPointer,int charCount)
        {
            // Write amount of bytes will follow.
            var byteCount = Uft8.GetByteCount(charPointer, charCount);
            WritePackedUnsignedInteger(ref span, (uint)byteCount);
            if (charCount != 0)
            {
                fixed (byte* spanPointer = span)
                {
                    // Write chars as utf8.
                    var writtenBytes = Uft8.GetBytes(charPointer, charCount, spanPointer, span.Length);
                    Debug.Assert(byteCount == writtenBytes, "Written bytes did not match encodings expected size");
                }
                // 'Advance' the span.
                span = span[byteCount..];
            }
        }
        
        public static void WriteString(ref Memory<byte> memory, char* charPointer,int charCount)
        {
            // Write amount of bytes will follow.
            var byteCount = Uft8.GetByteCount(charPointer, charCount);
            WritePackedUnsignedInteger(ref memory, (uint)byteCount);
            if (charCount != 0)
            {
                fixed (byte* spanPointer = memory.Span)
                {
                    // Write chars as utf8.
                    var writtenBytes = Uft8.GetBytes(charPointer, charCount, spanPointer, memory.Length);
                    Debug.Assert(byteCount == writtenBytes, "Written bytes did not match encodings expected size");
                }
                // 'Advance' the span.
                memory = memory[byteCount..];
            }
        }

        public static void WriteString(IBufferWriter<byte> span, char* charPointer, int charCount)
        {
            var size = GetSizeForString(charPointer, charCount);
            var spanPointer = span.GetSpan(size);
            WriteString(ref spanPointer,charPointer, charCount);
            span.Advance(size);
        }

        #endregion

        #region WriteStruct

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteStruct<T>(ref Span<byte> span, T val)
            where T : unmanaged
        {
            MemoryMarshal.Write(span, in val);
            // 'Advance' the span.
            var size = Unsafe.SizeOf<T>();
            span = span[size..];
        }

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteStruct<T>(ref Span<byte> span, in T val)
            where T : unmanaged
        {
            MemoryMarshal.Write(span, in val);
            // 'Advance' the span.
            var size = Unsafe.SizeOf<T>();
            span = span[size..];
        }
        
        public static void WriteStruct<T>(IBufferWriter<byte> wrt, in T val)
            where T : unmanaged
        {
            var size = Unsafe.SizeOf<T>();
            var span = wrt.GetSpan(size);
            WriteStruct(ref span,in val);
            wrt.Advance(size);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteStruct<T>(Span<byte> span,in T val)
            where T : unmanaged
        {
            MemoryMarshal.Write(span, in val);
        }
       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteStruct<T>(Span<byte> span,T val)
            where T : unmanaged
        {
            MemoryMarshal.Write(span, in val);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteStruct<T>(Memory<byte> memory,in T val)
            where T : unmanaged
        {
            MemoryMarshal.Write(memory.Span, in val);
        }
       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteStruct<T>(Memory<byte> memory,T val)
            where T : unmanaged
        {
            MemoryMarshal.Write(memory.Span, in val);
        }

        #endregion

        #region WriteBool

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBool(ref Span<byte> span, bool val)
        {
            MemoryMarshal.Write(span, in val);

            // 'Advance' the span.
            span = span[sizeof(bool)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBool(Span<byte> span, bool val)
        {
            MemoryMarshal.Write(span, in val);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBool(ref Memory<byte> memory, bool val)
        {
            MemoryMarshal.Write(memory.Span, in val);

            // 'Advance' the span.
            memory = memory[sizeof(bool)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBool(Memory<byte> memory, bool val)
        {
            MemoryMarshal.Write(memory.Span, in val);
        }
        
        public static void WriteBool(IBufferWriter<byte> wrt, bool val)
        {
            var span = wrt.GetSpan(1);
            span[0] = (byte)(val ? 1 : 0);
            wrt.Advance(1);
        }
        

        #endregion
        
        #region ReadBool

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ReadBool(ref ReadOnlySpan<byte> span)
        {
            var result = MemoryMarshal.Read<bool>(span);
            // 'Advance' the span.
            span = span[sizeof(bool)..];
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadBool(ref ReadOnlySpan<byte> span, ref bool value)
        {
            value = MemoryMarshal.Read<bool>(span);
            // 'Advance' the span.
            span = span[sizeof(bool)..];
        }
       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadBool(ReadOnlySpan<byte> span, ref bool value)
        {
            value = MemoryMarshal.Read<bool>(span);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadBool(ref ReadOnlyMemory<byte> memory, ref bool value)
        {
            value = MemoryMarshal.Read<bool>(memory.Span);
            // 'Advance' the span.
            memory = memory[sizeof(bool)..];
        }
       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadBool(ReadOnlyMemory<byte> memory, ref bool value)
        {
            value = MemoryMarshal.Read<bool>(memory.Span);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReadBool(ref SequenceReader<byte> reader, ref bool value)
        {
            if (reader.TryRead(out var result))
            {
                value = result != 0;
                return true;
            }

            return false;
        }
        

        #endregion

        #region WriteByte

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteByte(ref Span<byte> span, byte val)
        {
            MemoryMarshal.Write(span, in val);

            // 'Advance' the span.
            span = span[sizeof(byte)..];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteByte(Span<byte> span, byte val)
        {
            MemoryMarshal.Write(span, in val);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteByte(ref Memory<byte> span, byte val)
        {
            MemoryMarshal.Write(span.Span, in val);
            // 'Advance' the span.
            span = span[sizeof(byte)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteByte(IBufferWriter<byte> wrt, byte val)
        {
            var span = wrt.GetSpan(1);
            span[0] = val;
            wrt.Advance(1);
        }
        
       
        
        #endregion
        
        #region ReadByte

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReadByte(ref ReadOnlySpan<byte> span)
        {
            var result = MemoryMarshal.Read<byte>(span);
            // 'Advance' the span.
            span = span[sizeof(byte)..];
            return result;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadByte(ref ReadOnlySpan<byte> span, ref byte value)
        {
            value = MemoryMarshal.Read<byte>(span);
            // 'Advance' the span.
            span = span[sizeof(byte)..];
        }
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadByte(ReadOnlySpan<byte> span, ref byte value)
        {
            value = MemoryMarshal.Read<byte>(span);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte ReadByte(ref ReadOnlyMemory<byte> memory)
        {
            var result = MemoryMarshal.Read<byte>(memory.Span);
            // 'Advance' the span.
            memory = memory[sizeof(byte)..];
            return result;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadByte(ReadOnlyMemory<byte> memory, ref byte value)
        {
            value = MemoryMarshal.Read<byte>(memory.Span);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadByte(ref ReadOnlyMemory<byte> memory, ref byte value)
        {
            value = MemoryMarshal.Read<byte>(memory.Span);
            // 'Advance' the span.
            memory = memory[sizeof(byte)..];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReadByte(ref SequenceReader<byte> reader, ref byte value)
        {
            return reader.TryRead(out value);
        }

        #endregion

        #region WriteSByte

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSByte(ref Span<byte> span, sbyte val)
        {
            MemoryMarshal.Write(span, in val);

            // 'Advance' the span.
            span = span[sizeof(sbyte)..];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSByte(Span<byte> span, sbyte val)
        {
            MemoryMarshal.Write(span, in val);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSByte(ref Memory<byte> memory, sbyte val)
        {
            MemoryMarshal.Write(memory.Span, in val);

            // 'Advance' the span.
            memory = memory[sizeof(sbyte)..];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteSByte(Memory<byte> memory, sbyte val)
        {
            MemoryMarshal.Write(memory.Span, in val);
        }

        public static void WriteSByte(IBufferWriter<byte> wrt,in sbyte val)
        {
            var span = wrt.GetSpan(1);
            WriteSByte(ref span, val);
            wrt.Advance(1);
        }
        #endregion
        
        #region ReadSByte

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte ReadSByte(ref ReadOnlySpan<byte> span)
        {
            var result = MemoryMarshal.Read<sbyte>(span);
            // 'Advance' the span.
            span = span[sizeof(sbyte)..];
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadSByte(ref ReadOnlySpan<byte> span, ref sbyte value)
        {
            value = MemoryMarshal.Read<sbyte>(span);
            span = span[sizeof(sbyte)..];
        }
      
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadSByte(ReadOnlySpan<byte> span, ref sbyte value)
        {
            value = MemoryMarshal.Read<sbyte>(span);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadSByte(ref ReadOnlyMemory<byte> memory, ref sbyte value)
        {
            value = MemoryMarshal.Read<sbyte>(memory.Span);
            memory = memory[sizeof(sbyte)..];
        }
      
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadSByte(ReadOnlyMemory<byte> memory, ref sbyte value)
        {
            value = MemoryMarshal.Read<sbyte>(memory.Span);
        }
        
        public static bool TryReadSByte(ref SequenceReader<byte> reader, ref sbyte value)
        {
            if (reader.TryRead(out var result))
            {
                value = (sbyte)result;
                return true;
            }

            return false;
        }
        

        #endregion

        #region WriteShort

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteShort(ref Span<byte> span, short val)
        {
            MemoryMarshal.Write(span, in val);
            // 'Advance' the span.
            span = span[sizeof(short)..];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteShort(Span<byte> span, short val)
        {
            MemoryMarshal.Write(span, in val);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteShort(ref Memory<byte> span, short val)
        {
            MemoryMarshal.Write(span.Span, in val);
            // 'Advance' the span.
            span = span[sizeof(short)..];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteShort(Memory<byte> span, short val)
        {
            MemoryMarshal.Write(span.Span, in val);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteShort(IBufferWriter<byte> wrt, short val)
        {
            var span = wrt.GetSpan(sizeof(short));
            WriteShort(ref span, val);
            wrt.Advance(sizeof(short));
        }
        #endregion
        
        #region ReadShort

       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadShort(ref ReadOnlySpan<byte> span)
        {
            var result = MemoryMarshal.Read<short>(span);
            // 'Advance' the span.
            span = span[sizeof(short)..];
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadShort(ref ReadOnlySpan<byte> span, ref short value)
        {
            value = MemoryMarshal.Read<short>(span);
            // 'Advance' the span.
            span = span[sizeof(short)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadShort(ReadOnlySpan<byte> span, ref short value)
        {
            value = MemoryMarshal.Read<short>(span);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short ReadShort(ref ReadOnlyMemory<byte> span)
        {
            var result = MemoryMarshal.Read<short>(span.Span);
            // 'Advance' the span.
            span = span[sizeof(short)..];
            return result;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadShort(ref ReadOnlyMemory<byte> memory, ref short value)
        {
            value = MemoryMarshal.Read<short>(memory.Span);
            // 'Advance' the span.
            memory = memory[sizeof(short)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadShort(ReadOnlyMemory<byte> memory, ref short value)
        {
            value = MemoryMarshal.Read<short>(memory.Span);
        }

        public static bool TryReadShort(ref SequenceReader<byte> reader, ref short value)
        {
            const int size = sizeof(short);
            var buff = ArrayPool<byte>.Shared.Rent(size);
            try
            {
                var span = new Span<byte>(buff, 0, size);
                if (reader.TryCopyTo(span) == false) return false;
                reader.Advance(size);
                var roSpan = new ReadOnlySpan<byte>(buff, 0, size);
                ReadShort(ref roSpan, ref value);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buff);
            }

            return true;
        }

        #endregion

        #region WriteUShort

        /// <summary>
        /// Write a 16 bit unsigned integer.
        /// </summary>
        /// <remarks>
        /// Will consume 2 bytes.
        /// </remarks>
        /// <param name="span">Span to write to.</param>
        /// <param name="val">Value to write.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUShort(ref Span<byte> span, ushort val)
        {
            MemoryMarshal.Write(span, in val);
            // 'Advance' the span.
            span = span[sizeof(ushort)..];
        }
        
        /// <summary>
        /// Write a 16 bit unsigned integer.
        /// </summary>
        /// <remarks>
        /// Will consume 2 bytes.
        /// </remarks>
        /// <param name="span">Span to write to.</param>
        /// <param name="val">Value to write.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUShort(Span<byte> span, ushort val)
        {
            MemoryMarshal.Write(span, in val);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUShort(IBufferWriter<byte> wrt, ushort val)
        {
            var span = wrt.GetSpan(sizeof(ushort));
            WriteUShort(ref span, val);
            wrt.Advance(sizeof(ushort));
        }
        
        #endregion
        
        #region ReadUShort

        /// <summary>
        /// Read a unsigned 16 bit integer.
        /// </summary>
        /// <remarks>
        /// Will consume 2 bytes.
        /// </remarks>
        /// <param name="span">Span to read from.</param>
        /// <returns>Read value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadUShort(ref ReadOnlySpan<byte> span)
        {
            var result = MemoryMarshal.Read<ushort>(span);

            // 'Advance' the span.
            span = span[sizeof(ushort)..];

            return result;
        }
        /// <summary>
        /// Read a unsigned 16 bit integer.
        /// </summary>
        /// <remarks>
        /// Will consume 2 bytes.
        /// </remarks>
        /// <param name="span">Span to read from.</param>
        /// <param name="value">Read value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadUShort(ref ReadOnlySpan<byte> span, ref ushort value)
        {
            value = MemoryMarshal.Read<ushort>(span);
            span = span[sizeof(short)..];
        }
        /// <summary>
        /// Read a unsigned 16 bit integer.
        /// </summary>
        /// <remarks>
        /// Will consume 2 bytes.
        /// </remarks>
        /// <param name="span">Span to read from.</param>
        /// <param name="value">Read value</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadUShort(ReadOnlySpan<byte> span, ref ushort value)
        {
            value = MemoryMarshal.Read<ushort>(span);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadUShort(ref ReadOnlyMemory<byte> span, ref ushort value)
        {
            value = MemoryMarshal.Read<ushort>(span.Span);
            span = span[sizeof(ushort)..];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadUShort(ReadOnlyMemory<byte> span, ref ushort value)
        {
            value = MemoryMarshal.Read<ushort>(span.Span);
        }
        
        public static bool TryReadUShort(ref SequenceReader<byte> reader, ref ushort value)
        {
            const int size = sizeof(ushort);
            var buff = ArrayPool<byte>.Shared.Rent(size);
            try
            {
                var span = new Span<byte>(buff, 0, size);
                if (reader.TryCopyTo(span) == false) return false;
                reader.Advance(size);
                var roSpan = new ReadOnlySpan<byte>(buff, 0, size);
                ReadUShort(ref roSpan, ref value);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buff);
            }

            return true;
        }

        #endregion

        #region WriteInt

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt(ref Span<byte> span, int val)
        {
            MemoryMarshal.Write(span, in val);
            // 'Advance' the span.
            span = span[sizeof(int)..];
        }
       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt(Span<byte> span, int val)
        {
            MemoryMarshal.Write(span, in val);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt(ref Memory<byte> mem, in int val)
        {
            MemoryMarshal.Write(mem.Span, in val);
            // 'Advance' the span.
            mem = mem[sizeof(int)..];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt(Memory<byte> mem, in int val)
        {
            MemoryMarshal.Write(mem.Span, in val);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteInt(IBufferWriter<byte> wrt, int val)
        {
            var span = wrt.GetSpan(sizeof(int));
            WriteInt(ref span, val);
            wrt.Advance(sizeof(int));
        }
        
        #endregion
        
        #region ReadInt

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ReadInt(ref ReadOnlySpan<byte> span)
        {
            var result = MemoryMarshal.Read<int>(span);

            // 'Advance' the span.
            span = span[sizeof(int)..];

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadInt(ref ReadOnlySpan<byte> span, ref int value)
        {
            value = MemoryMarshal.Read<int>(span);
            // 'Advance' the span.
            span = span[sizeof(int)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadInt(ReadOnlySpan<byte> span, ref int value)
        {
            value = MemoryMarshal.Read<int>(span);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadInt(ReadOnlyMemory<byte> memory, ref int value)
        {
            value = MemoryMarshal.Read<int>(memory.Span);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadInt(ref ReadOnlyMemory<byte> memory, ref int value)
        {
            value = MemoryMarshal.Read<int>(memory.Span);
            // 'Advance' the span.
            memory = memory[sizeof(int)..];
        }
        
        public static bool TryReadInt(ref SequenceReader<byte> reader, ref int value)
        {
            const int size = sizeof(int);
            var buff = ArrayPool<byte>.Shared.Rent(size);
            try
            {
                var span = new Span<byte>(buff, 0, size);
                if (reader.TryCopyTo(span) == false) return false;
                reader.Advance(size);
                var roSpan = new ReadOnlySpan<byte>(buff, 0, size);
                ReadInt(ref roSpan, ref value);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buff);
            }

            return true;
        }

        #endregion

        #region WriteUInt

      
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt(ref Span<byte> span, uint val)
        {
            MemoryMarshal.Write(span, in val);
            // 'Advance' the span.
            span = span[sizeof(uint)..];
        }
       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt(Span<byte> span, uint val)
        {
            MemoryMarshal.Write(span, in val);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt(ref Memory<byte> memory, uint val)
        {
            MemoryMarshal.Write(memory.Span, in val);
            // 'Advance' the span.
            memory = memory[sizeof(uint)..];
        }
       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt(Memory<byte> memory, uint val)
        {
            MemoryMarshal.Write(memory.Span, in val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteUInt(IBufferWriter<byte> wrt, uint val)
        {
            var span = wrt.GetSpan(sizeof(uint));
            WriteUInt(ref span, val);
            wrt.Advance(sizeof(uint));
        }
        
        #endregion
        
        #region ReadUInt

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadUInt(ref ReadOnlySpan<byte> span)
        {
            var result = MemoryMarshal.Read<uint>(span);
            // 'Advance' the span.
            span = span[sizeof(uint)..];
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadUInt(ref ReadOnlySpan<byte> span, ref uint value)
        {
            value = MemoryMarshal.Read<uint>(span);
            // 'Advance' the span.
            span = span[sizeof(uint)..];
        }
       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadUInt(ReadOnlySpan<byte> span, ref uint value)
        {
            value = MemoryMarshal.Read<uint>(span);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadUInt(ref ReadOnlyMemory<byte> span, ref uint value)
        {
            value = MemoryMarshal.Read<uint>(span.Span);
            // 'Advance' the span.
            span = span[sizeof(uint)..];
        }
       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadUInt(ReadOnlyMemory<byte> span, ref uint value)
        {
            value = MemoryMarshal.Read<uint>(span.Span);
        }
        
        public static bool TryReadUInt(ref SequenceReader<byte> reader, ref uint value)
        {
            const int size = sizeof(uint);
            var buff = ArrayPool<byte>.Shared.Rent(size);
            try
            {
                var span = new Span<byte>(buff, 0, size);
                if (reader.TryCopyTo(span) == false) return false;
                reader.Advance(size);
                var roSpan = new ReadOnlySpan<byte>(buff, 0, size);
                ReadUInt(ref roSpan, ref value);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buff);
            }
            return true;
        }

        #endregion

        #region WriteLong

     
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLong(ref Span<byte> span, long val)
        {
            MemoryMarshal.Write(span, in val);
            // 'Advance' the span.
            span = span[sizeof(long)..];
        }
       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLong(ref Span<byte> span, in long val)
        {
            MemoryMarshal.Write(span, in val);
            // 'Advance' the span.
            span = span[sizeof(long)..];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLong(Span<byte> span, in long val)
        {
            MemoryMarshal.Write(span, in val);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLong(ref Memory<byte> memory, in long val)
        {
            MemoryMarshal.Write(memory.Span, in val);
            // 'Advance' the memory.
            memory = memory[sizeof(long)..];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLong(Memory<byte> memory, in long val)
        {
            MemoryMarshal.Write(memory.Span, in val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteLong(IBufferWriter<byte> wrt, in long val)
        {
            var span = wrt.GetSpan(sizeof(long));
            WriteLong(ref span,in val);
            wrt.Advance(sizeof(long));
        }
        
        #endregion
        
        #region ReadLong

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadLong(ref ReadOnlySpan<byte> span)
        {
            var result = MemoryMarshal.Read<long>(span);

            // 'Advance' the span.
            span = span[sizeof(long)..];

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadLong(ref ReadOnlySpan<byte> span, ref long value)
        {
            value = MemoryMarshal.Read<long>(span);
            // 'Advance' the span.
            span = span[sizeof(long)..];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadLong(ReadOnlySpan<byte> span, ref long value)
        {
            value = MemoryMarshal.Read<long>(span);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadLong(ref ReadOnlyMemory<byte> memory, ref long value)
        {
            value = MemoryMarshal.Read<long>(memory.Span);
            // 'Advance' the span.
            memory = memory[sizeof(long)..];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadLong(ReadOnlyMemory<byte> memory, ref long value)
        {
            value = MemoryMarshal.Read<long>(memory.Span);
        }
        
        public static bool TryReadLong(ref SequenceReader<byte> reader, ref long value)
        {
            const int size = sizeof(long);
            var buff = ArrayPool<byte>.Shared.Rent(size);
            try
            {
                var span = new Span<byte>(buff, 0, size);
                if (reader.TryCopyTo(span) == false) return false;
                reader.Advance(size);
                var roSpan = new ReadOnlySpan<byte>(buff, 0, size);
                ReadLong(ref roSpan, ref value);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buff);
            }
            return true;
        }

        #endregion
        
        #region WriteULong

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteULong(ref Span<byte> span, ulong val)
        {
            MemoryMarshal.Write(span, in val);
            // 'Advance' the span.
            span = span[sizeof(ulong)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteULong(ref Span<byte> span, in ulong val)
        {
            MemoryMarshal.Write(span, in val);
            // 'Advance' the span.
            span = span[sizeof(ulong)..];
        }
      
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteULong(Span<byte> span, in ulong val)
        {
            MemoryMarshal.Write(span, in val);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteULong(ref Memory<byte> memory, in ulong val)
        {
            MemoryMarshal.Write(memory.Span, in val);
            // 'Advance' the span.
            memory = memory[sizeof(ulong)..];
        }
      
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteULong(Memory<byte> memory, in ulong val)
        {
            MemoryMarshal.Write(memory.Span, in val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteULong(IBufferWriter<byte> wrt, in ulong val)
        {
            var span = wrt.GetSpan(sizeof(ulong));
            WriteULong(ref span,in val);
            wrt.Advance(sizeof(ulong));
        }
        
        #endregion
        
        #region ReadULong

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadULong(ref ReadOnlySpan<byte> span)
        {
            var result = MemoryMarshal.Read<ulong>(span);

            // 'Advance' the span.
            span = span[sizeof(ulong)..];

            return result;
        }
       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadULong(ref ReadOnlySpan<byte> span, ref ulong value)
        {
            value = MemoryMarshal.Read<ulong>(span);
            // 'Advance' the span.
            span = span[sizeof(ulong)..];
        }
       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadULong(ReadOnlySpan<byte> span, ref ulong value)
        {
            value = MemoryMarshal.Read<ulong>(span);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadULong(ref ReadOnlyMemory<byte> memory, ref ulong value)
        {
            value = MemoryMarshal.Read<ulong>(memory.Span);
            // 'Advance' the span.
            memory = memory[sizeof(ulong)..];
        }
       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadULong(ReadOnlyMemory<byte> memory, ref ulong value)
        {
            value = MemoryMarshal.Read<ulong>(memory.Span);
        }
        
        public static bool TryReadULong(ref SequenceReader<byte> reader, ref ulong value)
        {
            const int size = sizeof(ulong);
            var buff = ArrayPool<byte>.Shared.Rent(size);
            try
            {
                var span = new Span<byte>(buff, 0, size);
                if (reader.TryCopyTo(span) == false) return false;
                reader.Advance(size);
                var roSpan = new ReadOnlySpan<byte>(buff, 0, size);
                ReadULong(ref roSpan, ref value);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buff);
            }

            return true;
        }

        #endregion

        #region Write Half
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteHalf(ref Span<byte> span, Half val)
        {
            MemoryMarshal.Write(span, in val);
            // 'Advance' the span.
            span = span[sizeof(Half)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteHalf(ref Span<byte> span, in Half val)
        {
            MemoryMarshal.Write(span, in val);
            // 'Advance' the span.
            span = span[sizeof(Half)..];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteHalf(Span<byte> span, in Half val)
        {
            MemoryMarshal.Write(span, in val);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteHalf(Span<byte> span, Half val)
        {
            MemoryMarshal.Write(span, in val);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteHalf(Memory<byte> memory, in Half val)
        {
            MemoryMarshal.Write(memory.Span, in val);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteHalf(ref Memory<byte> memory, in Half val)
        {
            MemoryMarshal.Write(memory.Span, in val);
            // 'Advance' the span.
            memory = memory[sizeof(Half)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteHalf(IBufferWriter<byte> wrt, in Half val)
        {
            var span = wrt.GetSpan(sizeof(Half));
            WriteHalf(ref span,in val);
            wrt.Advance(sizeof(Half));
        }
        
        #endregion
        
        #region Read Half
        
        /// <summary>
        /// Read a 32 bit Halfing-point number.
        /// </summary>
        /// <remarks>
        /// Will consume 4 bytes.
        /// </remarks>
        /// <param name="span">Span to read from.</param>
        /// <returns>Read value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Half ReadHalf(ref ReadOnlySpan<byte> span)
        {
            var result = MemoryMarshal.Read<Half>(span);
            // 'Advance' the span.
            span = span[sizeof(Half)..];
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadHalf(ref ReadOnlySpan<byte> span, ref Half value)
        {
            value = MemoryMarshal.Read<Half>(span);
            // 'Advance' the span.
            span = span[sizeof(Half)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadHalf(ReadOnlySpan<byte> span, ref Half value)
        {
            value = MemoryMarshal.Read<Half>(span);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadHalf(ref ReadOnlyMemory<byte> memory, ref Half value)
        {
            value = MemoryMarshal.Read<Half>(memory.Span);
            // 'Advance' the span.
            memory = memory[sizeof(Half)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadHalf(ReadOnlyMemory<byte> memory, ref Half value)
        {
            value = MemoryMarshal.Read<Half>(memory.Span);
        }

        
        public static bool TryReadHalf(ref SequenceReader<byte> reader, ref Half value)
        {
            var size = sizeof(Half);
            var buff = ArrayPool<byte>.Shared.Rent(size);
            try
            {
                var span = new Span<byte>(buff, 0, size);
                if (reader.TryCopyTo(span) == false) return false;
                reader.Advance(size);
                var roSpan = new ReadOnlySpan<byte>(buff, 0, size);
                ReadHalf(ref roSpan, ref value);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buff);
            }

            return true;
        }
        
        #endregion
        
        #region WriteFloat

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFloat(ref Span<byte> span, float val)
        {
            MemoryMarshal.Write(span, in val);
            // 'Advance' the span.
            span = span[sizeof(float)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFloat(ref Span<byte> span, in float val)
        {
            MemoryMarshal.Write(span, in val);
            // 'Advance' the span.
            span = span[sizeof(float)..];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFloat(Span<byte> span, in float val)
        {
            MemoryMarshal.Write(span, in val);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFloat(Span<byte> span, float val)
        {
            MemoryMarshal.Write(span, in val);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFloat(Memory<byte> memory, in float val)
        {
            MemoryMarshal.Write(memory.Span, in val);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFloat(ref Memory<byte> memory, in float val)
        {
            MemoryMarshal.Write(memory.Span, in val);
            // 'Advance' the span.
            memory = memory[sizeof(float)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteFloat(IBufferWriter<byte> wrt, in float val)
        {
            var span = wrt.GetSpan(sizeof(float));
            WriteFloat(ref span,in val);
            wrt.Advance(sizeof(float));
        }
        
        #endregion
        
        #region ReadFloat

        /// <summary>
        /// Read a 32 bit floating-point number.
        /// </summary>
        /// <remarks>
        /// Will consume 4 bytes.
        /// </remarks>
        /// <param name="span">Span to read from.</param>
        /// <returns>Read value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ReadFloat(ref ReadOnlySpan<byte> span)
        {
            var result = MemoryMarshal.Read<float>(span);
            // 'Advance' the span.
            span = span[sizeof(float)..];
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadFloat(ref ReadOnlySpan<byte> span, ref float value)
        {
            value = MemoryMarshal.Read<float>(span);
            // 'Advance' the span.
            span = span[sizeof(float)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadFloat(ReadOnlySpan<byte> span, ref float value)
        {
            value = MemoryMarshal.Read<float>(span);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadFloat(ref ReadOnlyMemory<byte> memory, ref float value)
        {
            value = MemoryMarshal.Read<float>(memory.Span);
            // 'Advance' the span.
            memory = memory[sizeof(float)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadFloat(ReadOnlyMemory<byte> memory, ref float value)
        {
            value = MemoryMarshal.Read<float>(memory.Span);
        }

        
        public static bool TryReadFloat(ref SequenceReader<byte> reader, ref float value)
        {
            const int size = sizeof(float);
            var buff = ArrayPool<byte>.Shared.Rent(size);
            try
            {
                var span = new Span<byte>(buff, 0, size);
                if (reader.TryCopyTo(span) == false) return false;
                reader.Advance(size);
                var roSpan = new ReadOnlySpan<byte>(buff, 0, size);
                ReadFloat(ref roSpan, ref value);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buff);
            }

            return true;
        }

        #endregion

        #region WriteDouble

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDouble(ref Span<byte> span, double val)
        {
            MemoryMarshal.Write(span, in val);
            // 'Advance' the span.
            span = span[sizeof(double)..];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDouble(ref Span<byte> span, in double val)
        {
            MemoryMarshal.Write(span, in val);
            // 'Advance' the span.
            span = span[sizeof(double)..];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDouble(Span<byte> span, in double val)
        {
            MemoryMarshal.Write(span, in val);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDouble(ref Memory<byte> memory, in double val)
        {
            MemoryMarshal.Write(memory.Span, in val);
            // 'Advance' the span.
            memory = memory[sizeof(double)..];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDouble(Memory<byte> memory, in double val)
        {
            MemoryMarshal.Write(memory.Span, in val);
        }

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDouble(IBufferWriter<byte> wrt, in double val)
        {
            var span = wrt.GetSpan(sizeof(double));
            WriteDouble(ref span,in val);
            wrt.Advance(sizeof(double));
        }


        #endregion
        
        #region ReadDouble

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ReadDouble(ref ReadOnlySpan<byte> span)
        {
            var result = MemoryMarshal.Read<double>(span);
            // 'Advance' the span.
            span = span[sizeof(double)..];
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadDouble(ref ReadOnlySpan<byte> span, ref double value)
        {
            value = MemoryMarshal.Read<double>(span);
            // 'Advance' the span.
            span = span[sizeof(double)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadDouble(ReadOnlySpan<byte> span, ref double value)
        {
            value = MemoryMarshal.Read<double>(span);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadDouble(ref ReadOnlyMemory<byte> span, ref double value)
        {
            value = MemoryMarshal.Read<double>(span.Span);
            // 'Advance' the span.
            span = span[sizeof(double)..];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadDouble(ReadOnlyMemory<byte> span, ref double value)
        {
            value = MemoryMarshal.Read<double>(span.Span);
        }

        public static bool TryReadDouble(ref SequenceReader<byte> reader, ref double value)
        {
            const int size = sizeof(double);
            var buff = ArrayPool<byte>.Shared.Rent(size);
            try
            {
                var span = new Span<byte>(buff, 0, size);
                if (reader.TryCopyTo(span) == false) return false;
                reader.Advance(size);
                var roSpan = new ReadOnlySpan<byte>(buff, 0, size);
                ReadDouble(ref roSpan, ref value);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buff);
            }

            return true;
        }
        
        #endregion

        #region ReadBitRange

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Read8BitRange(ref ReadOnlySpan<byte> span,in float min,in float max)
        {
            // Read a byte.
            var raw = ReadByte(ref span);
            // Remap it to the given range.
            return Interpolate(min, max, (float)raw / byte.MaxValue);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Read8BitRange(ref ReadOnlyMemory<byte> span,in float min,in float max)
        {
            // Read a byte.
            var raw = ReadByte(ref span);
            // Remap it to the given range.
            return Interpolate(min, max, (float)raw / byte.MaxValue);
        }

        public static bool TryRead8BitRange(ref SequenceReader<byte> rdr,in float min,in float max, ref float value)
        {
            byte raw = 0;
            if (TryReadByte(ref rdr, ref raw) == false) return false;
            value = Interpolate(min, max, (float)raw / byte.MaxValue);
            return true;
        }
       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Read16BitRange(ref ReadOnlySpan<byte> span,in  float min,in  float max)
        {
            // Read a ushort.
            var raw = ReadUShort(ref span);
            // Remap it to the given range.
            return Interpolate(min, max, (float)raw / ushort.MaxValue);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Read16BitRange(ref ReadOnlyMemory<byte> span,in  float min,in  float max)
        {
            ushort raw = 0; 
            ReadUShort(ref span, ref raw);
            // Remap it to the given range.
            return Interpolate(min, max, (float)raw / ushort.MaxValue);
        }
        
        public static bool TryRead16BitRange(ref SequenceReader<byte> rdr,in float min,in float max, ref float value)
        {
            ushort raw = 0;
            if (TryReadUShort(ref rdr, ref raw) == false) return false;
            value = Interpolate(min, max, (float)raw / byte.MaxValue);
            return true;
        }

        #endregion

        #region WriteBitRange

        /// <summary>
        /// Write a value as a fraction between a given minimum and maximum.
        /// Uses 8 bits so we have '256' steps between min and max.
        /// </summary>
        /// <remarks>
        /// Will consume 1 byte.
        /// </remarks>
        /// <param name="span">Span to write to.</param>
        /// <param name="min">Minimum value of the range.</param>
        /// <param name="max">Maximum value of the range.</param>
        /// <param name="val">Value to write (within the range.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write8BitRange(ref Span<byte> span,in  float min,in  float max,in  float val)
        {
            // Get a 0f - 1f fraction.
            var frac = Fraction(min, max, val);

            // Remap it to a byte and write it (+ .5f because we want round, not floor).
            WriteByte(ref span, (byte)(byte.MaxValue * frac + .5f));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write8BitRange(IBufferWriter<byte> wrt, in float min,in float max,in float val)
        {
            // Get a 0f - 1f fraction.
            var frac = Fraction(min, max, val);

            // Remap it to a byte and write it (+ .5f because we want round, not floor).
            WriteByte(wrt, (byte)(byte.MaxValue * frac + .5f));
        }
        
        /// <summary>
        /// Write a value as a fraction between a given minimum and maximum.
        /// Uses 16 bits so we have '65535' steps between min and max.
        /// </summary>
        /// <remarks>
        /// Will consume 2 bytes.
        /// </remarks>
        /// <param name="span">Span to write to.</param>
        /// <param name="min">Minimum value of the range.</param>
        /// <param name="max">Maximum value of the range.</param>
        /// <param name="val">Value to write (within the range.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write16BitRange(ref Span<byte> span, in float min,in float max,in float val)
        {
            // Get a 0f - 1f fraction.
            var frac = Fraction(min, max, val);

            // Remap it to ushort and write it (+ .5f because we want round, not floor).
            WriteUShort(ref span, (ushort)(ushort.MaxValue * frac + .5f));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write16BitRange(IBufferWriter<byte> wrt, in float min,in float max,in float val)
        {
            // Get a 0f - 1f fraction.
            var frac = Fraction(min, max, val);

            // Remap it to ushort and write it (+ .5f because we want round, not floor).
            WriteUShort(wrt, (ushort)(ushort.MaxValue * frac + .5f));
        }
        #endregion
        
        #region ReadString

        /// <summary>
        /// Read a string.
        /// </summary>
        /// <remarks>
        /// Can only be used for strings less then 128 kib as utf8, for bigger strings use a overload
        /// where you pass a 'Span{char}' as the output buffer.
        /// </remarks>
        /// <param name="span">Span to read from.</param>
        /// <returns>Read string.</returns>
        public static string ReadString(ref ReadOnlySpan<byte> span)
        {
            const int maxStackStringBytes = 128 * 1024; // 128 KiB.

            // Read how many bytes will follow.
            var byteCount = (int)ReadPackedUnsignedInteger(ref span);

            if (byteCount == 0) return string.Empty;

            // Check if the span contains the entire string.
            if (span.Length < byteCount)
                throw new ArgumentOutOfRangeException(nameof(span), "Given span is incomplete");

            // Sanity check the size before allocating space on the stack.
            if (byteCount >= maxStackStringBytes)
                throw new ArgumentException("Input contains a string with too many bytes to fit on the stack", nameof(span));

            // Decode on the stack to avoid having to allocate a temporary buffer on the heap.
            var maxCharCount = Uft8.GetMaxCharCount(byteCount);

            // var charBuffer = stackalloc char[maxCharCount];
            var charBuffer = ArrayPool<char>.Shared.Rent(maxCharCount); // faster then  stackalloc char[maxCharCount]; (https://stackoverflow.com/questions/55229518/why-allocation-on-arraypool-is-faster-then-allocation-on-stack)

            try
            {
                // Read chars as utf8.
                int actualCharCount;
                fixed (byte* bytePointer = span)
                fixed (char* charPointer = charBuffer)
                {
                    actualCharCount =
                        Utf8decoder.GetChars(bytePointer, byteCount, charPointer, maxCharCount, flush: false);
                }
                // 'Advance' the span.
                span = span[byteCount..];

                // Allocate the string.
                return new string(charBuffer, startIndex: 0, length: actualCharCount);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(charBuffer);
            }
        }

        public static string ReadString(ref ReadOnlyMemory<byte> span)
        {
            var originSpan = span.Span;
            var str = ReadString(ref originSpan);
            var size = span.Span.Length - originSpan.Length;
            span = span[size..];
            return str;

        }

        /// <summary>
        /// Read a string to a given output-buffer.
        /// </summary>
        /// <param name="span">Span to read from.</param>
        /// <param name="chars">Buffer to write to.</param>
        /// <returns>Amount of characters written</returns>
        public static int ReadString(ref ReadOnlySpan<byte> span, Span<char> chars)
        {
            // Read amount of bytes will follow.
            var byteCount = (int)ReadPackedUnsignedInteger(ref span);

            // Check if input span contains the entire string.
            if (span.Length < byteCount)
                throw new ArgumentOutOfRangeException(nameof(span), "Given span is incomplete");

            // No need to check if the output span has enough space as 'Encoding.GetChars' will
            // already do that for us.

            // Read chars as utf8.
            int charsRead;
            fixed (char* charPointer = chars)
            fixed (byte* bytePointer = span)
            {
                charsRead = Uft8.GetChars(bytePointer, byteCount, charPointer, chars.Length);
            }

            // 'Advance' the span.
            span = span[byteCount..];

            return charsRead;
        }

        public static bool TryReadString(ref SequenceReader<byte> reader, ref string value)
        {
            uint size = 0;
            if (TryReadPackedUnsignedInteger(ref reader, ref size, out var consumed) == false)
            {
                return false;
            }
            var buff = ArrayPool<byte>.Shared.Rent((int)size);
            try
            {
                var span = new Span<byte>(buff, 0, (int)size);
                if (reader.TryCopyTo(span) == false)
                {
                    reader.Rewind(consumed);
                    return false;
                }
                value = Uft8.GetString(span);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buff);
            }

            return true;
        }
        
        

        #endregion

        #region ReadStruct

        /// <summary>
        /// Read a unmanaged struct.
        /// </summary>
        /// <remarks>
        /// When using this make sure that 'T' has a explict memory-layout so its consistent
        /// accross platforms.
        /// In other words, only use this if you are 100% sure its safe to do so.
        /// </remarks>
        /// <param name="span">Span to read from.</param>
        /// <typeparam name="T">Type of the struct to read.</typeparam>
        /// <returns>Read value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T ReadStruct<T>(ref ReadOnlySpan<byte> span)
            where T : unmanaged
        {
            var result = MemoryMarshal.Read<T>(span);
            // 'Advance' the span.
            var size = Unsafe.SizeOf<T>();
            span = span[size..];
            return result;
        }

        /// <summary>
        /// Read a unmanaged struct.
        /// </summary>
        /// <remarks>
        /// When using this make sure that 'T' has a explict memory-layout so its consistent
        /// accross platforms.
        /// In other words, only use this if you are 100% sure its safe to do so.
        /// </remarks>
        /// <param name="span">Span to read from.</param>
        /// <param name="value">Read value</param>
        /// <typeparam name="T">Type of the struct to read.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadStruct<T>(ref ReadOnlySpan<byte> span, ref T value)
            where T : unmanaged
        {
            value = MemoryMarshal.Read<T>(span);
            // 'Advance' the span.
            var size = Unsafe.SizeOf<T>();
            span = span[size..];
        }
        /// <summary>
        /// Read a unmanaged struct.
        /// </summary>
        /// <remarks>
        /// When using this make sure that 'T' has a explict memory-layout so its consistent
        /// accross platforms.
        /// In other words, only use this if you are 100% sure its safe to do so.
        /// </remarks>
        /// <param name="span">Span to read from.</param>
        /// <param name="value">Read value</param>
        /// <typeparam name="T">Type of the struct to read.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadStruct<T>(ReadOnlySpan<byte> span, ref T value)
            where T : unmanaged
        {
            value = MemoryMarshal.Read<T>(span);
        }

        #endregion

        #region WriteBlock

        /// <summary>
        /// Write a continuous block of bytes.
        /// </summary>
        /// <remarks>
        /// Will consume as many bytes as are in the given block.
        /// </remarks>
        /// <param name="span">Span to write to.</param>
        /// <param name="val">Block of bytes to write.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBlock(ref Span<byte> span, ReadOnlySpan<byte> val)
        {
            val.CopyTo(span);
            // 'Advance' the span.
            span = span[val.Length..];
        }
        /// <summary>
        /// Write a continuous block of bytes.
        /// </summary>
        /// <remarks>
        /// Will consume as many bytes as are in the given block.
        /// </remarks>
        /// <param name="span">Span to write to.</param>
        /// <param name="val">Block of bytes to write.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBlock(Span<byte> span, ReadOnlySpan<byte> val)
        {
            val.CopyTo(span);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteBlock(IBufferWriter<byte> wrt, ReadOnlySpan<byte> val)
        {
            var span = wrt.GetSpan(val.Length);
            WriteBlock(ref span, val);
            wrt.Advance(val.Length);
        }
        #endregion

        #region ReadBlock

        /// <summary>
        /// Read a continuous block of bytes as a new byte-array.
        /// </summary>
        /// <remarks>
        /// Will consume '<paramref name="byteCount"/>' amount of bytes.
        /// </remarks>
        /// <param name="span">Span to read from.</param>
        /// <param name="byteCount">Amount of bytes to read.</param>
        /// <returns>New byte-array containing the read bytes.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ReadBlock(ref ReadOnlySpan<byte> span, int byteCount)
        {
            var result = new byte[byteCount];
            ReadBlock(ref span, result);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadBlock(ref ReadOnlySpan<byte> span, Span<byte> output)
        {
            span[..output.Length].CopyTo(output);

            // 'Advance' the span.
            span = span[output.Length..];
        }
       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadBlock(ReadOnlySpan<byte> span, Span<byte> output)
        {
            span[..output.Length].CopyTo(output);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadBlock(ReadOnlyMemory<byte> span, Span<byte> output)
        {
            span[..output.Length].Span.CopyTo(output);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ReadBlock(ref ReadOnlyMemory<byte> span, Span<byte> output)
        {
            span[..output.Length].Span.CopyTo(output);

            // 'Advance' the span.
            span = span[output.Length..];
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryReadBlock(ref SequenceReader<byte> reader, Span<byte> output)
        {
            if (reader.TryCopyTo(output) == false)
            {
                return false;
            }
            reader.Advance(output.Length);
            return true;
        }

        #endregion

        /// <summary>
        /// 'Reserve' space for a boolean and return a ref to the space.
        /// </summary>
        /// <remarks>
        /// Will consume 1 byte.
        /// </remarks>
        /// <param name="span">Span to reserver from.</param>
        /// <returns>Reference to the reserved space.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref bool ReserveBool(ref Span<byte> span)
        {
            ref var result = ref Unsafe.As<byte, bool>(ref span[0]);

            // Init to default, as otherwise it would be whatever data was at that memory.
            result = default;

            // 'Advance' the span.
            span = span[sizeof(bool)..];

            return ref result;
        }

        /// <summary>
        /// 'Reserve' space for a unsigned 8 bit integer.
        /// </summary>
        /// <remarks>
        /// Will consume 1 byte.
        /// </remarks>
        /// <param name="span">Span to reserver from.</param>
        /// <returns>Reference to the reserved space.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref byte ReserveByte(ref Span<byte> span)
        {
            ref var result = ref span[0];
            // Init to default, as otherwise it would be whatever data was at that memory.
            result = default;
            // 'Advance' the span.
            span = span[sizeof(byte)..];
            return ref result;
        }

        /// <summary>
        /// 'Reserve' space for a signed 8 bit integer.
        /// </summary>
        /// <remarks>
        /// Will consume 1 byte.
        /// </remarks>
        /// <param name="span">Span to reserver from.</param>
        /// <returns>Reference to the reserved space.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref sbyte ReserveSByte(ref Span<byte> span)
        {
            ref var result = ref Unsafe.As<byte, sbyte>(ref span[0]);
            // Init to default, as otherwise it would be whatever data was at that memory.
            result = default;
            // 'Advance' the span.
            span = span[sizeof(sbyte)..];
            return ref result;
        }

        /// <summary>
        /// 'Reserve' space for a signed 16 bit integer.
        /// </summary>
        /// <remarks>
        /// Will consume 2 bytes.
        /// </remarks>
        /// <param name="span">Span to reserver from.</param>
        /// <returns>Reference to the reserved space.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref short ReserveShort(ref Span<byte> span)
        {
            ref var result = ref Unsafe.As<byte, short>(ref span[0]);
            // Init to default, as otherwise it would be whatever data was at that memory.
            result = default;
            // 'Advance' the span.
            span = span[sizeof(short)..];
            return ref result;
        }

        /// <summary>
        /// 'Reserve' space for a unsigned 16 bit integer.
        /// </summary>
        /// <remarks>
        /// Will consume 2 bytes.
        /// </remarks>
        /// <param name="span">Span to reserver from.</param>
        /// <returns>Reference to the reserved space.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref ushort ReserveUShort(ref Span<byte> span)
        {
            ref var result = ref Unsafe.As<byte, ushort>(ref span[0]);
            // Init to default, as otherwise it would be whatever data was at that memory.
            result = default;
            // 'Advance' the span.
            span = span[sizeof(ushort)..];
            return ref result;
        }

        /// <summary>
        /// 'Reserve' space for a signed 32 bit integer.
        /// </summary>
        /// <remarks>
        /// Will consume 4 bytes.
        /// </remarks>
        /// <param name="span">Span to reserver from.</param>
        /// <returns>Reference to the reserved space.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref int ReserveInt(ref Span<byte> span)
        {
            ref var result = ref Unsafe.As<byte, int>(ref span[0]);
            // Init to default, as otherwise it would be whatever data was at that memory.
            result = default;
            // 'Advance' the span.
            span = span[sizeof(int)..];
            return ref result;
        }

        /// <summary>
        /// 'Reserve' space for a unsigned 32 bit integer.
        /// </summary>
        /// <remarks>
        /// Will consume 4 bytes.
        /// </remarks>
        /// <param name="span">Span to reserver from.</param>
        /// <returns>Reference to the reserved space.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref uint ReserveUInt(ref Span<byte> span)
        {
            ref var result = ref Unsafe.As<byte, uint>(ref span[0]);
            // Init to default, as otherwise it would be whatever data was at that memory.
            result = default;
            // 'Advance' the span.
            span = span[sizeof(uint)..];
            return ref result;
        }

        /// <summary>
        /// 'Reserve' space for a signed 64 bit integer.
        /// </summary>
        /// <remarks>
        /// Will consume 8 bytes.
        /// </remarks>
        /// <param name="span">Span to reserver from.</param>
        /// <returns>Reference to the reserved space.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref long ReserveLong(ref Span<byte> span)
        {
            ref var result = ref Unsafe.As<byte, long>(ref span[0]);
            // Init to default, as otherwise it would be whatever data was at that memory.
            result = default;
            // 'Advance' the span.
            span = span[sizeof(long)..];
            return ref result;
        }

        /// <summary>
        /// 'Reserve' space for a unsigned 64 bit integer.
        /// </summary>
        /// <remarks>
        /// Will consume 8 bytes.
        /// </remarks>
        /// <param name="span">Span to reserver from.</param>
        /// <returns>Reference to the reserved space.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref ulong ReserveULong(ref Span<byte> span)
        {
            ref var result = ref Unsafe.As<byte, ulong>(ref span[0]);
            // Init to default, as otherwise it would be whatever data was at that memory.
            result = default;
            // 'Advance' the span.
            span = span[sizeof(ulong)..];
            return ref result;
        }

        /// <summary>
        /// 'Reserve' space for a unmanaged struct.
        /// </summary>
        /// <remarks>
        /// When using this make sure that 'T' has a explict memory-layout so its consistent
        /// accross platforms.
        /// In other words, only use this if you are 100% sure its safe to do so.
        /// Will consume sizeof T.
        /// </remarks>
        /// <param name="span">Span to reserver from.</param>
        /// <typeparam name="T">Type of the unmanaged struct.</typeparam>
        /// <returns>Reference to the reserved space.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T ReserveStruct<T>(ref Span<byte> span)
            where T : unmanaged
        {
            ref var result = ref Unsafe.As<byte, T>(ref span[0]);
            // Init to default, as otherwise it would be whatever data was at that memory.
            result = default;
            // 'Advance' the span.
            var size = Unsafe.SizeOf<T>();
            span = span[size..];
            return ref result;
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
            min + (max - min) * Clamp01(frac);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Fraction(float min, float max, float val) =>
            min == max ? 0f : Clamp01((val - min) / (max - min));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Clamp01(float val) => val < 0f ? 0f : val > 1f ? 1f : val;

        
    }
}
