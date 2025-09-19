using System;
using System.Buffers;
using System.Collections.Immutable;
using System.IO;
using DotNext.Buffers;
using ZstdSharp;

namespace Asv.IO;

public sealed class ChimpRecordDecoder : IDisposable
{
    private readonly string _id;
    private readonly Func<(IVisitable, object)> _factory;
    private readonly Stream _stream;
    private readonly int _fieldCount;

    private readonly PoolingArrayBufferWriter<byte> _indexRdr;
    private readonly PoolingArrayBufferWriter<byte> _timestampRdr;
    private readonly ImmutableArray<PoolingArrayBufferWriter<byte>> _readers;

    public ChimpRecordDecoder(string id, Func<(IVisitable, object)> factory, Stream stream)
    {
        _id = id;
        _factory = factory;
        _stream = stream;
        var countVisitor = new ChimpFieldCounterVisitor();
        var msg = factory();
        msg.Item1.Accept(countVisitor);
        _fieldCount = countVisitor.Count;

        _indexRdr = new PoolingArrayBufferWriter<byte>(ArrayPool<byte>.Shared);

        _timestampRdr = new PoolingArrayBufferWriter<byte>(ArrayPool<byte>.Shared);

        var builder = ImmutableArray.CreateBuilder<PoolingArrayBufferWriter<byte>>(_fieldCount);
        for (var i = 0; i < _fieldCount; i++)
        {
            var buff = new PoolingArrayBufferWriter<byte>(ArrayPool<byte>.Shared);
            builder.Add(buff);
        }
        _readers = builder.ToImmutable();
    }

    public bool Read(Action<(VisitableRecord, object)> visitor)
    {
        try
        {
            var header = new ChunkHeader();
            header.ReadFrom(_stream);
            if (_fieldCount + 2 != header.FieldCount)
            {
                throw new InvalidOperationException("Invalid field count");
            }

            while (header.RawCount > 300)
            {
                header.RawCount -= 300;
            }

            _timestampRdr.Clear();
            ReadField(_timestampRdr);
            var timestamp = new GorillaTimestampDecoder(
                new MemoryBitReader(_timestampRdr.WrittenMemory)
            );
            _indexRdr.Clear();
            ReadField(_indexRdr);
            var index = new GorillaTimestampDecoder(new MemoryBitReader(_indexRdr.WrittenMemory));
            var streams = new ChimpDecoder[_fieldCount];
            for (var i = 0; i < _fieldCount; i++)
            {
                var buff = _readers[i];
                buff.Clear(true);
                ReadField(buff);
                streams[i] = new ChimpDecoder(new MemoryBitReader(buff.WrittenMemory));
            }

            var decoder = new ChimpFieldDecoderVisitor(streams);
            for (var i = 0; i < header.RawCount; i++)
            {
                var msg = _factory();
                msg.Item1.Accept(decoder);
                visitor(
                    (
                        new VisitableRecord(
                            (uint)index.ReadNext(),
                            DateTime.FromBinary(timestamp.ReadNext()),
                            _id,
                            msg.Item1
                        ),
                        msg.Item2
                    )
                );
                decoder.Reset();
            }

            return true;
        }
        catch (EndOfStreamException)
        {
            return false;
        }
    }

    private void ReadField(PoolingArrayBufferWriter<byte> rdr)
    {
        var field = new FieldHeader();
        field.ReadFrom(_stream);
        var buffer = ArrayPool<byte>.Shared.Rent(field.Size);
        try
        {
            _stream.ReadExactly(buffer, 0, field.Size);
            if (field.IsCompressed)
            {
                using var decompressor = new Decompressor();
                var decompressed = decompressor.Unwrap(
                    new ReadOnlySpan<byte>(buffer, 0, field.Size)
                );
                rdr.Write(decompressed);
            }
            else
            {
                rdr.Write(new ReadOnlySpan<byte>(buffer, 0, field.Size));
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public void Dispose()
    {
        _stream.Dispose();
        _indexRdr.Dispose();
        _timestampRdr.Dispose();
        foreach (var reader in _readers)
        {
            reader.Dispose();
        }
    }
}
