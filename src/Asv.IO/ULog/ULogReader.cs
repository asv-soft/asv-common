using System;
using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Emit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Exception = System.Exception;

namespace Asv.IO;

public enum ULogToken
{
    Unknown,
    FileHeader,
    FlagBits,
    Format,
    Information,
    MultiInformation,
    Parameter,
    DefaultParameter,
    Unsubscription,
    Subscription,
    LoggedData,
    LoggedString,
    Synchronization,
    TaggedLoggedString,
    Dropout
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

public class ULogReader(ImmutableDictionary<byte, Func<IULogToken>> factory, ILogger? logger = null)
    : IULogReader
{
    private ReaderState _state = ReaderState.HeaderSection;

    public bool TryRead(ref SequenceReader<byte> rdr,out IULogToken? token)
    {
        token = null;
        switch (_state)
        {
            case ReaderState.HeaderSection:
                // always start with header
                if (!InternalReadHeader(ref rdr, ref token)) return false;
                _state = ReaderState.FlagBitsMessage;
                return true;
            case ReaderState.FlagBitsMessage:
                // must be right after header
                if (!InternalReadToken(ref rdr, ref token)) return false;
                Debug.Assert(token != null);
                if (token.TokenType != ULogToken.FlagBits)
                {
                    _state = ReaderState.Corrupted;
                    // (https://docs.px4.io/main/en/dev_log/ulog_file_format.html#d-logged-data-message)
                    throw new ULogException($"{ULogToken.FlagBits:G} must be right after {ReaderState.HeaderSection:G}, but got {token.TokenType:G}");
                }
                _state = ReaderState.DataSection;
                break;
           case ReaderState.DefinitionSection:
                // definition section doesn't have Token Synchronization message
                if (!InternalReadToken(ref rdr, ref token)) return false;
                Debug.Assert(token != null);
                // if we read all definition tokens, then we can switch to data section
                if (token.TokenSection.HasFlag(TokenPlaceFlags.Data))
                {
                    _state = ReaderState.DataSection;
                }
                break;
           case ReaderState.DataSection:
                // data section can have Token Synchronization message
                // if token we couldn't read token (exception occured), then it's corrupted data and we need to find sync message
                try
                {
                    if (!InternalReadToken(ref rdr, ref token)) return false;
                }
                catch (ULogException e)
                {
                    _state = ReaderState.Corrupted;
                    goto corrupted; // uff, I'm so sorry for this goto
                }
                break;
            case ReaderState.Corrupted:
                corrupted:
                // TODO: try to find sync message and switch to DataSection
                throw new Exception("Corrupted ULog file. Sync message not implemented.");
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        CurrentToken = token;
        return true;

    }

    private bool InternalReadToken(ref SequenceReader<byte> rdr, ref IULogToken? token)
    {
        if (rdr.TryReadLittleEndian(out ushort size) == false) return false;
        if (rdr.TryRead(out var type) == false)
        {
            rdr.Rewind(sizeof(ushort)); // rewind size
            return false;
        }

        var payloadBuffer = ArrayPool<byte>.Shared.Rent(size);
        try
        {
            if (rdr.TryCopyTo(new Span<byte>(payloadBuffer,0, size)) == false) return false;
            token = factory.TryGetValue(type, out var tokenFactory) ? tokenFactory() : new ULogUnknownToken(type, size);
            var readSpan = new ReadOnlySpan<byte>(payloadBuffer,0,size);
            token.Deserialize(ref readSpan);
            rdr.Advance(size);  // advance only if token was read successfully
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(payloadBuffer);
        }
        return true;
    }

    private bool InternalReadHeader(ref SequenceReader<byte> rdr, ref IULogToken? token)
    {
        var headerBuffer = ArrayPool<byte>.Shared.Rent(ULogFileHeaderToken.HeaderSize);
        try
        {
            if (rdr.TryCopyTo(new Span<byte>(headerBuffer, 0, ULogFileHeaderToken.HeaderSize)) == false) return false;
            rdr.Advance(ULogFileHeaderToken.HeaderSize);
            var span = new ReadOnlySpan<byte>(headerBuffer, 0, ULogFileHeaderToken.HeaderSize);
            token = new ULogFileHeaderToken();
            token.Deserialize(ref span);
            return true;
        }
        finally
        { 
            ArrayPool<byte>.Shared.Return(headerBuffer);
        }
    }

    private enum ReaderState
    {
        HeaderSection,
        FlagBitsMessage,
        DataSection,
        Corrupted,
        DefinitionSection
    }
    public IULogToken? CurrentToken { get; private set; }
}