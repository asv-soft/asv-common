using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using DotNext.Buffers;
using Microsoft.Extensions.Logging;
using ZLogger;
using ZstdSharp;

namespace Asv.IO;

public sealed class ChimpTableDecoder : IDisposable
{
    private readonly string _id;
    private readonly Func<(IVisitable, object)> _factory;
    private readonly Stream _stream;
    private readonly uint _flushEvery;
    private readonly ILogger _logger;
    private readonly int _fieldCount;

    private readonly PoolingArrayBufferWriter<byte> _indexRdr;
    private readonly PoolingArrayBufferWriter<byte> _timestampRdr;
    private readonly ImmutableArray<PoolingArrayBufferWriter<byte>> _readers;
    private readonly List<string> _fieldNames;

    public ChimpTableDecoder(
        string id,
        Func<(IVisitable, object)> factory,
        Stream stream,
        uint flushEvery,
        ILogger logger
    )
    {
        _id = id;
        _factory = factory;
        _stream = stream;
        _flushEvery = flushEvery;
        _logger = logger;
        var countVisitor = new ChimpColumnVisitor();
        var msg = factory();
        msg.Item1.Accept(countVisitor);
        _fieldCount = countVisitor.Count;
        _fieldNames = countVisitor.Columns;

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
        var ii = 0;
        try
        {
            var header = new BatchHeader();
            header.ReadFrom(_stream);
            if (_fieldCount + 2 != header.FieldCount)
            {
                throw new InvalidOperationException("Invalid field count");
            }
            if (header.Name != _id)
            {
                throw new InvalidDataException(
                    $"Unexpected batch name: '{header.Name}', expected '{_id}'"
                );
            }

            while (header.RawCount > _flushEvery)
            {
                header.RawCount -= _flushEvery;
            }

            _indexRdr.Clear();
            ReadField(_indexRdr, "Index");
            var index = new GorillaTimestampDecoder(new MemoryBitReader(_indexRdr.WrittenMemory));

            _timestampRdr.Clear();
            ReadField(_timestampRdr, "Timestamp");

            var timestamp = new GorillaTimestampDecoder(
                new MemoryBitReader(_timestampRdr.WrittenMemory)
            );

            var streams = new ChimpDecoder[_fieldCount];
            for (var i = 0; i < _fieldCount; i++)
            {
                var buff = _readers[i];
                buff.Clear(true);
                ReadField(buff, _fieldNames[i]);
                streams[i] = new ChimpDecoder(new MemoryBitReader(buff.WrittenMemory));
            }

            var decoder = new ChimpColumnDecoderVisitor(streams);
            for (var i = 0; i < header.RawCount; i++)
            {
                ii = i;
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
            _logger.ZLogTrace($"{_id}: End of stream reached at {ii}");
            return false;
        }
    }

    private void ReadField(PoolingArrayBufferWriter<byte> rdr, string fieldName)
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

                // _logger.ZLogTrace($"{fieldName}: Read(ZST) {decompressed.Length} <= {field.Size} bytes {Convert.ToBase64String(decompressed)}");
                rdr.Write(decompressed);
            }
            else
            {
                // _logger.ZLogTrace($"{fieldName}: Read {field.Size} bytes {Convert.ToBase64String(new ReadOnlySpan<byte>(buffer, 0, field.Size))}");
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
