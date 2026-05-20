using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Asv.Common;
using JetBrains.Annotations;
using ObservableCollections;
using R3;

namespace Asv.Modeling.Test;

[TestSubject(typeof(ISupportLayout<>))]
public partial class ISupportLayoutTest : IDisposable
{
    private readonly string _storageDirectory = Path.Combine(
        Path.GetTempPath(),
        "Asv.Modeling.Test",
        nameof(ISupportLayoutTest),
        Guid.NewGuid().ToString("N")
    );

    [Fact]
    public async ValueTask SaveLoad_RestoresRegisteredLayoutByPath()
    {
        var root = new LayoutRootViewModel("root", new JsonLayoutStore(_storageDirectory));
        var child = new TestLayoutViewModel("child");
        root.Children.Add(child);
        var nested = new TestLayoutViewModel("nested");
        child.Children.Add(nested);

        child.Value = 10;
        nested.Value = 42;

        await root.LayoutRoot.SaveAsync(TestContext.Current.CancellationToken);
        root.Dispose();

        root = new LayoutRootViewModel("root", new JsonLayoutStore(_storageDirectory));
        child = new TestLayoutViewModel("child");
        root.Children.Add(child);
        nested = new TestLayoutViewModel("nested");
        child.Children.Add(nested);

        await root.LayoutRoot.LoadAsync(TestContext.Current.CancellationToken);

        Assert.Equal(10, child.Value);
        Assert.Equal(42, nested.Value);
        root.Dispose();
    }

    [Fact]
    public async ValueTask LoadAsync_DoesNotCallHandler_WhenLayoutWasNotSaved()
    {
        var root = new LayoutRootViewModel("root", new JsonLayoutStore(_storageDirectory));
        var child = new TestLayoutViewModel("child");
        root.Children.Add(child);

        await root.LayoutRoot.LoadAsync(TestContext.Current.CancellationToken);

        Assert.False(child.WasLoaded);
        root.Dispose();
    }

    [Fact]
    public async ValueTask SaveAsync_StoresLayoutsInSingleConfigurationFile()
    {
        var root = new LayoutRootViewModel("root", new JsonLayoutStore(_storageDirectory));
        var child = new TestLayoutViewModel("child");
        root.Children.Add(child);
        var nested = new TestLayoutViewModel("nested");
        child.Children.Add(nested);

        child.Value = 10;
        nested.Value = 42;

        await root.LayoutRoot.SaveAsync(TestContext.Current.CancellationToken);

        Assert.True(File.Exists(Path.Combine(_storageDirectory, "layout.json")));
        Assert.Empty(Directory.GetFiles(_storageDirectory, "*.layout.json"));
        root.Dispose();
    }

    public void Dispose()
    {
        if (Directory.Exists(_storageDirectory))
        {
            Directory.Delete(_storageDirectory, true);
        }
    }

    private sealed class LayoutRootViewModel : ViewModelBase
    {
        public LayoutRootViewModel(string id, ILayoutStore store)
            : base(id)
        {
            Children.SetParent<IViewModel, IViewModel>(this).AddTo(ref DisposableBag);
            Children.DisposeRemovedItems().AddTo(ref DisposableBag);
            LayoutRoot = new LayoutRootController<IViewModel>(this, store).AddTo(ref DisposableBag);
        }

        public ObservableList<IViewModel> Children { get; } = new();

        public LayoutRootController<IViewModel> LayoutRoot { get; }

        public override IEnumerable<IViewModel> GetChildren()
        {
            return Children;
        }
    }

    private sealed class TestLayoutViewModel : ViewModelBase, ISupportLayout<IViewModel>
    {
        private readonly ILayoutRegistration _handler;

        public TestLayoutViewModel(string id)
            : base(id)
        {
            Children.SetParent<IViewModel, IViewModel>(this).AddTo(ref DisposableBag);
            Children.DisposeRemovedItems().AddTo(ref DisposableBag);
            Layout = new LayoutController<IViewModel>(this).DisposeItWith(Disposable);

            _handler = Layout
                .Create<TestLayoutData>(
                    nameof(Value),
                    data =>
                    {
                        Value = data.Value;
                        WasLoaded = true;
                    },
                    data => data.Value = Value
                )
                .AddTo(ref DisposableBag);
            
        }

        public bool BoolProperty
        {
            get;
            set => SetField(ref field, value);
        }

        public ObservableList<IViewModel> Children { get; } = new();

        public ILayoutController Layout { get; }

        public int Value { get; set; }

        public bool WasLoaded { get; private set; }

        public override IEnumerable<IViewModel> GetChildren()
        {
            return Children;
        }
    }

    private sealed class TestLayoutData : IJsonLayoutData<TestLayoutData>
    {
        public static JsonTypeInfo<TestLayoutData> JsonTypeInfo =>
            TestLayoutJsonContext.Default.TestLayoutData;

        public int Value { get; set; }
    }

    [JsonSerializable(typeof(TestLayoutData))]
    private sealed partial class TestLayoutJsonContext : JsonSerializerContext;
}
