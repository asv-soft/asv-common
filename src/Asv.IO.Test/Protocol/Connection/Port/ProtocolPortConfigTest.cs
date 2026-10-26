using System;
using System.Web;
using Xunit;

namespace Asv.IO.Test.Connection.Port;

public class ProtocolPortConfigTest
{
    [Theory]
    [InlineData("tcp://127.0.0.1:7341/?enabled=True#name=GBS", "GBS")]
    [InlineData("tcp://127.0.0.1:7341/?name=Current#name=Legacy", "Current")]
    [InlineData("tcp://127.0.0.1:7341/?name=#name=Legacy", "")]
    [InlineData("tcp://127.0.0.1:7341/", null)]
    public void Name_ReadsLegacyFragmentWithQueryPriority(string uri, string? expected)
    {
        var config = new ProtocolPortConfig(new Uri(uri));
        Assert.Equal(expected, config.Name);
    }

    [Theory]
    [InlineData("tcp://127.0.0.1:7341/?enabled=False#name=GBS&protocols=mavlink", "GBS")]
    [InlineData("tcp://127.0.0.1:7341/?enabled=False&name=Current#name=Old&protocols=mavlink", "Current")]
    [InlineData("tcp://127.0.0.1:7341/?enabled=False#name=%d0%91%d0%b0%d0%b7%d0%b0+%26+GNSS%2b&protocols=mavlink", "База & GNSS+")]
    public void AsUri_MigratesNameAndPreservesOtherParameters(string uri, string expected)
    {
        var config = new ProtocolPortConfig(new Uri(uri));
        var saved = config.AsUri();
        var query = HttpUtility.ParseQueryString(saved.Query);
        var fragment = HttpUtility.ParseQueryString(saved.Fragment.TrimStart('#'));
        Assert.Equal(expected, query[ProtocolPortConfig.NameQueryKey]);
        Assert.Null(fragment[ProtocolPortConfig.NameQueryKey]);
        Assert.Equal("False", query[ProtocolPortConfig.EnabledKey]);
        Assert.Equal("mavlink", fragment[ProtocolPortConfig.ProtocolQueryKey]);
        Assert.Equal(expected, new ProtocolPortConfig(saved).Name);
        Assert.Equal(saved, new ProtocolPortConfig(saved).AsUri());
    }

    [Theory]
    [InlineData("New name")]
    [InlineData("")]
    [InlineData(null)]
    public void Name_SetterReplacesOrClearsLegacyName(string? name)
    {
        var config = new ProtocolPortConfig(new Uri("tcp://127.0.0.1:7341/#name=Old&protocols=mavlink"));
        config.Name = name;
        Assert.Equal(name, config.Name);
        Assert.Null(config.Fragment[ProtocolPortConfig.NameQueryKey]);
        Assert.Equal(name, new ProtocolPortConfig(config.AsUri()).Name);
    }

    [Theory]
    [InlineData("tcp://127.0.0.1:7341/#name=GBS")]
    [InlineData("tcps://127.0.0.1:8888/#name=GBS")]
    [InlineData("udp://127.0.0.1:14550/#name=GBS")]
    [InlineData("serial://COM3/?br=115200#name=GBS")]
    public void TypedConfigClone_PreservesTypeAndMigratesName(string uri)
    {
        var source = new Uri(uri);
        ProtocolPortConfig config = source.Scheme switch
        {
            "tcp" => new TcpClientProtocolPortConfig(source),
            "tcps" => new TcpServerProtocolPortConfig(source),
            "udp" => new UdpProtocolPortConfig(source),
            _ => new SerialProtocolPortConfig(source),
        };
        var clone = Assert.IsAssignableFrom<ProtocolPortConfig>(config.Clone());
        Assert.Equal(config.GetType(), clone.GetType());
        Assert.Equal("GBS", clone.Name);
        Assert.Equal("GBS", clone.Query[ProtocolPortConfig.NameQueryKey]);
        Assert.Null(clone.Fragment[ProtocolPortConfig.NameQueryKey]);
    }

    [Fact]
    public void Clone_MigratesNameWithoutMutatingOriginalCollections()
    {
        var config = new ProtocolPortConfig(new Uri("tcp://127.0.0.1:7341/#name=GBS&protocols=mavlink"));
        var clone = Assert.IsType<ProtocolPortConfig>(config.Clone());
        Assert.Equal("GBS", clone.Name);
        Assert.Equal("GBS", clone.Query[ProtocolPortConfig.NameQueryKey]);
        Assert.Null(clone.Fragment[ProtocolPortConfig.NameQueryKey]);
        Assert.Null(config.Query[ProtocolPortConfig.NameQueryKey]);
        Assert.Equal("GBS", config.Fragment[ProtocolPortConfig.NameQueryKey]);
        Assert.Equal("mavlink", clone.Fragment[ProtocolPortConfig.ProtocolQueryKey]);
    }
}
