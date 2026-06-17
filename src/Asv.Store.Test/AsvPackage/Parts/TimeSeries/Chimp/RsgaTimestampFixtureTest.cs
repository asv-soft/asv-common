using System.Reflection;
using System.Text.Json;
using ZstdSharp;

namespace Asv.Store.Test;

public class RsgaTimestampFixtureTest
{
    [Fact]
    public void RsgaTimestampFixtures_ShouldDecodeNewAndLegacyTimestampPayloads()
    {
        var resource = LoadResource();

        Assert.NotNull(resource.Fixtures);
        Assert.NotEmpty(resource.Fixtures);
        foreach (var fixture in resource.Fixtures)
        {
            try
            {
                var rawPayload = Convert.FromBase64String(fixture.PayloadBase64);
                var payload = fixture.IsCompressed ? Decompress(rawPayload) : rawPayload;

                var timestamps = ChimpTableDecoder.DecodeTimestamps(payload, fixture.RowCount);

                Assert.Equal(fixture.RowCount, timestamps.Length);
                Assert.True(
                    timestamps.Zip(timestamps.Skip(1)).All(pair => pair.First <= pair.Second),
                    $"Timestamps must be monotonic for fixture '{fixture.Id}'."
                );
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to decode timestamp fixture '{fixture.Id}' from '{fixture.SourceFile}' part '{fixture.PackagePart}' batch {fixture.BatchIndex}.",
                    ex
                );
            }
        }
    }

    private static byte[] Decompress(byte[] payload)
    {
        using var decompressor = new Decompressor();
        return decompressor.Unwrap(payload).ToArray();
    }

    private static RsgaTimestampFixtureResource LoadResource()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly
            .GetManifestResourceNames()
            .Single(name => name.EndsWith("RsgaTimestampFixtures.json", StringComparison.Ordinal));
        using var stream =
            assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Resource '{resourceName}' was not found.");

        return JsonSerializer.Deserialize<RsgaTimestampFixtureResource>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? throw new InvalidOperationException("Failed to deserialize timestamp fixtures.");
    }

    private sealed class RsgaTimestampFixtureResource
    {
        public required RsgaTimestampFixture[] Fixtures { get; init; }
    }

    private sealed class RsgaTimestampFixture
    {
        public required string Id { get; init; }
        public required string SourceFile { get; init; }
        public required string PackagePart { get; init; }
        public int BatchIndex { get; init; }
        public int RowCount { get; init; }
        public bool IsCompressed { get; init; }
        public required string PayloadBase64 { get; init; }
    }
}
