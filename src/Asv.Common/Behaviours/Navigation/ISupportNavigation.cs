using System.Threading.Tasks;

namespace Asv.Common;

public interface ISupportNavigation<TBase, TId> : ISupportId<TId>
{
    ValueTask<TBase> Navigate(TId id);
}
