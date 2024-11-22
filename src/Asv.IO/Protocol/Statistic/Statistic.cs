using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Asv.IO;

public class Statistic : IStatisticHandler
{
    private uint _rxBytes;
    private uint _txBytes;
    private uint _rxMessages;
    private uint _txMessages;
    private uint _rxError;
    private uint _txError;
    private uint _parsedBytes;
    private uint _parsedMessages;
    private uint _unknownMessages;
    private uint _messagePublishError;
    private uint _badCrc;
    private uint _deserializeError;
    private uint _messageReadNotAllData;
    
    public uint RxBytes => _rxBytes;
    public uint TxBytes => _txBytes;
    public uint RxMessages => _rxMessages;
    public uint TxMessages => _txMessages;
    public uint RxError => _rxError;
    public uint TxError => _txError;
    public uint ParsedBytes => _parsedBytes;
    public uint ParsedMessages => _parsedMessages;
    public uint UnknownMessages => _unknownMessages;
    public uint MessagePublishError => _messagePublishError;
    public uint BadCrcError => _badCrc;
    public uint DeserializeError => _deserializeError;
    public uint MessageReadNotAllData => _messageReadNotAllData;
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRxBytes(int size) => Interlocked.Add(ref _rxBytes, (uint)size);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddTxBytes(int size) => Interlocked.Add(ref _txBytes, (uint)size);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncrementRxMessage() => Interlocked.Increment(ref _rxMessages);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncrementTxMessages() => Interlocked.Increment(ref _txMessages);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncrementRxError() => Interlocked.Increment(ref _rxError);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncrementTxError() => Interlocked.Increment(ref _txError);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddParserBytes(int size) => Interlocked.Add(ref _parsedBytes, (uint)size);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncrementParsedMessage() => Interlocked.Increment(ref _parsedMessages);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncrementParserUnknownMessageError() => Interlocked.Increment(ref _unknownMessages);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncrementParserBadCrcError() => Interlocked.Increment(ref _badCrc);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncrementParserDeserializeError() => Interlocked.Increment(ref _deserializeError);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncrementParserReadNotAllDataError() => Interlocked.Increment(ref _messageReadNotAllData);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void IncrementParserPublishError() => Interlocked.Increment(ref _messagePublishError);
}

public class InheritedStatistic(IStatisticHandler parent) : IStatisticHandler
{
    private uint _rxBytes;
    private uint _txBytes;
    private uint _rxMessages;
    private uint _txMessages;
    private uint _rxError;
    private uint _txError;
    private uint _parsedBytes;
    private uint _parsedMessages;
    private uint _unknownMessages;
    private uint _messagePublishError;
    private uint _badCrc;
    private uint _deserializeError;
    private uint _messageReadNotAllData;
    
    public uint RxBytes => _rxBytes;
    public uint TxBytes => _txBytes;
    public uint RxMessages => _rxMessages;
    public uint TxMessages => _txMessages;
    public uint RxError => _rxError;
    public uint TxError => _txError;
    public uint ParsedBytes => _parsedBytes;
    public uint ParsedMessages => _parsedMessages;
    public uint UnknownMessages => _unknownMessages;
    public uint MessagePublishError => _messagePublishError;
    public uint BadCrcError => _badCrc;
    public uint DeserializeError => _deserializeError;
    public uint MessageReadNotAllData => _messageReadNotAllData;
    
    public void AddRxBytes(int size)
    {
        Interlocked.Add(ref _rxBytes, (uint)size);
        parent.AddRxBytes(size);
    }

    public void AddTxBytes(int size)
    {
        Interlocked.Add(ref _txBytes, (uint)size);
        parent.AddTxBytes(size);
    }

    public void IncrementRxMessage()
    {
        Interlocked.Increment(ref _rxMessages);
        parent.IncrementRxMessage();
    }

    public void IncrementTxMessages()
    {
        Interlocked.Increment(ref _txMessages);
        parent.IncrementTxMessages();
    }

    public void IncrementRxError()
    {
        Interlocked.Increment(ref _rxError);
        parent.IncrementRxError();
    }

    public void IncrementTxError()
    {
        Interlocked.Increment(ref _txError);
        parent.IncrementTxError();
    }

    public void AddParserBytes(int size)
    {
        Interlocked.Add(ref _parsedBytes, (uint)size);
        parent.AddParserBytes(size);
    }

    public void IncrementParsedMessage()
    {
        Interlocked.Increment(ref _parsedMessages);
        parent.IncrementParsedMessage();
    }

    public void IncrementParserUnknownMessageError()
    {
        Interlocked.Increment(ref _unknownMessages);
        parent.IncrementParserUnknownMessageError();
    }

    public void IncrementParserBadCrcError()
    {
        Interlocked.Increment(ref _badCrc);
        parent.IncrementParserBadCrcError();
    }

    public void IncrementParserDeserializeError()
    {
        Interlocked.Increment(ref _deserializeError);
        parent.IncrementParserDeserializeError();
    }

    public void IncrementParserReadNotAllDataError()
    {
        Interlocked.Increment(ref _messageReadNotAllData);
        parent.IncrementParserReadNotAllDataError();
    }

    public void IncrementParserPublishError()
    {
        Interlocked.Increment(ref _messagePublishError);
        parent.IncrementParserPublishError();
    }
}

public enum ParserStatistic
{
    RxBytes,
    RxMessages,
    UnknownMessageError,
    BadCrcError,
    DeserializeError,
    ReadNotAllDataError,
    PublishError
}

public struct ParserStatisticKey(ProtocolInfo info, ParserStatistic statistic) : IEquatable<ParserStatisticKey>
{
    public ProtocolInfo Info { get; } = info;
    public ParserStatistic Statistic { get; } = statistic;

    public bool Equals(ParserStatisticKey other)
    {
        return Info.Equals(other.Info) && Statistic == other.Statistic;
    }

    public override bool Equals(object? obj)
    {
        return obj is ParserStatisticKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Info, (int)Statistic);
    }

    public static bool operator ==(ParserStatisticKey left, ParserStatisticKey right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ParserStatisticKey left, ParserStatisticKey right)
    {
        return !left.Equals(right);
    }
}