using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Asv.Common;
using R3;

namespace Asv.IO;

public abstract class AsvPackagePart(AsvPackageContext context) : AsyncDisposableOnce
{
    private DisposableBag _disposeBag;
    protected AsvPackageContext Context => context;

    protected void EnsureReadAccess()
    {
        if (
            Context.Package.FileOpenAccess != FileAccess.Read
            && Context.Package.FileOpenAccess != FileAccess.ReadWrite
        )
        {
            throw new InvalidOperationException("Package is not opened with read access");
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected void EnsureWriteAccess()
    {
        if (
            Context.Package.FileOpenAccess != FileAccess.Write
            && Context.Package.FileOpenAccess != FileAccess.ReadWrite
        )
        {
            throw new InvalidOperationException("Package is not opened with write access");
        }
    }

    protected T AddToDispose<T>(T obj)
    {
        if (obj is IDisposable disposable)
        {
            _disposeBag.Add(disposable);
        }
        return obj;
    }

    public abstract void Flush();

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Flush();
            _disposeBag.Dispose();
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        Flush();
        _disposeBag.Dispose();
        await base.DisposeAsyncCore();
    }
}
