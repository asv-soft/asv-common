using System;

namespace Asv.IO;

public interface IVirtualConnection : IDisposable, IAsyncDisposable
{
    void SetClientToServerFilter(Func<IProtocolMessage, bool> filter);
    void SetServerToClientFilter(Func<IProtocolMessage, bool> filter);
    IStatistic Statistic { get; }
    IProtocolConnection Server { get; }
    IProtocolConnection Client { get; }
}

public static class VirtualConnectionHelper
{
    public static void SetClientToServerFilterByType<TMessage>(
        this IVirtualConnection src,
        Func<TMessage, bool> filter
    )
        where TMessage : IProtocolMessage
    {
        src.SetClientToServerFilter(message => message is TMessage m && filter(m));
    }

    public static void SetServerToClientFilter<TMessage>(
        this IVirtualConnection src,
        Func<TMessage, bool> filter
    )
    {
        src.SetClientToServerFilter(message => message is TMessage m && filter(m));
    }
}
