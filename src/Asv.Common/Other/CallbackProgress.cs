using System;

namespace Asv.Common
{
    public class CallbackProgress<T> : IProgress<T>
    {
#pragma warning disable SA1401 // Preserve the public field for binary compatibility.
        public static Progress<T> Default = new(_ => { });
#pragma warning restore SA1401

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
