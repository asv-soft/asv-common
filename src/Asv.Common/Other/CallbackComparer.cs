using System;
using System.Collections.Generic;

namespace Asv.Common
{
    public class CallbackComparer<T>(Func<T, int> getHashCodeFunc, Func<T?, T?, bool> equalsFunc)
        : IEqualityComparer<T>
    {
        public bool Equals(T? x, T? y)
        {
            return equalsFunc(x, y);
        }

        public int GetHashCode(T obj)
        {
            return getHashCodeFunc(obj);
        }
    }
}
