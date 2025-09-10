using System.Collections.Immutable;
using System.Composition.Hosting;
using Asv.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ZLogger;

namespace Asv.Composition;

public static class CompositionHostHelper
{
    /// <summary>
    /// Sorts and retrieves the exports from the provided CompositionHost based on their dependencies.
    /// </summary>
    /// <typeparam name="T">The type of the class to export.</typeparam>
    /// <param name="container">The CompositionHost containing the exports.</param>
    /// <returns>A sorted collection of items based on their dependencies.</returns>
    /// <exception cref="Exception">Thrown if there is a circular dependency or a missing dependency.</exception>
    public static IEnumerable<Lazy<T, DependencyMetadata>> ExportWithDependencies<T>(
        this CompositionHost container
    )
        where T : IDisposable
    {
        return container
            .GetExports<Lazy<T, DependencyMetadata>>()
            .ExportWithDependencies(x => x.Name, x => x.Dependencies);
    }

    /// <summary>
    /// Sorts and retrieves the exports from the provided CompositionHost based on their dependencies.
    /// </summary>
    /// <typeparam name="T">The type of the class to export.</typeparam>
    /// <param name="items">Items .</param>
    /// <returns>A sorted collection of items based on their dependencies.</returns>
    /// <exception cref="Exception">Thrown if there is a circular dependency or a missing dependency.</exception>
    public static IEnumerable<Lazy<T, DependencyMetadata>> ExportWithDependencies<T>(
        this IEnumerable<Lazy<T, DependencyMetadata>> items
    )
        where T : IDisposable
    {
        return ExportWithDependencies(items, x => x.Name, x => x.Dependencies);
    }

    /// <summary>
    /// Sorts and retrieves the exports from the provided CompositionHost based on their dependencies.
    /// </summary>
    /// <typeparam name="T">The type of the class to export.</typeparam>
    /// <typeparam name="TMetadata">The type of the metadata associated with the class.</typeparam>
    /// <param name="container">The CompositionHost containing the exports.</param>
    /// <param name="getName">A function to retrieve the name from the metadata.</param>
    /// <param name="getDeps">A function to retrieve the dependencies from the metadata.</param>
    /// <returns>A sorted collection of items based on their dependencies.</returns>
    /// <exception cref="Exception">Thrown if there is a circular dependency or a missing dependency.</exception>
    public static IEnumerable<Lazy<T, TMetadata>> ExportWithDependencies<T, TMetadata>(
        this CompositionHost container,
        Func<TMetadata, string> getName,
        Func<TMetadata, string[]> getDeps
    )
        where T : IDisposable
    {
        return container.GetExports<Lazy<T, TMetadata>>().ExportWithDependencies(getName, getDeps);
    }

    /// <summary>
    /// Sorts and retrieves the exports from the provided collection of items based on their dependencies.
    /// </summary>
    /// <typeparam name="T">The type of the class to export.</typeparam>
    /// <typeparam name="TMetadata">The type of the metadata associated with the class.</typeparam>
    /// <typeparam name="TId">The type of the identifier used for the class names and dependencies.</typeparam>
    /// <param name="items">The collection of items to sort and retrieve.</param>
    /// <param name="getName">A function to retrieve the name from the metadata.</param>
    /// <param name="getDeps">A function to retrieve the dependencies from the metadata.</param>
    /// <returns>A sorted collection of items based on their dependencies.</returns>
    /// <exception cref="Exception">Thrown if there is a circular dependency or a missing dependency.</exception>
    public static IEnumerable<Lazy<T, TMetadata>> ExportWithDependencies<T, TMetadata, TId>(
        this IEnumerable<Lazy<T, TMetadata>> items,
        Func<TMetadata, TId> getName,
        Func<TMetadata, TId[]> getDeps
    )
        where T : IDisposable
        where TId : notnull
    {
        var itemsArray = items.ToImmutableArray();
        var itemsDict = itemsArray.ToImmutableDictionary(
            x => getName(x.Metadata),
            x => getDeps(x.Metadata)
        );

        // Sort items by dependencies
        foreach (var item in DepthFirstSearch.Sort(itemsDict))
        {
            yield return itemsArray.First(x => getName(x.Metadata).Equals(item));
        }
    }

    public static Lazy<TInterface, NameMetadata> GetExportFromConfig<TInterface>(
        this CompositionHost container,
        Dictionary<string, bool> config,
        out bool configUpdated,
        ILogger? logger = null
    )
    {
        return GetExportFromConfig(
            container.GetExports<Lazy<TInterface, NameMetadata>>(),
            config,
            x => x.Name,
            out configUpdated,
            x => x.Priority,
            logger
        );
    }

    /// <summary>
    /// Retrieves a single export from the provided CompositionHost based on the configuration.
    /// Ensures that only one implementation is enabled in the configuration.
    /// Updates the configuration to reflect the current state of available implementations.
    /// </summary>
    /// <typeparam name="TInterface">The type of the interface to export.</typeparam>
    /// <typeparam name="TMetadata">The type of the metadata associated with the export.</typeparam>
    /// <param name="container">The CompositionHost containing the exports.</param>
    /// <param name="config">The configuration dictionary indicating which implementations are enabled.</param>
    /// <param name="getName">A function to retrieve the name from the metadata.</param>
    /// <param name="configUpdated">Indicates whether the configuration was updated.</param>
    /// <param name="getPriority"></param>
    /// <param name="logger">Optional logger for logging debug information.</param>
    /// <returns>The single enabled export from the CompositionHost.</returns>
    /// <exception cref="Exception">
    /// Thrown if no implementations are found, if multiple implementations are enabled,
    /// or if all implementations are disabled.
    /// </exception>
    public static Lazy<TInterface, TMetadata> GetExportFromConfig<TInterface, TMetadata>(
        this CompositionHost container,
        Dictionary<string, bool> config,
        Func<TMetadata, string> getName,
        out bool configUpdated,
        Func<TMetadata, int>? getPriority = null,
        ILogger? logger = null
    )
    {
        return GetExportFromConfig(
            container.GetExports<Lazy<TInterface, TMetadata>>(),
            config,
            getName,
            out configUpdated,
            getPriority,
            logger
        );
    }

    /// <summary>
    /// Retrieves a single export from the provided collection of items based on the configuration.
    /// Ensures that only one implementation is enabled in the configuration.
    /// Updates the configuration to reflect the current state of available implementations.
    /// </summary>
    /// <typeparam name="TInterface">The type of the interface to export.</typeparam>
    /// <typeparam name="TMetadata">The type of the metadata associated with the export.</typeparam>
    /// <typeparam name="TKey"> </typeparam>
    /// <param name="items">The collection of items to search for the export.</param>
    /// <param name="config">The configuration dictionary indicating which implementations are enabled.</param>
    /// <param name="getId">A function to retrieve the name from the metadata.</param>
    /// <param name="getPriority"></param>
    /// <param name="configUpdated">Indicates whether the configuration was updated.</param>
    /// <param name="logger">Optional logger for logging debug information.</param>
    /// <returns>The single enabled export from the collection.</returns>
    /// <exception cref="Exception">
    /// Thrown if no implementations are found, if multiple implementations are enabled,
    /// or if all implementations are disabled.
    /// </exception>
    public static Lazy<TInterface, TMetadata> GetExportFromConfig<TInterface, TMetadata, TKey>(
        this IEnumerable<Lazy<TInterface, TMetadata>> items,
        Dictionary<TKey, bool> config,
        Func<TMetadata, TKey> getId,
        out bool configUpdated,
        Func<TMetadata, int>? getPriority = null,
        ILogger? logger = null
    )
        where TKey : notnull
    {
        configUpdated = false;
        logger ??= NullLogger.Instance;
        var foundImpl = items.ToImmutableArray();
        var foundNames = foundImpl.Select(x => getId(x.Metadata)).ToImmutableHashSet();
        if (foundNames.IsEmpty)
        {
            throw new Exception($"Implementation of '{nameof(TInterface)}' not found.");
        }

        // create implementation that not found at config
        foreach (var name in foundNames.Where(name => !config.TryGetValue(name, out _)))
        {
            logger.ZLogDebug($"Add new implementation '{name}' of {nameof(TInterface)} to config");
            config.Add(name, false);
            configUpdated = true;
        }

        // check not exist implementation at config
        var foundAtConfig = config.Keys.ToImmutableHashSet();
        foreach (var name in foundAtConfig.Where(name => !foundNames.Contains(name)))
        {
            config.Remove(name);
            configUpdated = true;
            logger.ZLogDebug(
                $"Remove not found implementation '{name}' of {nameof(TInterface)} from config"
            );
        }

        var enabledCount = config.Count(x => x.Value);
        if (enabledCount > 1)
        {
            var enabledItems = config.Where(x => x.Value).Select(x => x.Key).ToHashSet();
            if (getPriority == null)
            {
                throw new Exception(
                    $"Found several enabled implementations of '{nameof(TInterface)}': "
                        + $"{string.Join(",", enabledItems)}. Edit config and enable only one..."
                );
            }
            var first = foundImpl
                .Where(x => enabledItems.Contains(getId(x.Metadata)))
                .OrderBy(x => getPriority(x.Metadata))
                .First();
            logger.ZLogWarning(
                $"Found several enabled implementations of '{nameof(TInterface)}': {string.Join(",", enabledItems)}. Try select by priority '{first.Metadata}' ({getPriority(first.Metadata)})"
            );
            foreach (var cfg in config)
            {
                config[cfg.Key] = false;
            }
            config[getId(first.Metadata)] = true;
            configUpdated = true;
        }
        if (config.All(x => x.Value == false))
        {
            if (getPriority == null)
            {
                throw new Exception(
                    $"All implementation of '{nameof(TInterface)}' is disabled. Edit config and enable one..."
                );
            }
            var first = foundImpl.OrderBy(x => getPriority(x.Metadata)).First();
            logger.ZLogWarning(
                $"All implementation of '{nameof(TInterface)}' is disabled. Try select by priority '{first.Metadata}' ({getPriority(first.Metadata)})"
            );
            config[getId(first.Metadata)] = true;
            configUpdated = true;
        }

        var enabled = config.First(x => x.Value);
        return foundImpl.First(x => getId(x.Metadata).Equals(enabled.Key));
    }
}
