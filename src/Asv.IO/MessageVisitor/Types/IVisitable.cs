namespace Asv.IO.MessageVisitor;

public interface IVisitable
{
    void Accept(IMessageVisitor visitor);
}