using System.Collections.Generic;
using LiteDB;

namespace Asv.Store
{
    public interface IKeyValueStore
    {
        string Name { get; }
        IEnumerable<string> Ids { get; }
        BsonValue Read(string id);
        void Write(string id, BsonValue bsonValue);
    }
}
