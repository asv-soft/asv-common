using System.Collections.Immutable;
using System.Net.Sockets;

namespace Asv.IO;

public class TcpServerSocketProtocolEndpoint(
    Socket socket,
    string id,
    ProtocolPortConfig config,
    ImmutableArray<IProtocolParser> parsers,
    IProtocolContext context,
    IStatisticHandler statisticHandler
) : TcpSocketProtocolEndpoint(socket, id, config, parsers, context, statisticHandler) { }
