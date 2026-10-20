using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xunit;

namespace Asv.IO.Test;

public class ProtocolTagsTest
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void ClearAndRemove_ReleaseRemovedValues(bool overflow, bool remove)
    {
        var (owner, reference) = CreateRemovedValue(overflow, remove);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.False(reference.IsAlive);
        Assert.Equal(remove && overflow ? 8 : 0, owner.Tags.Count);
        GC.KeepAlive(owner);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OverflowTransition_DoesNotRetainReplacedInlineValues(bool insert)
    {
        var (owner, reference) = CreateReplacedOverflowValue(insert);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        Assert.False(reference.IsAlive);
        Assert.Equal(9, owner.Tags.Count);
        Assert.Equal("replacement", owner.Tags["payload"]);
        GC.KeepAlive(owner);
    }

    [Fact]
    public void ClearAndReuse_PreservesOrderingAndEnumeratorContract()
    {
        var tags = default(ProtocolTags);
        for (var i = 0; i < 9; i++)
        {
            tags.Add(i.ToString(), i);
        }
        tags.Clear();
        tags.Add("first", 1);
        tags.Add("second", 2);
        tags.RemoveAt(0);
        using IEnumerator<KeyValuePair<string, object?>> enumerator = tags.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        Assert.Equal("second", enumerator.Current.Key);
        Assert.Equal(2, enumerator.Current.Value);
        Assert.False(enumerator.MoveNext());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (Owner Owner, WeakReference Reference) CreateRemovedValue(
        bool overflow,
        bool remove
    )
    {
        var owner = new Owner();
        if (overflow)
        {
            for (var i = 0; i < 8; i++)
            {
                owner.Tags.Add(i.ToString(), i);
            }
        }
        var value = new object();
        var reference = new WeakReference(value);
        owner.Tags.Add("payload", value);
        if (remove)
        {
            owner.Tags.RemoveAt(owner.Tags.Count - 1);
        }
        else
        {
            owner.Tags.Clear();
        }
        return (owner, reference);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (Owner Owner, WeakReference Reference) CreateReplacedOverflowValue(bool insert)
    {
        var owner = new Owner();
        var value = new object();
        var reference = new WeakReference(value);
        owner.Tags.Add("payload", value);
        for (var i = 1; i < 8; i++)
        {
            owner.Tags.Add(i.ToString(), i);
        }
        if (insert)
        {
            owner.Tags.Insert(0, new KeyValuePair<string, object?>("extra", 9));
        }
        else
        {
            owner.Tags.Add("extra", 9);
        }
        owner.Tags["payload"] = "replacement";
        return (owner, reference);
    }

    private sealed class Owner
    {
        public ProtocolTags Tags;
    }
}
