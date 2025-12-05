using System.Collections.Generic;

namespace Asv.Common;

public interface ISupportChildren<out T>
    where T : ISupportChildren<T>
{
    IEnumerable<T> GetChildren();
}
