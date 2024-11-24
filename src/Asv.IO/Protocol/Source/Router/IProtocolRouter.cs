using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using ObservableCollections;
using Disposable = DotNext.Disposable;

namespace Asv.IO;


public class PortConfig
{
    public required string Name { get; set; }
    public required string ConnectionString { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public interface IProtocolRouter : IProtocolConnection
{
    Task<PortConfig[]> GetConfig();
    Task<IProtocolPort> AddPort(PortConfig port);
    Task<bool> RemovePort(string portId, CancellationToken cancel);
    IReadOnlyObservableList<IProtocolPort> Ports { get; }
}