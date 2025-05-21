namespace Asv.IO;

public abstract class IntegerType<TSelf, TValue>(TValue min, TValue max) 
    : NumberType<TSelf, TValue>(min, max) where TSelf : IFieldType
{
    
}