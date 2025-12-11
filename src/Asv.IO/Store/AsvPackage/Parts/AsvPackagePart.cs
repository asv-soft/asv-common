using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Asv.Common;
using DotNext.IO;
using R3;

namespace Asv.IO;

public abstract class AsvPackagePart
    : AsyncDisposableOnceBag,
        ISupportRoutedEvents<AsvPackagePart>,
        IFlushable
{
    protected AsvPackagePart(AsvPackageContext context, AsvPackagePart? parent)
    {
        Context = context;
        Parent = parent;
        Events = new RoutedEventController<AsvPackagePart>(this).AddTo(ref DisposableBag);
    }

    public IRoutedEventController<AsvPackagePart> Events { get; }
    protected AsvPackageContext Context { get; }

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

    public AsvPackagePart? Parent { get; set; }
    public abstract IEnumerable<AsvPackagePart> GetChildren();
    public abstract void InternalFlush();

    public void Flush()
    {
        ThrowIfDisposed();
        EnsureWriteAccess();
        foreach (var child in GetChildren())
        {
            child.Flush();
        }
        InternalFlush();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // only flush if not read-only
            if (Context.Package.FileOpenAccess != FileAccess.Read)
            {
                Flush();
                Context.Package.Flush();
            }
            foreach (var child in GetChildren())
            {
                child.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        // only flush if not read-only
        if (Context.Package.FileOpenAccess != FileAccess.Read)
        {
            Flush();
            Context.Package.Flush();
        }
        foreach (var child in GetChildren())
        {
            await child.DisposeAsync();
        }
        await base.DisposeAsyncCore();
    }
}
