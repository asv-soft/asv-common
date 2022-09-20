using System;
using System.Threading;
using System.Threading.Tasks;
using Asv.Common;

namespace Asv.IO
{
    public interface ITextStream : IDisposable, IObservable<string>
    {
        IRxValue<PortState> OnPortState { get; }

        IObservable<Exception> OnError { get; }

        Task Send(string value, CancellationToken cancel);
    }
}
