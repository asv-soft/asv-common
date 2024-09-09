using System;
using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using ZLogger;

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
        token = null;
        if (_tokenCounter == 0)
        {
            token = new ULogTokenFileHeader();
            if (token.TryRead(data) == false) return false;
            ++_tokenCounter;
            return true;
        }
        var rdr = new SequenceReader<byte>(data);
        if (rdr.TryRead(out byte type) == false) return false;
        if (rdr.TryReadLittleEndian(out ushort size) == false)
        {
            rdr.Rewind(sizeof(byte)); // move back 1 byte (type)
            return false;
        }
        if (rdr.TryReadExact(size, out var payload) == false)
        {
            rdr.Rewind(sizeof(byte) + sizeof(ushort)); // move back 1 (type:byte) + 2 (size:ushort) bytes
            return false;
        }
        // unknown token
        token = _factory.TryGetValue(type, out var tokenFactory) ? tokenFactory() : new ULogTokenUnknown(type, size);
        if (token.TryRead(payload) == false)
        {
            rdr.Rewind(sizeof(byte) + sizeof(ushort)); // move back 1 (type:byte) + 2 (size:ushort) bytes
            return false;
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
        return true;

    }

    public IULogToken? CurrentToken { get; private set; }
}