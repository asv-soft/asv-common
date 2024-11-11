using System.Diagnostics;

namespace Asv.IO;

public interface IPipeConnection
{
    TagList Tags { get; }
}