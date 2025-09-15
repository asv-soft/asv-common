using System;
using System.Buffers;
using System.Buffers.Binary;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Asv.IO;

public partial class BinSerialize
{
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

    #region WriteStruct

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStruct<T>(Stream stream, T val)
        where T : unmanaged
    {
        var size = Unsafe.SizeOf<T>();
        var buff = ArrayPool<byte>.Shared.Rent(size);
        try
        {
            var span = new Span<byte>(buff, 0, size);
            MemoryMarshal.Write(span, in val);
            stream.Write(span);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buff);
        }
    }

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
        WriteStruct(ref span, in val);
        wrt.Advance(size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStruct<T>(Span<byte> span, in T val)
        where T : unmanaged
    {
        MemoryMarshal.Write(span, in val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStruct<T>(Span<byte> span, T val)
        where T : unmanaged
    {
        MemoryMarshal.Write(span, in val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStruct<T>(Memory<byte> memory, in T val)
        where T : unmanaged
    {
        MemoryMarshal.Write(memory.Span, in val);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteStruct<T>(Memory<byte> memory, T val)
        where T : unmanaged
    {
        MemoryMarshal.Write(memory.Span, in val);
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
}
