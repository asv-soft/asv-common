using JetBrains.Annotations;

namespace Asv.Modeling.Test;

[TestSubject(typeof(NavId))]
public class NavIdTest
{
    [Fact]
    public void Constructor_Throws_WhenTypeIdIsInvalid()
    {
        Assert.Throws<ArgumentException>(() => new NavId("bad/id"));
        Assert.Throws<ArgumentException>(() => new NavId(""));
    }

    [Fact]
    public void ToString_WithoutArgs_ReturnsTypeId()
    {
        var id = new NavId("file.item");

        Assert.Equal("file.item", id.ToString());
    }

    [Fact]
    public void ToString_WithArgs_ReturnsCanonicalString()
    {
        var id = new NavId(
            "file.item",
            new NavArgs(new KeyValuePair<string, string?>("path", @"C:\Temp\My File.txt"))
        );

        var text = id.ToString();

        Assert.StartsWith("file.item?", text);
        Assert.Contains("path=", text);
        Assert.Contains("%", text);
    }

    [Fact]
    public void Constructor_FromString_ParsesArgs()
    {
        var id = new NavId("file.item?path=C%3A%5CTemp%5CMy+File.txt");

        Assert.Equal("file.item", id.TypeId);
        Assert.False(id.Args.IsEmpty);
        Assert.Equal("path", id.Args[0].Key);
        Assert.Equal(@"C:\Temp\My File.txt", id.Args[0].Value);
    }

    [Fact]
    public void Equality_TypeIdIsCaseInsensitive_ArgsAreCaseSensitive()
    {
        var left = new NavId(
            "FILE.ITEM",
            new NavArgs(new KeyValuePair<string, string?>("path", @"C:\Temp\File.txt"))
        );
        var right = new NavId(
            "file.item",
            new NavArgs(new KeyValuePair<string, string?>("path", @"C:\Temp\File.txt"))
        );
        var other = new NavId(
            "file.item",
            new NavArgs(new KeyValuePair<string, string?>("path", @"C:\Temp\FILE.txt"))
        );

        Assert.Equal(left, right);
        Assert.NotEqual(left, other);
    }

    [Fact]
    public void RoundTrip_ToString_Parse_PreservesValue()
    {
        var source = new NavId(
            "file.item",
            new NavArgs(new KeyValuePair<string, string?>("path", @"C:\Temp\File.txt"))
        );

        var parsed = new NavId(source.ToString());

        Assert.Equal(source, parsed);
    }

    [Fact]
    public void GenerateByHash_IsDeterministic()
    {
        var left = NavId.GenerateByHash("file", 42, @"C:\Temp\File.txt");
        var right = NavId.GenerateByHash("file", 42, @"C:\Temp\File.txt");

        Assert.Equal(left, right);
        Assert.Matches("^[a-zA-Z0-9\\._\\-]+$", left.TypeId);
    }

    [Fact]
    public void GenerateByHash_DependsOnValueOrder()
    {
        var left = NavId.GenerateByHash("file", 42);
        var right = NavId.GenerateByHash(42, "file");

        Assert.NotEqual(left, right);
    }

    [Fact]
    public void GenerateByHash_DistinguishesNullAndEmptyString()
    {
        var withNull = NavId.GenerateByHash<string?>(null);
        var withEmpty = NavId.GenerateByHash(string.Empty);

        Assert.NotEqual(withNull, withEmpty);
    }

    [Fact]
    public void Empty_ReturnsCanonicalEmptyValue()
    {
        Assert.True(NavId.Empty.Args.IsEmpty);
        Assert.Equal(default, NavId.Empty);
        Assert.Null(NavId.Empty.TypeId);
    }
}
