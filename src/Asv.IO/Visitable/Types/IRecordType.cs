using System.Collections.Generic;

namespace Asv.IO;

public interface IRecordType : IFieldType
{
    int FieldCount { get; }

    Field GetFieldByIndex(int index);
    Field GetFieldByName(string name);
    int GetFieldIndex(string name, IEqualityComparer<string> comparer);
}