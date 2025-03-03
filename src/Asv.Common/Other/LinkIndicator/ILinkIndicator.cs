using R3;

namespace Asv.Common
{
    public interface ILinkIndicator
    {
        ReadOnlyReactiveProperty<LinkState> State { get; }
        /// <summary>
        /// Represents an event that is triggered when the link is connected.
        /// This event happens when the last state was Disconnected and the new state is Connected (downgrade is not considered).
        /// If current state is Connected event will be risen immediately after subscription.
        /// </summary>
        Observable<Unit> OnFound => State.Where(x => x == LinkState.Connected).Select(_ => Unit.Default);

        /// <summary>
        /// Represents an event that is triggered when the link is lost.
        /// This event happens when the last state was Connected or Downgrade, and the new state is Disconnected.
        /// If current state is Disconnected event will be risen immediately after subscription.
        /// </summary>
        /// <remarks>
        /// This event is triggered when the number of connection errors exceeds the specified downgrade errors.
        /// </remarks>
        Observable<Unit> OnLost => State.Where(x => x == LinkState.Disconnected).Select(_ => Unit.Default);
    }

    public enum LinkState
    {
        Disconnected,
        Downgrade,
        Connected,
    }
}
