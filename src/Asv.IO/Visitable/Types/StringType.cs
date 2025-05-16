namespace Asv.IO;

public sealed class StringType : FieldType
{
    public static readonly StringType Default = new();

    public override FieldTypeId TypeId => FieldTypeId.String;
    public override string Name => "utf8";

    public static void Accept(IVisitor visitor,Field field, ref string value)
    {
        if (visitor is IVisitor<string> accept)
        {
            accept.Visit(field, ref value);
        }
        else
        {
            visitor.VisitUnknown(field);
        }
        
    }
}