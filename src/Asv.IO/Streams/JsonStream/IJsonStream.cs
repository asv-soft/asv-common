using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using R3;

namespace Asv.IO
{
    public interface IJsonStream : IDisposable
    {
        Observable<Exception> OnError { get; }
        Observable<JObject> OnData { get; }

        Task SendText(string data, CancellationToken cancel);
        Task Send<T>(T data, CancellationToken cancel);

        Task<JObject> RequestText(
            string request,
            Func<JObject, bool> responseFilter,
            CancellationToken cancel
        );
        Task<JObject> Request<TRequest>(
            TRequest request,
            Func<JObject, bool> responseFilter,
            CancellationToken cancel
        );
    }
}
