using System;
using System.Collections.Generic;

namespace Asv.Common;

public static class ListMixin
{
    /// <summary>
    /// Resizes the list to the specified size. Adds default instances if the list is smaller, or removes items if larger.
    /// Requires a parameterless constructor.
    /// </summary>
    public static void Resize<T>(this IList<T> list, int size)
        where T : new()
    {
        if (size < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Size cannot be negative.");
        }

        while (list.Count > size)
        {
            list.RemoveAt(list.Count - 1);
        }
        while (list.Count < size)
        {
            list.Add(new T());
        }
    }

    /// <summary>
    /// Resizes the list to the specified size, adding items created by the provided factory method.
    /// </summary>
    public static void Resize<T>(this IList<T> list, int size, Func<T> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);

        if (size < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Size cannot be negative.");
        }

        while (list.Count > size)
        {
            list.RemoveAt(list.Count - 1);
        }
        while (list.Count < size)
        {
            list.Add(factory());
        }
    }

    /// <summary>
    /// Resizes the list to the specified size, using the provided default value.
    /// </summary>
    public static void Resize<T>(this IList<T> list, int size, T defaultValue)
    {
        if (size < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(size), "Size cannot be negative.");
        }

        while (list.Count > size)
        {
            list.RemoveAt(list.Count - 1);
        }
        while (list.Count < size)
        {
            list.Add(defaultValue);
        }
    }
}
