using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Asv.IO
{
    public interface IJsonStream : IDisposable, IObservable<JObject>
    {
        IObservable<Exception> OnError { get; }

        Task SendText(string data, CancellationToken cancel);
        Task Send<T>(T data, CancellationToken cancel);

        Task<JObject> RequestText(string request, Func<JObject, bool> responseFilter, CancellationToken cancel);
        Task<JObject> Request<TRequest>(TRequest request, Func<JObject, bool> responseFilter, CancellationToken cancel);
    }
}
