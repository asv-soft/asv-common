namespace Asv.Modeling;

/// <summary>
/// Applies persisted layout data to a registered model member.
/// </summary>
/// <typeparam name="TData">The concrete layout data type.</typeparam>
/// <param name="data">The restored layout data.</param>
/// <param name="cancel">A cancellation token for the operation.</param>
/// <returns>A task-like value that completes when the operation finishes.</returns>
public delegate ValueTask AsyncLoadLayoutCallback<in TData>(TData data, CancellationToken cancel)
    where TData : ILayoutData;

/// <summary>
/// Captures current display state into a registered layout data object.
/// </summary>
/// <typeparam name="TData">The concrete layout data type.</typeparam>
/// <param name="data">The layout data object to fill.</param>
/// <param name="cancel">A cancellation token for the operation.</param>
/// <returns>A task-like value that completes when the operation finishes.</returns>
public delegate ValueTask AsyncSaveLayoutCallback<in TData>(TData data, CancellationToken cancel)
    where TData : ILayoutData;

/// <summary>
/// Applies persisted layout data to a registered model member.
/// </summary>
/// <typeparam name="TData">The concrete layout data type.</typeparam>
/// <param name="data">The restored layout data.</param>
public delegate void LoadLayoutCallback<in TData>(TData data)
    where TData : ILayoutData;

/// <summary>
/// Captures current display state into a registered layout data object.
/// </summary>
/// <typeparam name="TData">The concrete layout data type.</typeparam>
/// <param name="data">The layout data object to fill.</param>
public delegate void SaveLayoutCallback<in TData>(TData data)
    where TData : ILayoutData;

/// <summary>
/// Registered layout data handler.
/// </summary>
public interface ILayoutRegistration : IDisposable
{
    string Id { get; }
    ILayoutData Create();
    ValueTask LoadAsync(CancellationToken cancel = default);
    ValueTask SaveAsync(CancellationToken cancel = default);
}

/// <summary>
/// Registers display state members and coordinates their persistence.
/// </summary>
public interface ILayoutController : IDisposable
{
    ILayoutRegistration Create<TData>(
        string layoutId,
        AsyncLoadLayoutCallback<TData> load,
        AsyncSaveLayoutCallback<TData> save,
        Func<TData> factory
    )
        where TData : ILayoutData;

    ILayoutRegistration this[string layoutId] { get; }

    ValueTask LoadAsync(CancellationToken cancel = default);
    ValueTask SaveAsync(CancellationToken cancel = default);
}
