using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using Xunit;

namespace Asv.Common.Test;

public class DepthFirstSearchTest
{
    [Fact]
    public void Depth_First_Search_Sort_Sorting()
    {
        var edges = new ReadOnlyDictionary<int, int[]>(
            new Dictionary<int, int[]>()
            {
                { 10, [5, 23, 54, 88] },
                { 5, [23, 54] },
                { 23, [] },
                { 54, [88] },
                { 88, [] },
            }
        );

        Assert.Equal([23, 88, 54, 5, 10], DepthFirstSearch.Sort(edges));
    }

    [Fact]
    public void Depth_First_Search_Sort_Circular_Dependency_Argument_Exception()
    {
        var edges = new ReadOnlyDictionary<int, int[]>(
            new Dictionary<int, int[]>()
            {
                { 12, [9] },
                { 1, [12] },
                { 9, [8] },
                { 8, [7, 1] },
                { 7, [10, 6, 3] },
                { 10, [11] },
                { 11, [7] },
                { 6, [5] },
                { 5, [3] },
                { 3, [2, 4] },
                { 2, [] },
                { 4, [] },
            }
        );

        Assert.Throws<ArgumentException>(() =>
        {
            var newEdges = DepthFirstSearch.Sort(edges);
            foreach (var edge in newEdges)
            {
                Debug.WriteLine($"{edge}");
            }
        });
    }

    [Fact]
    public void Depth_First_Search_Sort_Member_Not_Defined_Argument_Exception()
    {
        var edges = new ReadOnlyDictionary<int, int[]>(
            new Dictionary<int, int[]>()
            {
                { 12, [9] },
                { 1, [12] },
                { 8, [7, 1] },
                { 7, [10, 6, 3] },
            }
        );

        Assert.Throws<ArgumentException>(() =>
        {
            var newEdges = DepthFirstSearch.Sort(edges);
            foreach (var edge in newEdges)
            {
                Debug.WriteLine($"{edge}");
            }
        });
    }

    [Fact]
    public void Depth_First_Search_Sort_Argument_Null_Exception()
    {
        var edges = new ReadOnlyDictionary<int, int[]>(
            new Dictionary<int, int[]>()
            {
                { 12, null },
                { 1, [12] },
                { 8, [7, 1] },
                { 7, [10, 6, 3] },
            }
        );

        Assert.Throws<ArgumentNullException>(() =>
        {
            var newEdges = DepthFirstSearch.Sort(edges);
            foreach (var edge in newEdges)
            {
                Debug.WriteLine($"{edge}");
            }
        });
    }

    [Fact]
    public void Depth_First_Search_Sort_Random_Objects()
    {
        var dateTime = new DateTime(1234, 6, 12, 3, 4, 5);

        var someEnum = ParameterDirection.Output;

        var edges = new ReadOnlyDictionary<object, object[]>(
            new Dictionary<object, object[]>()
            {
                { 12, [1.2] },
                { 1.2, ["8RWE"] },
                { "8RWE", [dateTime, someEnum] },
                { dateTime, [] },
                { someEnum, [] },
            }
        );

        Assert.Equal([dateTime, someEnum, "8RWE", 1.2, 12], DepthFirstSearch.Sort(edges));
    }
}
