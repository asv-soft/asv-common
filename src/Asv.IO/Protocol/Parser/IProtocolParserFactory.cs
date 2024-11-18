using System.Collections.Generic;

namespace Asv.IO;

public interface IProtocolParserFactory
{
    IReadOnlySet<string> AvailableProtocolIds { get; }
    IProtocolParser Create(string protocolId);
}