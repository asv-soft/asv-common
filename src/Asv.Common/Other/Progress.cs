using System;

namespace Asv.Common
{
    public class CallbackProgress<T> : IProgress<T>
    {
        public static Progress<T> Default = new(_ => { });

        private readonly Action<T> _callback;

        public CallbackProgress(Action<T> callback)
        {
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public void Report(T value)
        {
            _callback(value);
        }
    }

}
