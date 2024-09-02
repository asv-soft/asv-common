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
        var edges = new ReadOnlyDictionary<int, int[]>(new Dictionary<int, int[]>()
        {
            {10, new [] { 5, 23, 54, 88 }},
            {5, new [] { 23, 54 }},
            {23, new int[] { }},
            {54, new [] { 88 }},
            {88, new int[] { }},
        });
        
        Assert.Equal(new [] {23, 88, 54, 5, 10}, DepthFirstSearch.Sort(edges));
    }
    
    [Fact]
    public void Depth_First_Search_Sort_Circular_Dependency_Argument_Exception()
    {
        var edges = new ReadOnlyDictionary<int, int[]>(new Dictionary<int, int[]>()
        {
            {12, new int[]{9}},
            {1, new int[]{12}},
            {9, new int[]{8}},
            {8, new int[]{7, 1}},
            {7, new int[]{10, 6, 3}},
            {10, new int[]{11}},
            {11, new int[]{7}},
            {6, new int[]{5}},
            {5, new int[]{3}},
            {3, new int[]{2, 4}},
            {2, new int[]{}},
            {4, new int[]{}},
        });
        
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
        var edges = new ReadOnlyDictionary<int, int[]>(new Dictionary<int, int[]>()
        {
            {12, new int[]{9}},
            {1, new int[]{12}},
            {8, new int[]{7, 1}},
            {7, new int[]{10, 6, 3}},
        });
        
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
        var edges = new ReadOnlyDictionary<int, int[]>(new Dictionary<int, int[]>()
        {
            {12, null},
            {1, new int[]{12}},
            {8, new int[]{7, 1}},
            {7, new int[]{10, 6, 3}},
        });
        
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
        
        var edges = new ReadOnlyDictionary<object, object[]>(new Dictionary<object, object[]>()
        {
            {12, new object[] {1.2}},
            {1.2, new object[] {"8RWE"}},
            {"8RWE", new object[] {dateTime, someEnum}},
            {dateTime, new object[] {}},
            {someEnum, new object[] {}},
        });

        Assert.Equal(new object[] {dateTime, someEnum, "8RWE", 1.2, 12}, 
            DepthFirstSearch.Sort(edges));
    }
}