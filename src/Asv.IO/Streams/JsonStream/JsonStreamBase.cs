using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Asv.IO
{
    public class JsonStreamBase : IJsonStream, IDisposable, IObservable<JObject>
    {
        private readonly CancellationTokenSource _disposeCancel = new();
        private readonly Subject<Exception> _onErrorSubject = new();
        private readonly Subject<JObject> _onData = new();
        private readonly ITextStream _textStream;
        private readonly bool _disposeStream;
        private readonly TimeSpan _requestTimeout;

        public JsonStreamBase(ITextStream textStream, bool disposeStream, TimeSpan requestTimeout)
        {
            this._textStream = textStream;
            this._disposeStream = disposeStream;
            this._requestTimeout = requestTimeout;
            this._textStream.OnError.Subscribe((IObserver<Exception>)this._onErrorSubject);
            this._textStream.Select<string, JObject>(new Func<string, JObject>(this.SafeConvert)).Where<JObject>((Func<JObject, bool>)(_ => _ != null)).Subscribe<JObject>((IObserver<JObject>)this._onData, this._disposeCancel.Token);
        }

        private JObject SafeConvert(string s)
        {
            try
            {
                return JsonConvert.DeserializeObject<JObject>(s);
            }
            catch (Exception ex)
            {
                this._onErrorSubject.OnNext(ex);
                return (JObject)null;
            }
        }

        public IObservable<Exception> OnError
        {
            get
            {
                return (IObservable<Exception>)this._onErrorSubject;
            }
        }

        public async Task Send<T>(T data, CancellationToken cancel)
        {
            try
            {
                string str = JsonConvert.SerializeObject((object)(T)data, Formatting.None);
                await this._textStream.Send(str, cancel).ConfigureAwait(false);
                str = (string)null;
            }
            catch (Exception ex)
            {
                this._onErrorSubject.OnNext(ex);
            }
        }

        public async Task<JObject> RequestText(string request, Func<JObject, bool> responseFilter,
            CancellationToken cancel)
        {
            using var linkedCancel = CancellationTokenSource.CreateLinkedTokenSource(cancel);
            linkedCancel.CancelAfter(_requestTimeout);
            var tcs = new TaskCompletionSource<JObject>();
            using var c1 = linkedCancel.Token.Register(tcs.SetCanceled);
            using var subscribe = this.FirstAsync(responseFilter).Subscribe(tcs.SetResult);
            await SendText(request, linkedCancel.Token).ConfigureAwait(false);
            return  await tcs.Task.ConfigureAwait(false);
        }

        public async Task SendText(string data, CancellationToken cancel)
        {
            try
            {
                await _textStream.Send(data, cancel).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this._onErrorSubject.OnNext(ex);
            }
        }

        public async Task<JObject> Request<TRequest>(TRequest request, Func<JObject, bool> responseFilter,
            CancellationToken cancel)
        {
            using CancellationTokenSource linkedCancel = CancellationTokenSource.CreateLinkedTokenSource(cancel);
            linkedCancel.CancelAfter(_requestTimeout);
            var tcs = new TaskCompletionSource<JObject>();
            using var c1 = linkedCancel.Token.Register(tcs.SetCanceled);
            using var subscribe = this.FirstAsync(responseFilter).Subscribe(tcs.SetResult);
            await Send(request, linkedCancel.Token).ConfigureAwait(false);
            return await tcs.Task.ConfigureAwait(false);
        }

        public void Dispose()
        {
            _disposeCancel.Cancel(false);
            _disposeCancel.Dispose();
            if (!_disposeStream)
                return;
            _textStream?.Dispose();
        }

        public IDisposable Subscribe(IObserver<JObject> observer)
        {
            return _onData.Subscribe(observer);
        }
    }

}
