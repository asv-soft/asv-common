using JetBrains.Annotations;

namespace Asv.Modeling.Test;

[TestSubject(typeof(NavArgs))]
public class NavArgsTest
{
    [Fact]
    public void ToString_UrlEncodesValue()
    {
        var args = new NavArgs(new KeyValuePair<string, string?>("path", @"C:\Temp\My File.txt"));

        var result = args.ToString();

        Assert.Equal("path=C%3a%5cTemp%5cMy+File.txt", result, ignoreCase: true);
    }

    [Fact]
    public void Parse_DecodesValue()
    {
        var args = NavArgs.Parse("path=C%3A%5CTemp%5CMy+File.txt");

        Assert.False(args.IsEmpty);
        Assert.Equal("path", args[0].Key);
        Assert.Equal(@"C:\Temp\My File.txt", args[0].Value);
    }

    [Fact]
    public void ExplicitOperator_ConvertsStringToNavArgs()
    {
        var args = (NavArgs)"path=C%3A%5CTemp%5CMy+File.txt&name=read+me";

        Assert.Equal(@"C:\Temp\My File.txt", args["path"]);
        Assert.Equal("read me", args["name"]);
    }

    [Fact]
    public void Constructor_Throws_WhenKeyDoesNotMatchRegex()
    {
        Assert.Throws<ArgumentException>(() =>
            new NavArgs(new KeyValuePair<string, string?>("bad.key", "value"))
        );
    }

    [Fact]
    public void TupleConstructor_CreatesArgs()
    {
        var args = new NavArgs(("path", @"C:\Temp\File.txt"), ("name", "read me"));

        Assert.Equal(2, args.Count);
        Assert.Equal(@"C:\Temp\File.txt", args["path"]);
        Assert.Equal("read me", args["name"]);
    }

    [Fact]
    public void TupleConstructor_ReturnsEmpty_WhenNoArgs()
    {
        var args = new NavArgs();

        Assert.True(args.IsEmpty);
    }

    [Fact]
    public void TupleConstructor_Throws_WhenKeyDoesNotMatchRegex()
    {
        Assert.Throws<ArgumentException>(() =>
            new NavArgs(("bad.key", "value"))
        );
    }

    [Fact]
    public void Parse_RoundTripsCanonicalForm()
    {
        var source = new NavArgs(
            new KeyValuePair<string, string?>("path", @"C:\Temp\File.txt"),
            new KeyValuePair<string, string?>("name", "read me")
        );

        var parsed = NavArgs.Parse(source.ToString());

        Assert.Equal(source, parsed);
    }

    [Fact]
    public void Constructor_AllowsUnderscoreAndHyphenInKey()
    {
        var args = new NavArgs(
            new KeyValuePair<string, string?>("file_path-id", @"C:\Temp\File.txt")
        );

        Assert.Equal("file_path-id", args[0].Key);
    }

    [Fact]
    public void StringIndexer_ReturnsValueByKey()
    {
        var args = new NavArgs(
            new KeyValuePair<string, string?>("path", @"C:\Temp\File.txt"),
            new KeyValuePair<string, string?>("name", "read me")
        );

        Assert.Equal(@"C:\Temp\File.txt", args["path"]);
        Assert.Equal("read me", args["name"]);
    }

    [Fact]
    public void StringIndexer_ReturnsNull_WhenKeyNotFound()
    {
        var args = new NavArgs(new KeyValuePair<string, string?>("path", @"C:\Temp\File.txt"));

        Assert.Null(args["missing"]);
    }

    [Fact]
    public void Empty_ReturnsCanonicalEmptyValue()
    {
        Assert.True(NavArgs.Empty.IsEmpty);
        Assert.Equal(default, NavArgs.Empty);
    }
}
