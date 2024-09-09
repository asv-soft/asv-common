using System;
using System.Buffers;
using System.Collections.Immutable;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Asv.IO;

public enum ULogToken
{
    Unknown = 0,
    FileHeader = 1,
    FlagBits
}

public class ULogReader
{
    private readonly ImmutableDictionary<byte, Func<IULogToken>> _factory;
    private readonly ILogger _logger;
    private int _tokenCounter;

    public ULogReader(ImmutableDictionary<byte,Func<IULogToken>> factory, ILogger? logger = null)
    {
        _factory = factory;
        _logger = logger ?? NullLogger.Instance;
        _tokenCounter = 0;
    }

    
    
    public bool TryRead(ReadOnlySequence<byte> data,out IULogToken? token)
    {
        var rdr = new SequenceReader<byte>(data);
        token = null;
        if (_tokenCounter == 0)
        {
            unsafe
            {
                var buffer = stackalloc byte[ULogTokenFileHeader.HeaderSize];
                rdr.TryCopyTo(new Span<byte>(buffer, ULogTokenFileHeader.HeaderSize));
                token = new ULogTokenFileHeader();
                var span = new ReadOnlySpan<byte>(buffer, ULogTokenFileHeader.HeaderSize);
                token.Deserialize(ref span);
                ++_tokenCounter;
                return true;
            }
        }
        
        if (rdr.TryRead(out var type) == false) return false;
        if (rdr.TryReadLittleEndian(out ushort size) == false)
        {
            rdr.Rewind(sizeof(byte)); // move back 1 byte (type)
            return false;
        }

        unsafe
        {
            var buffer = stackalloc byte[size];
            rdr.TryCopyTo(new Span<byte>(buffer, size));
            token = _factory.TryGetValue(type, out var tokenFactory) ? tokenFactory() : new ULogTokenUnknown(type, size);
            var readSpan = new ReadOnlySpan<byte>(buffer,size);
            token.Deserialize(ref readSpan);
        }
        // ULogMessageFlagBits message must be the first message right after the header section, so that it has a fixed constant offset from the start of the file!
        // (https://docs.px4.io/main/en/dev_log/ulog_file_format.html#d-logged-data-message)
        if (_tokenCounter == 1)
        {
            if (token.Type != ULogMessageFlagBits.Token)
            {
                throw new ULogException($"{ULogMessageFlagBits.Token:G} message must be right after the header section");
            }
        }
        CurrentToken = token;
        ++_tokenCounter;
        return true;

    }

    public IULogToken? CurrentToken { get; private set; }
}