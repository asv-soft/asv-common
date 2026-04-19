using JetBrains.Annotations;

namespace Asv.Modeling.Test;

[TestSubject(typeof(NavigationStore))]
public class NavigationStoreTest : IDisposable
{
    private readonly string _storageDirectory = Path.Combine(
        Path.GetTempPath(),
        "Asv.Modeling.Test",
        nameof(NavigationStoreTest),
        Guid.NewGuid().ToString("N")
    );

    [Fact]
    public void SaveAndLoad_RestoresBothStacksInSameOrder()
    {
        var store = new NavigationStore(_storageDirectory);
        var expectedForward = new[]
        {
            new NavPath(new NavId("root"), new NavId("page1")),
            new NavPath(new NavId("root"), new NavId("page2")),
        };
        var expectedBackward = new[]
        {
            new NavPath(new NavId("root"), new NavId("details")),
            new NavPath(new NavId("root"), new NavId("summary")),
        };

        store.Save(expectedForward, expectedBackward);

        var actualForward = new List<NavPath>();
        var actualBackward = new List<NavPath>();
        store.Load(actualForward.Add, actualBackward.Add);

        Assert.Equal(expectedForward, actualForward);
        Assert.Equal(expectedBackward, actualBackward);
    }

    [Fact]
    public void Load_SkipsInvalidAndEmptyLines()
    {
        Directory.CreateDirectory(_storageDirectory);
        File.WriteAllLines(
            Path.Combine(_storageDirectory, "navigation.forward.txt"),
            ["root/page1", "", "bad type!/page2"]
        );
        File.WriteAllLines(
            Path.Combine(_storageDirectory, "navigation.backward.txt"),
            ["root/details"]
        );

        var actualForward = new List<NavPath>();
        var actualBackward = new List<NavPath>();

        var store = new NavigationStore(_storageDirectory);
        store.Load(actualForward.Add, actualBackward.Add);

        Assert.Single(actualForward);
        Assert.Equal(new NavPath(new NavId("root"), new NavId("page1")), actualForward[0]);
        Assert.Single(actualBackward);
        Assert.Equal(new NavPath(new NavId("root"), new NavId("details")), actualBackward[0]);
    }

    public void Dispose()
    {
        if (Directory.Exists(_storageDirectory))
        {
            Directory.Delete(_storageDirectory, true);
        }
    }
}
