namespace Asv.IO.MessageVisitor;

public interface IMessageVisitor<TValue> : IMessageVisitor
{
    void Visit(IField field, IType type, ref TValue value);
}

public interface IMessageVisitor
{
    void VisitUnknown(IField field);
}

public interface IFullMessageVisitor : 
    Int8T.IVisitor
{
    
}

public class FullMessageVisitor : IFullMessageVisitor
{
    public void Visit(IField field, IType type, ref sbyte value)
    {
        if (field is Int8T.Field f)
        {
            
        }
    }

    public void VisitUnknown(IField field, IType type)
    {
        throw new System.NotImplementedException();
    }

    public void VisitUnknown(IField field)
    {
        throw new System.NotImplementedException();
    }
}