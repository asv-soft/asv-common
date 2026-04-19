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
    public void Constructor_Throws_WhenKeyDoesNotMatchRegex()
    {
        Assert.Throws<ArgumentException>(() =>
            new NavArgs(new KeyValuePair<string, string?>("bad.key", "value"))
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
}
