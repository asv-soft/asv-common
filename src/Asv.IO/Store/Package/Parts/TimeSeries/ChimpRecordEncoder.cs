using System;
using System.Buffers;
using System.Collections.Immutable;
using System.IO;
using DotNext.Buffers;
using DotNext.IO;
using ZstdSharp;

namespace Asv.IO;

public sealed class ChimpRecordEncoder : IDisposable
{
    private readonly Stream _stream;
    private const int Level = 15;
    private readonly PoolingArrayBufferWriter<byte> _indexWrt;
    private GorillaTimestampEncoder _index;
    private readonly PoolingArrayBufferWriter<byte> _timestampWrt;
    private GorillaTimestampEncoder _timestamp;
    private readonly ImmutableArray<PoolingArrayBufferWriter<byte>> _writers;
    private readonly ChimpEncoder[] _streams;
    private readonly ChimpFieldEncoderVisitor _visitor;
    private uint _count;
    private readonly string _id;
    private readonly uint _flushCount;

    public ChimpRecordEncoder(VisitableRecord msg, Stream stream, uint flushCount)
    {
        _stream = stream;
        _flushCount = flushCount;
        _indexWrt = new PoolingArrayBufferWriter<byte>(ArrayPool<byte>.Shared);
        _index = new GorillaTimestampEncoder(
            new StreamBitWriter(_indexWrt.AsStream()),
            leaveOpen: true
        );
        _timestampWrt = new PoolingArrayBufferWriter<byte>(ArrayPool<byte>.Shared);
        _timestamp = new GorillaTimestampEncoder(
            new StreamBitWriter(_timestampWrt.AsStream()),
            leaveOpen: true
        );
        var countVisitor = new ChimpFieldCounterVisitor();
        msg.Data.Accept(countVisitor);
        var count = countVisitor.Count;
        var wrtBuilder = ImmutableArray.CreateBuilder<PoolingArrayBufferWriter<byte>>(count);
        _streams = new ChimpEncoder[count];
        for (var i = 0; i < count; i++)
        {
            wrtBuilder.Add(new PoolingArrayBufferWriter<byte>(ArrayPool<byte>.Shared));
            _streams[i] = new ChimpEncoder(
                new StreamBitWriter(wrtBuilder[i].AsStream()),
                leaveOpen: true
            );
        }
        _writers = wrtBuilder.ToImmutable();
        _visitor = new ChimpFieldEncoderVisitor(_streams);
        _id = msg.Id;
    }

    public void Append(VisitableRecord msg)
    {
        if (msg.Id != _id)
        {
            throw new InvalidOperationException("Message id must be unique");
        }

        _index.Add(msg.Index);
        _timestamp.Add(msg.Timestamp.ToBinary());
        msg.Data.Accept(_visitor);
        _visitor.Reset();
        ++_count;
        if (_count % _flushCount == 0)
        {
            SaveBatch();
            Flush();
        }
    }

    private void SaveBatch()
    {
        var header = new ChunkHeader
        {
            Name = _id,
            FieldCount = (uint)(_writers.Length + 2),
            RawCount = (uint)_count,
        };
        header.Serialize(_stream);

        _index.Dispose();
        Save(_indexWrt, _stream);
        _indexWrt.Clear(true);
        _index = new GorillaTimestampEncoder(
            new StreamBitWriter(_indexWrt.AsStream()),
            leaveOpen: true
        );

        _timestamp.Dispose();
        Save(_timestampWrt, _stream);
        _timestampWrt.Clear(true);
        _timestamp = new GorillaTimestampEncoder(
            new StreamBitWriter(_timestampWrt.AsStream()),
            leaveOpen: true
        );
        for (var i = 0; i < _writers.Length; i++)
        {
            _streams[i].Dispose();
            Save(_writers[i], _stream);
            _writers[i].Clear(true);
            _streams[i] = new ChimpEncoder(
                new StreamBitWriter(_writers[i].AsStream()),
                leaveOpen: true
            );
        }
    }

    private static void Save(PoolingArrayBufferWriter<byte> buff, Stream stream)
    {
        using var compressor = new Compressor(Level);
        var size = buff.WrittenCount;
        var compressed = MemoryPool<byte>.Shared.Rent(size);
        var header = new FieldHeader();
        if (
            compressor.TryWrap(buff.WrittenMemory.Span, compressed.Memory.Span, out var written)
            == false
        )
        {
            header.IsCompressed = false;
            header.Size = size;
            header.Serialize(stream);
            stream.Write(buff.WrittenMemory.Span);
        }
        else
        {
            header.IsCompressed = true;
            header.Size = written;
            header.Serialize(stream);
            stream.Write(compressed.Memory.Span[..written]);
        }
    }

    public void Flush()
    {
        _stream.Flush();
    }

    public void Dispose()
    {
        SaveBatch();
        _stream.Flush();
        _index.Dispose();
        _indexWrt.Dispose();
        _timestamp.Dispose();
        _timestampWrt.Dispose();
        foreach (var stream in _streams)
        {
            stream.Dispose();
        }
        foreach (var writer in _writers)
        {
            writer.Dispose();
        }

        _stream.Dispose();
    }
}

public class ChunkHeader : ISizedSpanSerializable
{
    public const byte StartSignatureString = (byte)'[';
    public const byte EndSignatureString = (byte)']';

    public string? Name { get; set; }
    public uint FieldCount { get; set; }
    public uint RawCount { get; set; }

    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        var start = BinSerialize.ReadByte(ref buffer);
        if (start != StartSignatureString)
        {
            throw new InvalidOperationException("Invalid start signature");
        }

        Name = BinSerialize.ReadString(ref buffer);
        FieldCount = BinSerialize.ReadPackedUnsignedInteger(ref buffer);
        RawCount = BinSerialize.ReadPackedUnsignedInteger(ref buffer);
        var end = BinSerialize.ReadByte(ref buffer);
        if (end != EndSignatureString)
        {
            throw new InvalidOperationException("Invalid end signature");
        }
    }

    public void Serialize(ref Span<byte> buffer)
    {
        BinSerialize.WriteByte(ref buffer, StartSignatureString);
        BinSerialize.WriteString(ref buffer, Name);
        BinSerialize.WritePackedUnsignedInteger(ref buffer, FieldCount);
        BinSerialize.WritePackedUnsignedInteger(ref buffer, RawCount);
        BinSerialize.WriteByte(ref buffer, EndSignatureString);
    }

    public int GetByteSize()
    {
        return BinSerialize.GetSizeForString(Name)
            + BinSerialize.GetSizeForPackedUnsignedInteger(FieldCount)
            + BinSerialize.GetSizeForPackedUnsignedInteger(RawCount)
            + (2 * sizeof(byte));
    }

    public void ReadFrom(Stream stream)
    {
        var start = stream.ReadByte();
        if (start < 0)
        {
            throw new EndOfStreamException();
        }

        if (start != StartSignatureString)
        {
            throw new InvalidOperationException("Invalid start signature");
        }

        Name = BinSerialize.ReadString(stream);
        FieldCount = BinSerialize.ReadPackedUnsignedInteger(stream);
        RawCount = BinSerialize.ReadPackedUnsignedInteger(stream);
        var end = stream.ReadByte();
        if (end != EndSignatureString)
        {
            throw new InvalidOperationException("Invalid end signature");
        }
    }
}

public class FieldHeader : ISizedSpanSerializable
{
    public const byte StartField = (byte)'\t';
    public const byte EndOfField = (byte)'\n';
    public int Size { get; set; }
    public bool IsCompressed { get; set; }

    public void Deserialize(ref ReadOnlySpan<byte> buffer)
    {
        var start = BinSerialize.ReadByte(ref buffer);
        if (start != StartField)
        {
            throw new InvalidOperationException("Invalid start field");
        }

        Size = BinSerialize.ReadPackedInteger(ref buffer);
        IsCompressed = BinSerialize.ReadBool(ref buffer);
        var end = BinSerialize.ReadByte(ref buffer);
        if (end != EndOfField)
        {
            throw new InvalidOperationException("Invalid end field");
        }
    }

    public void Serialize(ref Span<byte> buffer)
    {
        BinSerialize.WriteByte(ref buffer, StartField);
        BinSerialize.WritePackedInteger(ref buffer, Size);
        BinSerialize.WriteBool(ref buffer, IsCompressed);
        BinSerialize.WriteByte(ref buffer, EndOfField);
    }

    public void ReadFrom(Stream stream)
    {
        var start = stream.ReadByte();
        if (start != StartField)
        {
            throw new InvalidOperationException("Invalid start field");
        }

        Size = BinSerialize.ReadPackedInteger(stream);
        IsCompressed = BinSerialize.ReadBool(stream);
        var end = stream.ReadByte();
        if (end != EndOfField)
        {
            throw new InvalidOperationException("Invalid end field");
        }
    }

    public int GetByteSize()
    {
        return BinSerialize.GetSizeForPackedInteger(Size) + sizeof(bool) + (2 * sizeof(byte));
    }
}
