using System;

namespace Asv.IO;

public class HasChangesEvent(AsvPackagePart part, bool hasChanged) : EventArgs
{
    public AsvPackagePart Part { get; } = part;
    public bool HasChanged { get; } = hasChanged;
}
