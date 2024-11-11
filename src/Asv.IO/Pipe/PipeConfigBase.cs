using System;

namespace Asv.IO;

public abstract class PipeConfigBase
{
    public abstract bool TryValidate(out string? error);
    public void Validate()
    {
        if (!TryValidate(out var error))
        {
            throw new ArgumentException(error);
        }
    }
}