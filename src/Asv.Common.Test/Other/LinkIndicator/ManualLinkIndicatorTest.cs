using System;
using Xunit;

namespace Asv.Common.Test;

public class ManualLinkIndicatorTest : LinkIndicatorExTestBase<ManualLinkIndicator>
{
    protected override ManualLinkIndicator CreateLinkIndicator(int downgradeErrors = 3)
    {
        return new ManualLinkIndicator();
    }

    [Fact]
    public void InternalUpgrade_SetsStateToConnected()
    {
        // Arrange
        var linkIndicator = CreateLinkIndicator();

        // Act
        linkIndicator.Upgrade();

        // Assert
        Assert.Equal(LinkState.Connected, linkIndicator.Value);
    }

    [Fact]
    public void InternalDowngrade_MovesThroughStatesCorrectly()
    {
        // Arrange
        int downgradeErrors = 3;
        var linkIndicator = CreateLinkIndicator(downgradeErrors);

        // Act & Assert
        // First downgrade - should move to Downgrade
        linkIndicator.Downgrade();
        Assert.Equal(LinkState.Downgrade, linkIndicator.Value);

        // Second downgrade - should still be in Downgrade
        linkIndicator.Downgrade();
        Assert.Equal(LinkState.Downgrade, linkIndicator.Value);

        // Third downgrade - should move to Disconnected
        linkIndicator.Downgrade();
        Assert.Equal(LinkState.Disconnected, linkIndicator.Value);
    }

    [Fact]
    public void ForceDisconnected_SetsStateToDisconnected()
    {
        // Arrange
        var linkIndicator = CreateLinkIndicator();
        linkIndicator.Upgrade(); // Set to connected first

        // Act
        linkIndicator.ForceDisconnected();

        // Assert
        Assert.Equal(LinkState.Disconnected, linkIndicator.Value);
    }

    [Fact]
    public void OnLost_EmitsWhenDisconnected()
    {
        // Arrange
        var linkIndicator = CreateLinkIndicator();
        bool lostEmitted = false;
        linkIndicator.OnLost.Subscribe(x => lostEmitted = true);

        // Act
        linkIndicator.Downgrade(); // move to Downgrade
        linkIndicator.Downgrade(); // move to Disconnected

        // Assert
        Assert.True(lostEmitted);
    }
}
