using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.Extensions.Time.Testing;
using R3;
using Xunit;

namespace Asv.Common.Test;

[TestSubject(typeof(TimeBasedObservableLinkIndicator<Unit>))]
public class TimeBasedObservableLinkIndicatorTest:LinkIndicatorExTestBase<TimeBasedObservableLinkIndicator<Unit>>
{
    private readonly Subject<Unit> _input = new();
    private readonly FakeTimeProvider _fakeTime = new(DateTimeOffset.Now);
    protected override TimeBasedObservableLinkIndicator<Unit> CreateLinkIndicator(int downgradeErrors = 3)
    {
        return new TimeBasedObservableLinkIndicator<Unit>(_input,TimeSpan.FromSeconds(1), downgradeErrors,_fakeTime);
    }

    [Fact]
    public void Upgrade_UpdatesStateToConnectedAndResetsTimer()
    {
        var linkIndicator = CreateLinkIndicator();
        // start in disconnected state
        Assert.Equal(LinkState.Disconnected, linkIndicator.State.Value);
        // upgrade to connected state
        _input.OnNext(Unit.Default);
        Assert.Equal(LinkState.Connected, linkIndicator.State.Value);
        // advance time by 500 ms
        _fakeTime.Advance(TimeSpan.FromMilliseconds(500));
        // connection still active
        Assert.Equal(LinkState.Connected, linkIndicator.State.Value);
        // advance time by 501 ms
        _fakeTime.Advance(TimeSpan.FromMilliseconds(520));
        Assert.Equal(LinkState.Downgrade, linkIndicator.State.Value);
        _fakeTime.Advance(TimeSpan.FromMilliseconds(1000));
        Assert.Equal(LinkState.Downgrade, linkIndicator.State.Value);
        _fakeTime.Advance(TimeSpan.FromMilliseconds(1000));
        Assert.Equal(LinkState.Disconnected, linkIndicator.State.Value);
    
        
    }
    
}