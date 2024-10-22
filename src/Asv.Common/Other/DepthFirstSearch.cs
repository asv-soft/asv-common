using System;
using System.Collections.Generic;
using System.Linq;

namespace Asv.Common
{
    /// <summary>
    /// https://en.wikipedia.org/wiki/Depth-first_search
    /// </summary>
    public class DepthFirstSearch
    {
        public static IEnumerable<T> Sort<T>(IReadOnlyDictionary<T,T[]> edges)
        {
            var gray = new HashSet<T>();
            var black = new HashSet<T>();
            return edges.SelectMany(_=>InternalCalc(_.Key, edges, gray, black));
        }

        private static IEnumerable<T> InternalCalc<T>(T key, IReadOnlyDictionary<T, T[]> edges, ISet<T> gray, ISet<T> black)
        {
            if (!edges.ContainsKey(key)) throw new ArgumentException($"Member '{key}' not defined");

            if (gray.Contains(key)) throw new ArgumentException($"Circular dependency from {key}");
            if (black.Contains(key)) yield break;
            gray.Add(key);
            
            var subitems = edges[key];
            
            foreach (var subitem in subitems.SelectMany(_ => InternalCalc(_, edges, gray, black)))
            {
                yield return subitem;
            }

            yield return key;

            gray.Remove(key);
            black.Add(key);
        }

    }

   
}