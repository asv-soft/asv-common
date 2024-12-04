using System.Collections.Generic;

namespace Asv.IO;

public static class DeviceClass
{
    public const string ClassDelimiter = ".";
    
    public static string Combine(IEnumerable<string> classes)
    {
        return string.Join(ClassDelimiter, classes);
    }
    public static string Combine(params string[] classes)
    {
        return string.Join(ClassDelimiter, classes);
    }
    
    public static IEnumerable<string> Split(string deviceClass)
    {
        return deviceClass.Split(ClassDelimiter);
    }
}