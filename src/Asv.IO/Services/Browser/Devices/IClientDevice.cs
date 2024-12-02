using System;
using R3;

namespace Asv.IO;

public interface IClientDevice:IDisposable, IAsyncDisposable
{
    DeviceId Id { get; }
    ReadOnlyReactiveProperty<string> Name { get; }
    ReadOnlyReactiveProperty<ClientDeviceState> InitState { get; }
    IClientDeviceProvider Provider { get; }
}

public enum ClientDeviceState
{
    /// <summary>
    /// Represent 
    /// </summary>
    Uninitialized,

    /// <summary>
    /// Represents the state of an initialization process where the initialization has failed.
    /// </summary>
    Failed,

    /// <summary>
    /// Represents the current initialization state as being in progress.
    /// </summary>
    InProgress,

    /// <summary>
    /// Represents the initialization state of a process.
    /// </summary>
    Complete
}