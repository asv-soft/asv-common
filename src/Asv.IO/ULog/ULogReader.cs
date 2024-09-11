using System;
using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Asv.IO;

public enum ULogToken
{
    Unknown,
    FileHeader,
    FlagBits,
    Format,
}

public interface IULogReader
{
    bool TryRead(ref SequenceReader<byte> rdr,out IULogToken? token);
    IULogToken? CurrentToken { get; }
    
    public bool TryRead<TToken>(ref SequenceReader<byte> rdr,out TToken? token) 
        where TToken : class, IULogToken
    {
        var result = TryRead(ref rdr, out var t);
        token = t as TToken;
        Debug.Assert(token != null);
        return result;
    }
}

public class ULogReader:IULogReader
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
    
    public bool TryRead(ref SequenceReader<byte> rdr,out IULogToken? token)
    {
        token = null;
        if (_tokenCounter == 0)
        {
            var headerBuffer = ArrayPool<byte>.Shared.Rent(ULogFileHeaderToken.HeaderSize);
            try
            {
                if (rdr.TryCopyTo(new Span<byte>(headerBuffer, 0, ULogFileHeaderToken.HeaderSize)) == false) return false;
                rdr.Advance(ULogFileHeaderToken.HeaderSize);
                var span = new ReadOnlySpan<byte>(headerBuffer, 0, ULogFileHeaderToken.HeaderSize);
                token = new ULogFileHeaderToken();
                token.Deserialize(ref span);
                ++_tokenCounter;
                return true;
            }
            finally
            { 
                ArrayPool<byte>.Shared.Return(headerBuffer);
            }
            
        }

        if (rdr.TryReadLittleEndian(out ushort size) == false) return false;
        if (rdr.TryRead(out var type) == false)
        {
            rdr.Rewind(sizeof(ushort)); // rewind size
            return false;
        }

        var payloadBuffer = ArrayPool<byte>.Shared.Rent(size);
        try
        {
            if (rdr.TryCopyTo(new Span<byte>(payloadBuffer, 0, size)) == false)
            {
                rdr.Rewind(sizeof(ushort) + sizeof(byte));
                return false;
            }
            rdr.Advance(size);
            token = _factory.TryGetValue(type, out var tokenFactory) ? tokenFactory() : new ULogUnknownToken(type, size);
            var readSpan = new ReadOnlySpan<byte>(payloadBuffer,0,size);
            token.Deserialize(ref readSpan);
            _tokenCounter++;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(payloadBuffer);
        }
        
        
        // ULogMessageFlagBits message must be the first message right after the header section, so that it has a fixed constant offset from the start of the file!
        // (https://docs.px4.io/main/en/dev_log/ulog_file_format.html#d-logged-data-message)
        if (_tokenCounter == 1)
        {
            if (token.Type != ULogFlagBitsMessageToken.Token)
            {
                throw new ULogException($"{ULogFlagBitsMessageToken.Token:G} message must be right after the header section");
            }
        }
        CurrentToken = token;
        ++_tokenCounter;
        return true;

    }

    public IULogToken? CurrentToken { get; private set; }
}