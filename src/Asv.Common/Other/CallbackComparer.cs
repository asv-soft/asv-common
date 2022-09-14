using System;
using System.Collections.Generic;

namespace Asv.Common
{
    public class CallbackComparer<T> : IEqualityComparer<T>
    {
        private readonly Func<T, int> _getHashCodeFunc;
        private readonly Func<T, T, bool> _equalsFunc;

        public CallbackComparer(Func<T, int> getHashCodeFunc, Func<T, T, bool> equalsFunc)
        {
            _getHashCodeFunc = getHashCodeFunc;
            _equalsFunc = equalsFunc;
        }

        public bool Equals(T x, T y)
        {
            return _equalsFunc(x, y);
        }

        public int GetHashCode(T obj)
        {
            return _getHashCodeFunc(obj);
        }
    }
}
