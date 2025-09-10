using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using R3;

namespace Asv.IO
{
    public class JsonStreamBase : IJsonStream, IDisposable, IAsyncDisposable
    {
        private readonly CancellationTokenSource _disposeCancel = new();
        private readonly Subject<Exception> _onErrorSubject = new();

        private readonly ITextStream _textStream;
        private readonly bool _disposeStream;
        private readonly TimeSpan _requestTimeout;
        private readonly IDisposable _sub1;
        private readonly IDisposable _sub2;
        private readonly Subject<JObject> _onData = new();

        public JsonStreamBase(ITextStream textStream, bool disposeStream, TimeSpan requestTimeout)
        {
            _textStream = textStream;
            _disposeStream = disposeStream;
            _requestTimeout = requestTimeout;
            _sub1 = _textStream.OnError.Subscribe(_onErrorSubject.AsObserver());
            _sub2 = _textStream
                .OnReceive.Select(SafeConvert)
                .Where(x => x != null)
                .Cast<JObject?, JObject>()
                .Subscribe(_onData.AsObserver());
        }

        private JObject? SafeConvert(string s)
        {
            try
            {
                return JsonConvert.DeserializeObject<JObject>(s);
            }
            catch (Exception ex)
            {
                _onErrorSubject.OnNext(ex);
                return null;
            }
        }

        public Observable<Exception> OnError => _onErrorSubject;
        public Observable<JObject> OnData => _onData;

        public async Task Send<T>(T data, CancellationToken cancel)
        {
            try
            {
                var str = JsonConvert.SerializeObject(data, Formatting.None);
                await _textStream.Send(str, cancel).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _onErrorSubject.OnNext(ex);
            }
        }

        public async Task<JObject> RequestText(
            string request,
            Func<JObject, bool> responseFilter,
            CancellationToken cancel
        )
        {
            using var linkedCancel = CancellationTokenSource.CreateLinkedTokenSource(cancel);
            linkedCancel.CancelAfter(_requestTimeout);
            var tcs = new TaskCompletionSource<JObject>();
            using var c1 = linkedCancel.Token.Register(tcs.SetCanceled);
            using var subscribe = _onData.Where(responseFilter).Take(1).Subscribe(tcs.SetResult);
            await SendText(request, linkedCancel.Token).ConfigureAwait(false);
            return await tcs.Task.ConfigureAwait(false);
        }

        public async Task SendText(string data, CancellationToken cancel)
        {
            try
            {
                await _textStream.Send(data, cancel).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _onErrorSubject.OnNext(ex);
            }
        }

        public async Task<JObject> Request<TRequest>(
            TRequest request,
            Func<JObject, bool> responseFilter,
            CancellationToken cancel
        )
        {
            using CancellationTokenSource linkedCancel =
                CancellationTokenSource.CreateLinkedTokenSource(cancel);
            linkedCancel.CancelAfter(_requestTimeout);
            var tcs = new TaskCompletionSource<JObject>();
            using var c1 = linkedCancel.Token.Register(tcs.SetCanceled);
            using var subscribe = _onData.Where(responseFilter).Take(1).Subscribe(tcs.SetResult);
            await Send(request, linkedCancel.Token).ConfigureAwait(false);
            return await tcs.Task.ConfigureAwait(false);
        }

        #region Dispose

        public void Dispose()
        {
            _disposeCancel.Cancel(false);
            _disposeCancel.Dispose();
            _onErrorSubject.Dispose();
            _onData.Dispose();
            if (_disposeStream)
            {
                _textStream.Dispose();
            }

            _sub1.Dispose();
            _sub2.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            _disposeCancel.Cancel(false);
            await CastAndDispose(_disposeCancel);
            await CastAndDispose(_onErrorSubject);
            await CastAndDispose(_onData);
            if (_disposeStream)
            {
                await _textStream.DisposeAsync();
            }

            await CastAndDispose(_sub1);
            await CastAndDispose(_sub2);

            return;

            static async ValueTask CastAndDispose(IDisposable resource)
            {
                if (resource is IAsyncDisposable resourceAsyncDisposable)
                {
                    await resourceAsyncDisposable.DisposeAsync();
                }
                else
                {
                    resource.Dispose();
                }
            }
        }

        #endregion

        public IDisposable Subscribe(IObserver<JObject> observer)
        {
            throw new NotImplementedException();
        }
    }
}
