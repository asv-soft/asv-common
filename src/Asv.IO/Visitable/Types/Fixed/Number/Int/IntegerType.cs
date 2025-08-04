namespace Asv.IO;

public abstract class IntegerType<TSelf, TValue>(TValue min, TValue max, TValue defaultValue) 
    : NumberType<TSelf, TValue>(min, max, defaultValue) where TSelf : IFieldType
{
    
}