using Asv.Common;
using JetBrains.Annotations;
using ObservableCollections;
using R3;

namespace Asv.Modeling.Test;

[TestSubject(typeof(ISupportLayout))]
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

        SaveLayouts(root);
        Assert.True(
            SpinWait.SpinUntil(
                () => File.Exists(Path.Combine(_storageDirectory, "layout.json")),
                TimeSpan.FromSeconds(1)
            )
        );
        root.Dispose();

        root = new LayoutRootViewModel("root", new JsonLayoutStore(_storageDirectory));
        child = new TestLayoutViewModel("child");
        root.Children.Add(child);
        nested = new TestLayoutViewModel("nested");
        child.Children.Add(nested);

        await LoadLayoutsAsync(root, TestContext.Current.CancellationToken);

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

        await LoadLayoutsAsync(root, TestContext.Current.CancellationToken);

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

        SaveLayouts(root);

        Assert.True(
            SpinWait.SpinUntil(
                () => File.Exists(Path.Combine(_storageDirectory, "layout.json")),
                TimeSpan.FromSeconds(1)
            )
        );
        Assert.Empty(Directory.GetFiles(_storageDirectory, "*.layout.json"));
        root.Dispose();
    }

    private static void SaveLayouts(IViewModel current)
    {
        if (current is TestLayoutViewModel layout)
        {
            layout.SaveLayout();
        }

        foreach (var child in current.GetChildren())
        {
            SaveLayouts(child);
        }
    }

    private static async ValueTask LoadLayoutsAsync(IViewModel current, CancellationToken cancel)
    {
        cancel.ThrowIfCancellationRequested();

        if (current is TestLayoutViewModel layout)
        {
            await layout.LoadLayoutAsync(cancel);
        }

        foreach (var child in current.GetChildren())
        {
            await LoadLayoutsAsync(child, cancel);
        }
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
            LayoutManager = new LayoutManager<IViewModel>(this, store).AddTo(ref DisposableBag);
        }

        public ObservableList<IViewModel> Children { get; } = new();

        public LayoutManager<IViewModel> LayoutManager { get; }

        public override IEnumerable<IViewModel> GetChildren()
        {
            return Children;
        }
    }

    private sealed class TestLayoutViewModel : ViewModelBase, ISupportLayout
    {
        private readonly ILayoutSink<TestLayoutData> _handler;

        public TestLayoutViewModel(string id)
            : base(id)
        {
            Children.SetParent<IViewModel, IViewModel>(this).AddTo(ref DisposableBag);
            Children.DisposeRemovedItems().AddTo(ref DisposableBag);
            Layout = new LayoutController<IViewModel>(this).DisposeItWith(Disposable);

            _handler = Layout
                .Register<TestLayoutData>(
                    nameof(Value),
                    data =>
                    {
                        Value = data.Value;
                        WasLoaded = true;
                    }
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

        public void SaveLayout()
        {
            _handler.Save(new TestLayoutData { Value = Value });
        }

        public ValueTask LoadLayoutAsync(CancellationToken cancel)
        {
            return _handler.LoadAsync(cancel);
        }

        public override IEnumerable<IViewModel> GetChildren()
        {
            return Children;
        }
    }

    private sealed class TestLayoutData : ILayoutData
    {
        public int Value { get; set; }
    }
}
