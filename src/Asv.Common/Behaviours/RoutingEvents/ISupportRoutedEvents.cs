using System.Threading.Tasks;

namespace Asv.Common;

public interface ISupportRoutedEvents<T> : ISupportParent<T>, ISupportChildren<T>
    where T : ISupportRoutedEvents<T>
{
    IRoutedEventController<T> Events { get; }
}
