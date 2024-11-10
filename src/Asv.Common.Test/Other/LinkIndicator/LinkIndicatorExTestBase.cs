using System;
using Xunit;
using R3;

namespace Asv.Common.Test;

/// <summary>
/// Base class for testing implementations of ILinkIndicatorEx.
/// </summary>
/// <typeparam name="T">Type that implements ILinkIndicatorEx</typeparam>
public abstract class LinkIndicatorExTestBase<T>
    where T : ILinkIndicator, IDisposable
{
    protected abstract T CreateLinkIndicator(int downgradeErrors = 3);
    
    
    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var linkIndicator = CreateLinkIndicator();

        // Assert
        Assert.Equal(LinkState.Disconnected, linkIndicator.State.Value);
        Assert.NotNull(linkIndicator.OnFound);
        Assert.NotNull(linkIndicator.OnLost);
    }
    [Fact]
    public void Dispose_DisposesResources()
    {
        // Arrange
        var linkIndicator = CreateLinkIndicator();

        // Act
        linkIndicator.Dispose();

        // Assert
        Assert.Throws<ObjectDisposedException>(() => linkIndicator.OnFound.Subscribe(_ => { }));
        Assert.Throws<ObjectDisposedException>(() => linkIndicator.OnLost.Subscribe(_ => { }));
    }
}