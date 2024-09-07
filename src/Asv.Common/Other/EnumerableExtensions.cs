using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Asv.Common
{
    /// <summary>
    /// Extension methods for all kinds of (typed) enumerable data (Array, List, ...)
    /// </summary>
    public static class EnumerableExtensions
    {
        
        /// <summary>
        /// Split the elements of a sequence into chunks of size at most <paramref name="size"/>.
        /// </summary>
        /// <remarks>
        /// Every chunk except the last will be of size <paramref name="size"/>.
        /// The last chunk will contain the remaining elements and may be of a smaller size.
        /// </remarks>
        /// <param name="source">
        /// An <see cref="IEnumerable{T}"/> whose elements to chunk.
        /// </param>
        /// <param name="size">
        /// Maximum size of each chunk.
        /// </param>
        /// <typeparam name="TSource">
        /// The type of the elements of source.
        /// </typeparam>
        /// <returns>
        /// An <see cref="IEnumerable{T}"/> that contains the elements the input sequence split into chunks of size <paramref name="size"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="size"/> is below 1.
        /// </exception>
        public static IEnumerable<TSource[]> Chunked<TSource>(this IEnumerable<TSource> source, int size)
        {
            return ChunkIterator(source, size);
        }

        private static IEnumerable<TSource[]> ChunkIterator<TSource>(IEnumerable<TSource> source, int size)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
            using var e = source.GetEnumerator();
            while (e.MoveNext())
            {
                TSource[] chunk = new TSource[size];
                chunk[0] = e.Current;

                var i = 1;
                for (; i < chunk.Length && e.MoveNext(); i++)
                {
                    chunk[i] = e.Current;
                }

                if (i == chunk.Length)
                {
                    yield return chunk;
                }
                else
                {
                    Array.Resize(ref chunk, i);
                    yield return chunk;
                    yield break;
                }
            }
        }


        /// <summary>
        /// Synchronizes a dictionary with a given list of keys.
        /// Adds keys to the dictionary that are not present and removes keys from the dictionary that are not in the list.
        /// </summary>
        /// <typeparam name="TKey">The type of the dictionary keys.</typeparam>
        /// <typeparam name="TValue">The type of the dictionary values.</typeparam>
        /// <param name="dict">The dictionary to synchronize.</param>
        /// <param name="keys">The list of keys to synchronize with.</param>
        /// <param name="addItem">A function that adds missing keys to the dictionary and returns the corresponding value.</param>
        /// <param name="afterRemoveItem">An action that is executed after removing a key from the dictionary.</param>
        /// <returns>True if any changes were made to the dictionary; otherwise, false.</returns>
        public static bool SyncDictionaryWithKeys<TKey, TValue>(this IDictionary<TKey, TValue> dict, IEnumerable<TKey> keys,
            Func<TKey, TValue> addItem, Action<TKey>? afterRemoveItem)
            where TKey : notnull
        {
            var updated = false;

            var list = keys.ToImmutableArray();
            foreach (var key in list.Where(key => !dict.ContainsKey(key)))
            {
                dict.Add(key, addItem(key));
                updated = true;
            }
            //remove not found keys
            foreach (var key in dict.Keys.ToImmutableArray().Where(key => !list.Contains(key)))
            {
                dict.Remove(key);
                afterRemoveItem?.Invoke(key);
                updated = true;
            }

            return updated;
        }
        public static void SyncCollection<T>(this IEnumerable<T> exist, IEnumerable<T> actual, Action<T> deleteCallback, Action<T> addCallback,IEqualityComparer<T> comparer) 
        {
            var toDelete = exist.Except(actual,comparer).ToArray();
            var toAdd = actual.Except(exist,comparer).ToArray();
            foreach (var item in toDelete)
            {
                deleteCallback(item);
            }

            foreach (var item in toAdd)
            {
                addCallback(item);
            }
        }

        

        /// <summary>
        /// Performs an action for each item in the enumerable
        /// </summary>
        /// <typeparam name="T">The enumerable data type</typeparam>
        /// <param name="values">The data values.</param>
        /// <param name="action">The action to be performed.</param>
        /// <example>
        /// 
        /// var values = new[] { "1", "2", "3" };
        /// values.ConvertList&lt;string, int&gt;().ForEach(Console.WriteLine);
        /// 
        /// </example>
        /// <remarks>This method was intended to return the passed values to provide method chaining. Howver due to defered execution the compiler would actually never run the entire code at all.</remarks>
        public static void ForEach<T>(this IEnumerable<T> values, Action<T> action)
        {
            foreach (var value in values)
            {
                action(value);
            }
        }

        /// <summary>
        /// Returns enumerable object based on target, which does not contains null references.
        /// If target is null reference, returns empty enumerable object.
        /// </summary>
        /// <typeparam name="T">Type of items in target.</typeparam>
        /// <param name="target">Target enumerable object. Can be null.</param>
        /// <example>
        /// object[] items = null;
        /// foreach(var item in items.NotNull()){
        ///     // result of items.NotNull() is empty but not null enumerable
        /// }
        /// 
        /// object[] items = new object[]{ null, "Hello World!", null, "Good bye!" };
        /// foreach(var item in items.NotNull()){
        ///		// result of items.NotNull() is enumerable with two strings
        /// }
        /// </example>
        /// <remarks>Contributed by tencokacistromy, http://www.codeplex.com/site/users/view/tencokacistromy </remarks>
        public static IEnumerable<T> IgnoreNulls<T>(this IEnumerable<T> target)
        {
            if (ReferenceEquals(target, null))
            {
                yield break;
            }

            foreach (var item in target)
            {
                if (ReferenceEquals(item, null)) continue;
                yield return item;
            }
        }

        /// <summary>
        /// Returns the maximum item based on a provided selector.
        /// </summary>
        /// <typeparam name="TItem">The item type</typeparam>
        /// <typeparam name="TValue">The value item</typeparam>
        /// <param name="items">The items.</param>
        /// <param name="selector">The selector.</param>
        /// <param name="maxValue">The max value as output parameter.</param>
        /// <returns>The maximum item</returns>
        /// <example><code>
        /// int age;
        /// var oldestPerson = persons.MaxItem(p =&gt; p.Age, out age);
        /// </code>
        /// </example>
        public static TItem MaxItem<TItem, TValue>(this IEnumerable<TItem> items, Func<TItem, TValue> selector, out TValue maxValue)
            where TItem : class
            where TValue : IComparable
        {
            TItem maxItem = null;
            maxValue = default(TValue);

            foreach (var item in items)
            {
                if (item != null)
                {
                    var itemValue = selector(item);

                    if ((maxItem == null) || (itemValue.CompareTo(maxValue) > 0))
                    {
                        maxValue = itemValue;
                        maxItem = item;
                    }
                }
            }

            return maxItem;
        }

        /// <summary>
        /// Returns the maximum item based on a provided selector.
        /// </summary>
        /// <typeparam name="TItem">The item type</typeparam>
        /// <typeparam name="TValue">The value item</typeparam>
        /// <param name="items">The items.</param>
        /// <param name="selector">The selector.</param>
        /// <returns>The maximum item</returns>
        /// <example><code>
        /// var oldestPerson = persons.MaxItem(p =&gt; p.Age);
        /// </code>
        /// </example>
        public static TItem MaxItem<TItem, TValue>(this IEnumerable<TItem> items, Func<TItem, TValue> selector)
            where TItem : class
            where TValue : IComparable
        {
            TValue maxValue;

            return items.MaxItem(selector, out maxValue);
        }

        /// <summary>
        /// Returns the minimum item based on a provided selector.
        /// </summary>
        /// <typeparam name="TItem">The item type</typeparam>
        /// <typeparam name="TValue">The value item</typeparam>
        /// <param name="items">The items.</param>
        /// <param name="selector">The selector.</param>
        /// <param name="minValue">The min value as output parameter.</param>
        /// <returns>The minimum item</returns>
        /// <example><code>
        /// int age;
        /// var youngestPerson = persons.MinItem(p =&gt; p.Age, out age);
        /// </code>
        /// </example>
        public static TItem MinItem<TItem, TValue>(this IEnumerable<TItem> items, Func<TItem, TValue> selector, out TValue minValue)
            where TItem : class
            where TValue : IComparable
        {
            TItem minItem = null;
            minValue = default(TValue);

            foreach (var item in items)
            {
                if (item != null)
                {
                    var itemValue = selector(item);

                    if ((minItem == null) || (itemValue.CompareTo(minValue) < 0))
                    {
                        minValue = itemValue;
                        minItem = item;
                    }
                }
            }

            return minItem;
        }

        /// <summary>
        /// Returns the minimum item based on a provided selector.
        /// </summary>
        /// <typeparam name="TItem">The item type</typeparam>
        /// <typeparam name="TValue">The value item</typeparam>
        /// <param name="items">The items.</param>
        /// <param name="selector">The selector.</param>
        /// <returns>The minimum item</returns>
        /// <example><code>
        /// var youngestPerson = persons.MinItem(p =&gt; p.Age);
        /// </code>
        /// </example>
        public static TItem MinItem<TItem, TValue>(this IEnumerable<TItem> items, Func<TItem, TValue> selector)
            where TItem : class
            where TValue : IComparable
        {
            TValue minValue;

            return items.MinItem(selector, out minValue);
        }


        
    }

    
}
