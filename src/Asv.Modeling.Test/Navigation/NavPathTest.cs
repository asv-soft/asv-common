using JetBrains.Annotations;

namespace Asv.Modeling.Test;

[TestSubject(typeof(NavPath))]
public class NavPathTest
{
    [Fact]
    public void ToString_Empty_ReturnsEmptyString()
    {
        var path = default(NavPath);

        Assert.Equal(string.Empty, path.ToString());
        Assert.True(path.IsEmpty);
        Assert.Equal(0, path.Count);
    }

    [Fact]
    public void ToString_JoinsNavIdsWithSlash()
    {
        var path = new NavPath(
            new NavId("root"),
            new NavId(
                "file.item",
                new NavArgs(new KeyValuePair<string, string?>("path", @"C:\Temp\File.txt"))
            )
        );

        var text = path.ToString();

        Assert.StartsWith("root/file.item?", text);
        Assert.Contains("/", text);
        Assert.Contains("path=", text);
    }

    [Fact]
    public void Parse_RoundTripsSegments()
    {
        var source = new NavPath(
            new NavId("root"),
            new NavId(
                "folder",
                new NavArgs(new KeyValuePair<string, string?>("name", "read me"))
            ),
            new NavId(
                "file.item",
                new NavArgs(new KeyValuePair<string, string?>("path", @"C:\Temp\File.txt"))
            )
        );

        var parsed = NavPath.Parse(source.ToString());

        Assert.Equal(source, parsed);
        Assert.Equal(3, parsed.Count);
    }

    [Fact]
    public void Parse_Empty_ReturnsDefault()
    {
        var parsed = NavPath.Parse(string.Empty);

        Assert.True(parsed.IsEmpty);
        Assert.Equal(0, parsed.Count);
    }

    [Fact]
    public void Equality_DependsOnSegmentOrder()
    {
        var left = new NavPath(new NavId("root"), new NavId("child"));
        var right = new NavPath(new NavId("root"), new NavId("child"));
        var other = new NavPath(new NavId("child"), new NavId("root"));

        Assert.Equal(left, right);
        Assert.NotEqual(left, other);
    }

    [Fact]
    public void Enumerator_ReturnsAllSegments()
    {
        var path = new NavPath(new NavId("root"), new NavId("child"));

        var items = path.ToArray();

        Assert.Equal(2, items.Length);
        Assert.Equal(new NavId("root"), items[0]);
        Assert.Equal(new NavId("child"), items[1]);
    }
}
