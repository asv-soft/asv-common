using System;
using System.Collections.Generic;
using DeepEqual.Syntax;
using Xunit;
using Xunit.Abstractions;

namespace Asv.IO.Test.Example.Protocol;

public abstract class ExampleMessageTestBase<T>(ITestOutputHelper output)
    where T : IProtocolMessage, IVisitable, new()
{
    [Fact]
    public void SerializeDeserialize_RandomizedMessage_RestoresOriginalAndConsumesSpan()
    {
        var seed = Random.Shared.Next();
        Random random = new Random(seed);
        try
        {
            // Arrange: Initialize and randomize an ExampleMessage1 instance
            var origin = new T().Randomize(random);

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
        catch (Exception e)
        {
            output.WriteLine($"Random seed: {seed}");
            throw;
        }
    }
}
