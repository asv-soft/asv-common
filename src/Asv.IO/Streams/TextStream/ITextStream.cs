using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Asv.IO
{
    public interface ITextStream : IDisposable, IObservable<string>
    {
        IObservable<Exception> OnError { get; }

        Task Send(string value, CancellationToken cancel);
    }


    public static class TextStreamHelper
    {
        public static async Task<string> RequestText(this ITextStream strm,string request, int timeoutMs, CancellationToken cancel)
        {
            using var linkedCancel = CancellationTokenSource.CreateLinkedTokenSource(cancel);
            linkedCancel.CancelAfter(timeoutMs);
            var tcs = new TaskCompletionSource<string>();
            using var c1 = linkedCancel.Token.Register(tcs.SetCanceled);
            using var subscribe = strm.FirstAsync().Subscribe(tcs.SetResult);
            await strm.Send(request, linkedCancel.Token);
            return await tcs.Task.ConfigureAwait(false);
        }
    }
}
