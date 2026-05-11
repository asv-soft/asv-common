using System.Threading.Tasks;
using R3;

namespace Asv.Common;

public class AsyncDisposableOnceBag : AsyncDisposableOnce
{
    private DisposableBag _disposableBag;

    protected ref DisposableBag DisposableBag => ref _disposableBag;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _disposableBag.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        _disposableBag.Dispose();
        await base.DisposeAsyncCore();
    }
}
