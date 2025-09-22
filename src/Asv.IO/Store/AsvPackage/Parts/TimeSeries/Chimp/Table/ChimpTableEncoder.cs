using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using DotNext.Buffers;
using DotNext.IO;
using Microsoft.Extensions.Logging;
using ZstdSharp;

namespace Asv.IO;

public sealed class ChimpTableEncoder : IDisposable
{
    private readonly Stream _stream;
    private const int Level = 15;
    private readonly PoolingArrayBufferWriter<byte> _indexWrt;
    private GorillaTimestampEncoder _index;
    private readonly PoolingArrayBufferWriter<byte> _timestampWrt;
    private GorillaTimestampEncoder _timestamp;
    private readonly ImmutableArray<PoolingArrayBufferWriter<byte>> _writers;
    private readonly ChimpEncoder[] _streams;
    private readonly ChimpColumnEncoderVisitor _visitor;
    private uint _count;
    private readonly string _id;
    private readonly uint _flushEvery;
    private readonly ILogger _logger;
    private readonly bool _useZstdForBatch;
    private readonly List<string> _fieldNames;

    public ChimpTableEncoder(
        TableRow msg,
        Stream stream,
        uint flushEvery,
        ILogger logger,
        bool useZstdForBatch
    )
    {
        _stream = stream;
        _flushEvery = flushEvery;
        _logger = logger;
        _useZstdForBatch = useZstdForBatch;
        _indexWrt = new PoolingArrayBufferWriter<byte>(ArrayPool<byte>.Shared);
        _index = new GorillaTimestampEncoder(new StreamBitWriter(_indexWrt.AsStream()));
        _timestampWrt = new PoolingArrayBufferWriter<byte>(ArrayPool<byte>.Shared);
        _timestamp = new GorillaTimestampEncoder(new StreamBitWriter(_timestampWrt.AsStream()));
        var countVisitor = new ChimpColumnVisitor();
        msg.Data.Accept(countVisitor);
        var count = countVisitor.Count;
        _fieldNames = countVisitor.Columns;
        var wrtBuilder = ImmutableArray.CreateBuilder<PoolingArrayBufferWriter<byte>>(count);
        _streams = new ChimpEncoder[count];
        for (var i = 0; i < count; i++)
        {
            wrtBuilder.Add(new PoolingArrayBufferWriter<byte>(ArrayPool<byte>.Shared));
            _streams[i] = new ChimpEncoder(new StreamBitWriter(wrtBuilder[i].AsStream()));
        }
        _writers = wrtBuilder.ToImmutable();
        _visitor = new ChimpColumnEncoderVisitor(_streams);
        _id = msg.Id;
    }

    public void Append(TableRow msg)
    {
        if (msg.Id != _id)
        {
            throw new InvalidOperationException(
                "All appended records must have the same Id as the encoder."
            );
        }

        _index.Add(msg.Index);
        _timestamp.Add(msg.Timestamp.ToBinary());
        msg.Data.Accept(_visitor);
        _visitor.Reset();
        ++_count;
        if (_count % _flushEvery == 0)
        {
            SaveBatch();
            Flush();
        }
    }

    private void SaveBatch()
    {
        if (_count == 0)
        {
            return;
        }

        var header = new BatchHeader
        {
            Name = _id,
            FieldCount = (uint)(_writers.Length + 2),
            RowCount = (uint)_count,
        };
        header.Serialize(_stream);

        // _logger.ZLogTrace($"Saving batch {_id} with {header.FieldCount} fields and {_count} records");
        _index.Dispose();
        Save(_indexWrt, _stream, "Index", _useZstdForBatch);
        _indexWrt.Clear(true);
        _index = new GorillaTimestampEncoder(new StreamBitWriter(_indexWrt.AsStream()));

        _timestamp.Dispose();
        Save(_timestampWrt, _stream, "Timestamp", _useZstdForBatch);
        _timestampWrt.Clear(true);
        _timestamp = new GorillaTimestampEncoder(new StreamBitWriter(_timestampWrt.AsStream()));
        for (var i = 0; i < _writers.Length; i++)
        {
            _streams[i].Dispose();
            Save(_writers[i], _stream, _fieldNames[i], _useZstdForBatch);
            _writers[i].Clear(true);
            _streams[i] = new ChimpEncoder(new StreamBitWriter(_writers[i].AsStream()));
        }

        _count = 0;
    }

    private static void Save(
        PoolingArrayBufferWriter<byte> buff,
        Stream stream,
        string fieldName,
        bool useZstdForBatch
    )
    {
        using var compressor = new Compressor(Level);
        var size = buff.WrittenCount;
        using var compressed = MemoryPool<byte>.Shared.Rent(size);
        var spanToWrite = compressed.Memory.Span[..size];
        var header = new FieldHeader();
        if (
            useZstdForBatch
            && compressor.TryWrap(buff.WrittenMemory.Span, spanToWrite, out var written)
        )
        {
            // _logger.ZLogTrace($"{fieldName}: Write(ZST) {size} => {written} bytes {Convert.ToBase64String(spanToWrite[..written])}");
            header.IsCompressed = true;
            header.Size = written;
            header.Serialize(stream);
            stream.Write(spanToWrite[..written]);
        }
        else
        {
            // _logger.ZLogTrace($"{fieldName}: Write {size} bytes {Convert.ToBase64String(buff.WrittenMemory.Span)}");
            header.IsCompressed = false;
            header.Size = size;
            header.Serialize(stream);
            stream.Write(buff.WrittenMemory.Span);
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
