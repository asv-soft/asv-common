using Asv.Common;

namespace Asv.Common;

public static class CircularBufferMixin
{
    public static void EnsureCapacity<T>(ref CircularBuffer2<T> buffer, int capacity)
    {
        if (buffer.Size > capacity)
        {
            var newBuffer = new CircularBuffer2<T>(capacity);
            for (var i = 0; i < capacity; i++)
            {
                newBuffer.PushFront(buffer[i]);
            }

            buffer = newBuffer;
        }

        if (buffer.Size < capacity)
        {
            var newBuffer = new CircularBuffer2<T>(capacity);
            for (var i = 0; i < buffer.Size; i++)
            {
                newBuffer.PushFront(buffer[i]);
            }

            buffer = newBuffer;
        }
    }
}
