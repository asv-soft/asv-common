using Asv.Common;

namespace Asv.Cfg;

public interface IVersionedFile:IConfiguration
{
    SemVersion FileVersion { get; }
}