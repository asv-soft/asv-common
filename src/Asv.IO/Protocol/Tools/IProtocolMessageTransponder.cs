using System;
using R3;

namespace Asv.IO;

/// <summary>
/// Represents the state of a packet transponder.
/// </summary>
public enum TransponderState
{
    /// <summary>
    /// Represents the state of a packet transponder when it is "Ok".
    /// </summary>
    Ok,

    /// <summary>
    /// Represents the state of a packet transponder when it has been skipped.
    /// </summary>
    /// <remarks>
    /// This state indicates that the packet transponder was skipped for processing.
    /// </remarks>
    Skipped,

    /// <summary>
    /// Represents the error state when sending a packet.
    /// </summary>
    ErrorToSend,
}

public interface IProtocolMessageTransponder<out TMessage> : IDisposable, IAsyncDisposable
    where TMessage : IProtocolMessage
{
    void Start(TimeSpan dueTime, TimeSpan period);
    void Set(Action<TMessage> changeCallback);
    void Stop();
    bool IsStarted { get; }
    ReadOnlyReactiveProperty<TransponderState> State { get; }
}