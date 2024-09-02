using System;
using System.Collections.Generic;
using Xunit;
using Asv.Common;

public class CircularBuffer2Tests
{
    [Fact]
    public void Constructor_ShouldInitializeWithCapacity()
    {
        int capacity = 5;
        var buffer = new CircularBuffer2<int>(capacity);

        Assert.Equal(capacity, buffer.Capacity);
        Assert.True(buffer.IsEmpty);
    }

    [Fact]
    public void Constructor_WithInitialItems_ShouldInitializeBuffer()
    {
        int capacity = 5;
        var items = new[] { 1, 2, 3 };
        var buffer = new CircularBuffer2<int>(capacity, items);

        Assert.Equal(items.Length, buffer.Size);
        Assert.Equal(1, buffer.Front());
        Assert.Equal(3, buffer.Back());
    }

    [Fact]
    public void PushBack_ShouldAddElementToBack()
    {
        var buffer = new CircularBuffer2<int>(3);

        buffer.PushBack(1);
        buffer.PushBack(2);

        Assert.Equal(2, buffer.Size);
        Assert.Equal(1, buffer.Front());
        Assert.Equal(2, buffer.Back());
    }

    [Fact]
    public void PushBack_WhenFull_ShouldOverwriteFrontElement()
    {
        var buffer = new CircularBuffer2<int>(3);
        buffer.PushBack(1);
        buffer.PushBack(2);
        buffer.PushBack(3);

        // Buffer is now full, next push will overwrite the front
        buffer.PushBack(4);

        Assert.Equal(3, buffer.Size);
        Assert.Equal(2, buffer.Front());
        Assert.Equal(4, buffer.Back());
    }

    [Fact]
    public void PushFront_ShouldAddElementToFront()
    {
        var buffer = new CircularBuffer2<int>(3);

        buffer.PushBack(1);
        buffer.PushBack(2);

        buffer.PushFront(0);

        Assert.Equal(3, buffer.Size);
        Assert.Equal(0, buffer.Front());
        Assert.Equal(2, buffer.Back());
    }

    [Fact]
    public void PushFront_WhenFull_ShouldOverwriteBackElement()
    {
        var buffer = new CircularBuffer2<int>(3);
        buffer.PushBack(1);
        buffer.PushBack(2);
        buffer.PushBack(3);

        // Buffer is now full, next push will overwrite the back
        buffer.PushFront(0);

        Assert.Equal(3, buffer.Size);
        Assert.Equal(0, buffer.Front());
        Assert.Equal(2, buffer.Back());
    }

    [Fact]
    public void PopBack_ShouldRemoveElementFromBack()
    {
        var buffer = new CircularBuffer2<int>(3);
        buffer.PushBack(1);
        buffer.PushBack(2);
        buffer.PushBack(3);

        buffer.PopBack();

        Assert.Equal(2, buffer.Size);
        Assert.Equal(1, buffer.Front());
        Assert.Equal(2, buffer.Back());
    }

    [Fact]
    public void PopFront_ShouldRemoveElementFromFront()
    {
        var buffer = new CircularBuffer2<int>(3);
        buffer.PushBack(1);
        buffer.PushBack(2);
        buffer.PushBack(3);

        buffer.PopFront();

        Assert.Equal(2, buffer.Size);
        Assert.Equal(2, buffer.Front());
        Assert.Equal(3, buffer.Back());
    }

    [Fact]
    public void Clear_ShouldEmptyTheBuffer()
    {
        var buffer = new CircularBuffer2<int>(3);
        buffer.PushBack(1);
        buffer.PushBack(2);
        buffer.PushBack(3);

        buffer.Clear();

        Assert.True(buffer.IsEmpty);
        Assert.Equal(0, buffer.Size);
    }

    [Fact]
    public void ToArray_ShouldReturnCorrectArray()
    {
        var buffer = new CircularBuffer2<int>(3);
        buffer.PushBack(1);
        buffer.PushBack(2);
        buffer.PushBack(3);

        var array = buffer.ToArray();

        Assert.Equal(new[] { 1, 2, 3 }, array);
    }

    [Fact]
    public void Indexer_ShouldReturnCorrectElement()
    {
        var buffer = new CircularBuffer2<int>(3);
        buffer.PushBack(1);
        buffer.PushBack(2);
        buffer.PushBack(3);

        Assert.Equal(1, buffer[0]);
        Assert.Equal(2, buffer[1]);
        Assert.Equal(3, buffer[2]);
    }

    [Fact]
    public void Indexer_ShouldThrowOnInvalidIndex()
    {
        var buffer = new CircularBuffer2<int>(3);

        Assert.Throws<IndexOutOfRangeException>(() => buffer[0]);
        buffer.PushBack(1);
        Assert.Throws<IndexOutOfRangeException>(() => buffer[1]);
    }

    [Fact]
    public void Enumerator_ShouldEnumerateCorrectly()
    {
        var buffer = new CircularBuffer2<int>(3);
        buffer.PushBack(1);
        buffer.PushBack(2);
        buffer.PushBack(3);

        var items = new List<int>();
        foreach (var item in buffer)
        {
            items.Add(item);
        }

        Assert.Equal(new[] { 1, 2, 3 }, items);
        
    }
    
    [Fact]
    public void CopyTo_ShouldCopyAllElements_WhenBufferIsNotWrapped()
    {
        var buffer = new CircularBuffer2<int>(5);
        buffer.PushBack(1);
        buffer.PushBack(2);
        buffer.PushBack(3);

        Span<int> destination = new int[3];
        buffer.CopyTo(destination);

        Assert.Equal(new[] { 1, 2, 3 }, destination.ToArray());
    }

    [Fact]
    public void CopyTo_ShouldCopyAllElements_WhenBufferIsWrapped()
    {
        var buffer = new CircularBuffer2<int>(5);
        buffer.PushBack(1);
        buffer.PushBack(2);
        buffer.PushBack(3);
        buffer.PushBack(4);
        buffer.PushBack(5);

        buffer.PopFront(); // Remove 1
        buffer.PopFront(); // Remove 2

        buffer.PushBack(6);
        buffer.PushBack(7);

        Span<int> destination = new int[5];
        buffer.CopyTo(destination);

        Assert.Equal(new[] { 3, 4, 5, 6, 7 }, destination.ToArray());
    }

    [Fact]
    public void CopyTo_ShouldNotCopyIfSpanIsSmallerThanBuffer()
    {
        var buffer = new CircularBuffer2<int>(5);
        buffer.PushBack(1);
        buffer.PushBack(2);
        buffer.PushBack(3);

        Span<int> destination = new int[2]; // Smaller than buffer size
        buffer.CopyTo(destination);

        // Check that the destination was not fully overwritten
        Assert.Equal(new[] { 1, 2 }, destination.ToArray());
    }
    
    [Fact]
    public void CopyTo_ShouldCopyOverflowedData()
    {
        var buffer = new CircularBuffer2<int>(5);
        buffer.PushBack(1);
        buffer.PushBack(2);
        buffer.PushBack(3);
        buffer.PushBack(4);
        buffer.PushBack(5);
        buffer.PushBack(6);

        Span<int> destination = new int[2]; // Smaller than buffer size
        buffer.CopyTo(destination);

        // Check that the destination was not fully overwritten
        Assert.Equal(new[] { 2, 3 }, destination.ToArray());
    }

    [Fact]
    public void CopyTo_ShouldHandleEmptyBuffer()
    {
        var buffer = new CircularBuffer2<int>(5);

        Span<int> destination = new int[5];
        buffer.CopyTo(destination);

        Assert.Equal(new int[5], destination.ToArray());
    }

    [Fact]
    public void CopyTo_ShouldHandleFullBuffer()
    {
        var buffer = new CircularBuffer2<int>(5);
        buffer.PushBack(1);
        buffer.PushBack(2);
        buffer.PushBack(3);
        buffer.PushBack(4);
        buffer.PushBack(5);

        Span<int> destination = new int[5];
        buffer.CopyTo(destination);

        Assert.Equal(new[] { 1, 2, 3, 4, 5 }, destination.ToArray());
    }

    [Fact]
    public void CopyTo_ShouldHandleBufferWithCapacityGreaterThanElements()
    {
        var buffer = new CircularBuffer2<int>(5);
        buffer.PushBack(1);
        buffer.PushBack(2);

        Span<int> destination = new int[5];
        buffer.CopyTo(destination);

        Assert.Equal(new[] { 1, 2, 0, 0, 0 }, destination.ToArray());
    }
}
