using System.Collections.Immutable;

namespace Asv.IO;

public interface IVisitable
{
    void Accept(IVisitor visitor);
}
