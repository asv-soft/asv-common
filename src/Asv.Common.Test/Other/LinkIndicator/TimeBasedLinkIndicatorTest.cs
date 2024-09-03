using System;
using System.Reactive;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Asv.Common.Test;

public class TimeBasedLinkIndicatorTest:LinkIndicatorExTestBase<TimeBasedLinkIndicator>
{
    protected override TimeBasedLinkIndicator CreateLinkIndicator(int downgradeErrors = 3)
    {
        return new TimeBasedLinkIndicator(TimeSpan.FromSeconds(1), downgradeErrors);
    }

    [Fact]
    public void Upgrade_UpdatesStateToConnectedAndResetsTimer()
    {
        var fakeTime = new FakeTimeProvider();
        var linkIndicator = new TimeBasedLinkIndicator(TimeSpan.FromSeconds(1), timeProvider: fakeTime);
        // start in disconnected state
        Assert.Equal(LinkState.Disconnected, linkIndicator.Value);
        // upgrade to connected state
        linkIndicator.Upgrade();
        Assert.Equal(LinkState.Connected, linkIndicator.Value);
        // advance time by 500 ms
        fakeTime.Advance(TimeSpan.FromMilliseconds(500));
        // connection still active
        Assert.Equal(LinkState.Connected, linkIndicator.Value);
        // advance time by 501 ms
        fakeTime.Advance(TimeSpan.FromMilliseconds(501));
        Assert.Equal(LinkState.Downgrade, linkIndicator.Value);
        fakeTime.Advance(TimeSpan.FromMilliseconds(1000));
        Assert.Equal(LinkState.Downgrade, linkIndicator.Value);
        fakeTime.Advance(TimeSpan.FromMilliseconds(1000));
        Assert.Equal(LinkState.Disconnected, linkIndicator.Value);
    }
    
}

public class TimeBasedObservableLinkIndicatorTest:LinkIndicatorExTestBase<TimeBasedObservableLinkIndicator<Unit>>
{
    protected override TimeBasedObservableLinkIndicator<Unit> CreateLinkIndicator(int downgradeErrors = 3)
    {
        return new TimeBasedObservableLinkIndicator<Unit>(TimeSpan.FromSeconds(1), downgradeErrors);
    }

    [Fact]
    public void Upgrade_UpdatesStateToConnectedAndResetsTimer()
    {
        var fakeTime = new FakeTimeProvider();
        
        var linkIndicator = new TimeBasedLinkIndicator(TimeSpan.FromSeconds(1), timeProvider: fakeTime);
        // start in disconnected state
        Assert.Equal(LinkState.Disconnected, linkIndicator.Value);
        // upgrade to connected state
        linkIndicator.Upgrade();
        Assert.Equal(LinkState.Connected, linkIndicator.Value);
        // advance time by 500 ms
        fakeTime.Advance(TimeSpan.FromMilliseconds(500));
        // connection still active
        Assert.Equal(LinkState.Connected, linkIndicator.Value);
        // advance time by 501 ms
        fakeTime.Advance(TimeSpan.FromMilliseconds(501));
        Assert.Equal(LinkState.Downgrade, linkIndicator.Value);
        fakeTime.Advance(TimeSpan.FromMilliseconds(1000));
        Assert.Equal(LinkState.Downgrade, linkIndicator.Value);
        fakeTime.Advance(TimeSpan.FromMilliseconds(1000));
        Assert.Equal(LinkState.Disconnected, linkIndicator.Value);
    
        
    }
    
}