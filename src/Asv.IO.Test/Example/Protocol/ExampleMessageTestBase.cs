using DeepEqual.Syntax;
using Xunit;

namespace Asv.IO.Test.Example.Protocol;

public abstract class ExampleMessageTestBase<T>
    where T : IProtocolMessage, IVisitable, new()
{
    [Fact]
    public void SerializeDeserialize_RandomizedMessage_RestoresOriginalAndConsumesSpan()
    {
        // Arrange: Initialize and randomize an ExampleMessage1 instance
        var origin = new T().Randomize();
        
        
        // Act: Serialize the instance into the byte array
        var arr = origin.Serialize();

        // Arrange: Create a new instance for deserialization
        var restored = new T();

        // Act: Deserialize the byte array into the new instance
        var wSize = restored.Deserialize(arr);

        // Assert: Ensure the entire span was consumed and instances match
        Assert.Equal(arr.Length, wSize);
        
        origin.ShouldDeepEqual(restored);
    }
}