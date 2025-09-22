using System.IO.Packaging;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Asv.IO;

public sealed class AsvPackageContext(Lock @lock, Package package, ILogger logger)
{
    public Lock Lock => @lock;
    public Package Package => package;
    public ILogger Logger => logger;
}
