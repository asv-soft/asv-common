namespace Asv.IO;

public interface IVisitable
{
    void Accept(IVisitor visitor);
}
