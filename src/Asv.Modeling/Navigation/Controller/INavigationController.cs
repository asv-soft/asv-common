using ObservableCollections;
using R3;

namespace Asv.Modeling;

public interface INavigationController<TBase>
{
    /// <summary>
    /// Gets the observable collection representing the backward navigation history.
    /// </summary>
    IObservableCollection<NavPath> BackwardStack { get; }

    /// <summary>
    /// Navigates to the previous item in the backward navigation stack.
    /// </summary>
    /// <param name="cancel">The token used to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask BackwardAsync(CancellationToken cancel = default);

    /// <summary>
    /// Gets the <see cref="ReactiveCommand"/> that triggers backward navigation.
    /// </summary>
    ReactiveCommand Backward { get; }

    /// <summary>
    /// Gets the observable collection representing the forward navigation history.
    /// </summary>
    IObservableCollection<NavPath> ForwardStack { get; }

    /// <summary>
    /// Navigates to the next item in the forward navigation stack.
    /// </summary>
    /// <param name="cancel">The token used to cancel the operation.</param>
    /// <returns>A <see cref="ValueTask"/> representing an asynchronous operation.</returns>
    ValueTask ForwardAsync(CancellationToken cancel = default);

    /// <summary>
    /// Gets the <see cref="ReactiveCommand"/> that triggers forward navigation.
    /// </summary>
    ReactiveCommand Forward { get; }

    /// <summary>
    /// Gets the currently selected (focused) <see cref="TBase"/>.
    /// </summary>
    ReadOnlyReactiveProperty<TBase?> SelectedControl { get; }

    /// <summary>
    /// Gets the <see cref="NavPath"/> of the currently selected control.
    /// </summary>
    ReadOnlyReactiveProperty<NavPath> SelectedPath { get; }

    ValueTask<TBase> GoTo(NavPath navPath, CancellationToken cancel = default);

    /// <summary>
    /// Forces focus change to the specified routable control.
    /// </summary>
    /// <param name="viewModel">Routable control to be set as currently focused.</param>
    void ForceSelect(TBase? viewModel);
}
