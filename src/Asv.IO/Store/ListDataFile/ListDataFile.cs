#nullable enable
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.IO.Abstractions;
using Asv.Common;

namespace Asv.IO;

public class ListDataFile<TMetadata> : DisposableOnce, IListDataFile<TMetadata> 
    where TMetadata : ISizedSpanSerializable, new()
{
    #region File header
    
    private static ushort CalculateHeaderCrc(ReadOnlySpan<byte> buffer)
    {
        buffer = buffer.Slice(0,ListDataFileFormat.MaxSize - sizeof(ushort) /*CRC*/);
        return Crc16Ccit.Accumulate(buffer);
    }
    
    private static ushort ReadHeaderCrc(ReadOnlySpan<byte> buffer)
    {
        buffer = buffer.Slice(ListDataFileFormat.MaxSize - sizeof(ushort) /*CRC*/);
        return BinSerialize.ReadUShort(ref buffer);
    }
    private static void WriteHeaderCrc(Span<byte> buffer, ushort crc)
    {
        buffer = buffer.Slice(ListDataFileFormat.MaxSize - sizeof(ushort) /*CRC*/);
        BinSerialize.WriteUShort(ref buffer,crc);
    }

    public ListDataFileFormat? ReadHeader(string filePath)
    {
        using var file = _fileSystem.File.OpenRead(filePath);
        return ReadHeader(file);
    }
    public static void WriteHeader(Stream stream, ListDataFileFormat header)
    {
        var data = ArrayPool<byte>.Shared.Rent(ListDataFileFormat.MaxSize);
        for (var i = 0; i < ListDataFileFormat.MaxSize; i++)
        {
            data[i] = 0;
        }

        try
        {
            var serializeSpan = new Span<byte>(data, 0, ListDataFileFormat.MaxSize - 2 /* CRC*/);
            header.Serialize(ref serializeSpan);
            var originSpan = new Span<byte>(data, 0, ListDataFileFormat.MaxSize);
            var crc = CalculateHeaderCrc(originSpan);
            WriteHeaderCrc(originSpan,crc);
            stream.Seek(0, SeekOrigin.Begin);
            stream.Write(originSpan);
            stream.Flush();
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(data);
        }
    }
    public static ListDataFileFormat? ReadHeader(Stream stream)
    {
        if (stream.Length < ListDataFileFormat.MaxSize) return null;
        var header = new ListDataFileFormat();
        var data = ArrayPool<byte>.Shared.Rent(ListDataFileFormat.MaxSize);
        for (var i = 0; i < ListDataFileFormat.MaxSize; i++)
        {
            data[i] = 0;
        }

        try
        {
            var readSpan = new Span<byte>(data, 0, ListDataFileFormat.MaxSize);
            stream.Seek(0, SeekOrigin.Begin);
            var readCount = stream.Read(readSpan);
            Debug.Assert(readCount == ListDataFileFormat.MaxSize);
            
            var crc = CalculateHeaderCrc(readSpan);
            var readCrc = ReadHeaderCrc(readSpan);
            if (crc != readCrc) return null;
            
            var deserializeSpan = new ReadOnlySpan<byte>(data, 0, ListDataFileFormat.MaxSize);
            header.Deserialize(ref deserializeSpan);
            
            return header;
        }
        catch (Exception)
        {
            return null;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(data);
        }
    }
    
    #endregion

    private const byte StartRowMagicByte = 0x0A;
    private const byte StopRowMagicByte = 0x0D;
    
    private readonly Stream _stream;
    private readonly bool _disposeSteam;
    private readonly object _sync = new();
    private readonly uint _startFileMetadataOffset;
    private readonly ushort _fileMetadataSize;
    private readonly uint _startDataOffset;
    private readonly ushort _rowDataSize;
    private readonly ushort _rowSize;
    private readonly int _startRowByteIndex;
    private readonly int _stopRowByteIndex;
    private TMetadata _metadata;
    private IFileSystem _fileSystem;

    public ListDataFile(Stream stream, ListDataFileFormat header, bool disposeSteam, IFileSystem? fileSystem = null)
    {
        ArgumentNullException.ThrowIfNull(header);
        fileSystem ??= new FileSystem();
        _fileSystem = fileSystem;
        header.Validate();
        Header = header;
        _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        if (_stream.CanSeek == false) throw new Exception("Need stream with seek support");
        _disposeSteam = disposeSteam;
        _startFileMetadataOffset = ListDataFileFormat.MaxSize;
        _fileMetadataSize = header.MetadataMaxSize;
        _startDataOffset = (uint)(ListDataFileFormat.MaxSize + _fileMetadataSize);
        _rowDataSize = header.ItemMaxSize;
        _rowSize = (ushort)(1U /*StartRowMagicByte*/ + _rowDataSize /*MAX DATA SIZE*/ + 1U /*StopRowMagicByte*/ + 2U) /*CRC*/;
        _startRowByteIndex = 0;
        _stopRowByteIndex = _rowSize - 3;

        var data = ArrayPool<byte>.Shared.Rent(ListDataFileFormat.MaxSize);
        for (var i = 0; i < ListDataFileFormat.MaxSize; i++)
        {
            data[i] = 0;
        }

        try
        {
            if (_stream.Length < ListDataFileFormat.MaxSize)
            {
                // no header => write origin 
                WriteHeader(stream, header);
            }
            else
            {
                // header exist => check
                var readHeader = ReadHeader(stream);
                if (readHeader == null || readHeader.Equals(header) == false)
                {
                    throw new Exception($"Wrong file format. Want {header}, got {(readHeader == null ? "N/A" : readHeader.ToString())}");
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(data);
        }
        
        _metadata = new TMetadata();
        var buffer = ArrayPool<byte>.Shared.Rent(_fileMetadataSize);
        for (var i = 0; i < _fileMetadataSize; i++)
        {
            buffer[i] = 0;
        }

        try
        {
            var readSpan = new Span<byte>(buffer, 0, _fileMetadataSize);
            // if file have metadata => read
            if (_stream.Length >= ListDataFileFormat.MaxSize + _fileMetadataSize)
            {
                _stream.Seek(_startFileMetadataOffset, SeekOrigin.Begin);
                var readCount = _stream.Read(readSpan);
                Debug.Assert(readCount == _fileMetadataSize);
                var calc = CalculateMetadataCrc(readSpan);
                var readCrc = ReadMetadataCrc(readSpan);
                if (calc != readCrc) throw new Exception($"Unknown file metadata CRC. Want {calc}, got {readCrc}");
                var deserializeSpan = new ReadOnlySpan<byte>(buffer, 0, _fileMetadataSize - 2 /* without CRC*/);
                _metadata.Deserialize(ref deserializeSpan);
            }
            // if file have no metadata => write origin
            else
            {
                var serializeSpan = new Span<byte>(buffer, 0, _fileMetadataSize - 2 /* without CRC*/);
                _metadata.Serialize(ref serializeSpan);
                var calc = CalculateMetadataCrc(readSpan);
                WriteMetadataCrc(readSpan, calc);
                _stream.Seek(_startFileMetadataOffset, SeekOrigin.Begin);
                _stream.Write(readSpan);
                _stream.Flush();
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
    
    public long ByteSize
    {
        get
        {
            lock (_sync)    
            {
                return _stream.Length;
            }
        }
    }

    #region Metadata

    private ushort CalculateMetadataCrc(ReadOnlySpan<byte> metadata)
    {
        metadata = metadata.Slice(0,_fileMetadataSize - sizeof(ushort) /*CRC*/);
        return X25Crc.Accumulate(ref metadata, X25Crc.CrcSeed);
    }
    
    private ushort ReadMetadataCrc(ReadOnlySpan<byte> metadata)
    {
        metadata = metadata.Slice(_fileMetadataSize - sizeof(ushort) /*CRC*/);
        return BinSerialize.ReadUShort(ref metadata);
    }
    
    private void WriteMetadataCrc(Span<byte> metadata, ushort crc)
    {
        metadata = metadata.Slice(_fileMetadataSize - sizeof(ushort) /*CRC*/);
        BinSerialize.WriteUShort(ref metadata, crc);
    }
    
    public void EditMetadata(Action<TMetadata> editCallback)
    {
        var data = ArrayPool<byte>.Shared.Rent(_fileMetadataSize);
        for (var i = 0; i < _fileMetadataSize; i++)
        {
            data[i] = 0;
        }
        try
        {
            lock (_sync)
            {
                editCallback(_metadata);
                var serializeSpan = new Span<byte>(data, 0, _fileMetadataSize - 2 /* without CRC*/);
                _metadata.Serialize(ref serializeSpan);
                
                var writeSpan = new Span<byte>(data, 0, _fileMetadataSize);
                var calcCrc = CalculateMetadataCrc(writeSpan);
                WriteMetadataCrc(writeSpan, calcCrc);
                
                 _stream.Seek(_startFileMetadataOffset, SeekOrigin.Begin);
                _stream.Write(writeSpan);
                _stream.Flush();
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(data);
        }
    }
    
    public IListDataFileFormat Header { get; }

    public TMetadata ReadMetadata()
    {
        lock (_sync)
        {
            return _metadata.BinaryClone();    
        }
    }
    

    #endregion
    
    #region Row

    public object? Tag { get; set; }

    public uint Count
    {
        get
        {
            lock (_sync)    
            {
                return (uint)Math.Max((_stream.Length - _startDataOffset) / _rowSize, 0);
            }
        }
    }

    

    private void GetStartStopRowAddress(uint index, out uint start, out uint stop)
    {
        start = index * _rowSize + _startDataOffset;
        stop = start + _rowSize;
    }
    
    private ushort CalculateRowCrc(ReadOnlySpan<byte> row)
    {
        row = row.Slice(0, (int)(_rowSize - sizeof(ushort)) /*CRC*/);
        return X25Crc.Accumulate(ref row, X25Crc.CrcSeed);
    }
    
    private ushort ReadRowCrc(ReadOnlySpan<byte> row)
    {
        row = row.Slice((int)(_rowSize - sizeof(ushort)) /*CRC*/);
        return BinSerialize.ReadUShort(ref row);
    }
    
    private void WriteRowCrc(Span<byte> row, ushort crc)
    {
        row = row.Slice((int)(_rowSize - sizeof(ushort)) /*CRC*/);
        BinSerialize.WriteUShort(ref row, crc);
    }
    
    private bool ValidateRaw(ReadOnlySpan<byte> row)
    {
        if (row[_startRowByteIndex] != StartRowMagicByte) return false;
        if (row[_stopRowByteIndex] != StopRowMagicByte) return false;
        var calculatedCrc = CalculateRowCrc(row);
        var readCrc = ReadRowCrc(row);
        return calculatedCrc == readCrc;
    }
    
    public bool Exist(uint index)
    {
        GetStartStopRowAddress(index, out var start, out var stop);
        
        var data = ArrayPool<byte>.Shared.Rent(_rowSize);
        try
        {
            var buffer = new Span<byte>(data, 0, _rowSize);
            lock (_sync)
            {
                if (_stream.Length <= stop) return false;
                _stream.Seek(start, SeekOrigin.Begin);
                var readBytes = _stream.Read(buffer);
                Debug.Assert(readBytes == _rowSize);
            }
            return ValidateRaw(buffer);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(data);
        }
    }

    public void Write(uint index, ISpanSerializable payload)
    {
        GetStartStopRowAddress(index, out var start, out var _);
        var data = ArrayPool<byte>.Shared.Rent(_rowSize);
        for (var i = 0; i < _rowSize; i++)
        {
            data[i] = 0;
        }
        try
        {
            var row = new Span<byte>(data, 0, _rowSize);
            var buffer = row;
            BinSerialize.WriteByte(ref buffer, StartRowMagicByte);
            payload.Serialize(ref buffer);
            data[_stopRowByteIndex] = StopRowMagicByte;
            var calcCrc = CalculateRowCrc(row);
            WriteRowCrc(row, calcCrc);
            lock (_sync)
            {
                _stream.Seek(start, SeekOrigin.Begin);
                _stream.Write(row);
                _stream.Flush();
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(data);
        }
    }

    public bool Read(uint index, ISpanSerializable payload)
    {
        GetStartStopRowAddress(index, out var start, out var stop);
        var data = ArrayPool<byte>.Shared.Rent((int)_rowSize);
        try
        {
            var row = new Span<byte>(data, 0, (int)_rowSize);
            lock (_sync)
            {
                // there is no data
                if (_stream.Length < stop) return false;
                _stream.Seek(start, SeekOrigin.Begin);
                var readBytes = _stream.Read(row);
                Debug.Assert(readBytes == _rowSize);
            }
            if (ValidateRaw(row) == false) return false;
            ReadOnlySpan<byte> buffer = row.Slice(1/*START BYTE*/, (int)_rowDataSize);
            payload.Deserialize(ref buffer);
            return true;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(data);
        }
    }

    #endregion
    
    protected override void InternalDisposeOnce()
    {
        lock (_sync)
        {
            _stream.Flush();
            if (_disposeSteam) _stream.Dispose();
        }
    }
}