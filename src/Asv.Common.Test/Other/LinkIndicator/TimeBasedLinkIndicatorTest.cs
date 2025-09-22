using System;
using JetBrains.Annotations;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Asv.Common.Test.Other.LinkIndicator;

[TestSubject(typeof(TimeBasedLinkIndicator))]
public class TimeBasedLinkIndicatorTest : LinkIndicatorExTestBase<TimeBasedLinkIndicator>
{
    readonly FakeTimeProvider _fakeTime = new();

    protected override TimeBasedLinkIndicator CreateLinkIndicator(int downgradeErrors = 3)
    {
        return new TimeBasedLinkIndicator(TimeSpan.FromSeconds(1), downgradeErrors, _fakeTime);
    }

    [Fact]
    public void Upgrade_UpdatesStateToConnectedAndResetsTimer()
    {
        var linkIndicator = CreateLinkIndicator();

        // start in disconnected state
        Assert.Equal(LinkState.Disconnected, linkIndicator.State.CurrentValue);

        // upgrade to connected state
        linkIndicator.Upgrade();
        Assert.Equal(LinkState.Connected, linkIndicator.State.CurrentValue);

        // advance time by 500 ms
        _fakeTime.Advance(TimeSpan.FromMilliseconds(500));

        // connection still active
        Assert.Equal(LinkState.Connected, linkIndicator.State.CurrentValue);

        // advance time by 501 ms
        _fakeTime.Advance(TimeSpan.FromMilliseconds(501));
        Assert.Equal(LinkState.Downgrade, linkIndicator.State.CurrentValue);
        _fakeTime.Advance(TimeSpan.FromMilliseconds(1000));
        Assert.Equal(LinkState.Downgrade, linkIndicator.State.CurrentValue);
        _fakeTime.Advance(TimeSpan.FromMilliseconds(1000));
        Assert.Equal(LinkState.Disconnected, linkIndicator.State.CurrentValue);
    }
}
