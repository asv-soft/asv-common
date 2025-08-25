using Newtonsoft.Json;

namespace Asv.IO;

public interface IJsonSerializable
{
    void Serialize(JsonWriter writer);
    void Deserialize(JsonReader reader);
}