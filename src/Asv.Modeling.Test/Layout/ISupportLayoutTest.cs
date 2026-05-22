using System.ComponentModel;
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
        var root = new LayoutRootViewModel("root", new JsonTokenLayoutStore(_storageDirectory));
        var child = new TestLayoutViewModel("child");
        root.Children.Add(child);
        var nested = new TestLayoutViewModel("nested");
        child.Children.Add(nested);

        child.Value = 10;
        nested.Value = 42;

        await SaveLayoutsAsync(root, TestContext.Current.CancellationToken);
        Assert.True(
            SpinWait.SpinUntil(
                () => HasSavedLayout(root, child, 10) && HasSavedLayout(root, nested, 42),
                TimeSpan.FromSeconds(1)
            )
        );
        Assert.False(File.Exists(Path.Combine(_storageDirectory, "layout.json")));
        root.Dispose();
        Assert.True(File.Exists(Path.Combine(_storageDirectory, "layout.json")));

        root = new LayoutRootViewModel("root", new JsonTokenLayoutStore(_storageDirectory));
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
        var root = new LayoutRootViewModel("root", new JsonTokenLayoutStore(_storageDirectory));
        var child = new TestLayoutViewModel("child");
        root.Children.Add(child);

        await LoadLayoutsAsync(root, TestContext.Current.CancellationToken);

        Assert.False(child.WasLoaded);
        root.Dispose();
    }

    [Fact]
    public async ValueTask Flush_StoresLayoutsInSingleJsonTokenFile()
    {
        var root = new LayoutRootViewModel("root", new JsonTokenLayoutStore(_storageDirectory));
        var child = new TestLayoutViewModel("child");
        root.Children.Add(child);
        var nested = new TestLayoutViewModel("nested");
        child.Children.Add(nested);

        child.Value = 10;
        nested.Value = 42;

        await SaveLayoutsAsync(root, TestContext.Current.CancellationToken);
        Assert.True(
            SpinWait.SpinUntil(
                () => HasSavedLayout(root, child, 10) && HasSavedLayout(root, nested, 42),
                TimeSpan.FromSeconds(1)
            )
        );
        Assert.False(File.Exists(Path.Combine(_storageDirectory, "layout.json")));

        root.LayoutManager.Store.Flush();

        Assert.True(File.Exists(Path.Combine(_storageDirectory, "layout.json")));
        root.Dispose();
    }

    [Fact]
    public async ValueTask Flush_StoresHumanReadableJson()
    {
        var root = new LayoutRootViewModel("root", new JsonTokenLayoutStore(_storageDirectory));
        var child = new TestLayoutViewModel("child");
        root.Children.Add(child);
        child.Value = 10;

        await SaveLayoutsAsync(root, TestContext.Current.CancellationToken);
        Assert.True(
            SpinWait.SpinUntil(() => HasSavedLayout(root, child, 10), TimeSpan.FromSeconds(1))
        );

        root.LayoutManager.Store.Flush();

        var savedLayout = File.ReadAllText(Path.Combine(_storageDirectory, "layout.json"));
        Assert.StartsWith("[", savedLayout.TrimStart());
        Assert.Contains(Environment.NewLine, savedLayout);
        Assert.Contains("  {", savedLayout);
        Assert.Contains("\"Path\"", savedLayout);
        Assert.Contains("\"LayoutId\"", savedLayout);
        Assert.Contains("\"Data\"", savedLayout);
        Assert.Contains("\"Value\": 10", savedLayout);
        Assert.DoesNotContain("\"layout_", savedLayout);
        root.Dispose();
    }

    [Fact]
    public async ValueTask Save_FlushesLayoutsByInterval()
    {
        var root = new LayoutRootViewModel(
            "root",
            new JsonTokenLayoutStore(
                _storageDirectory,
                flushInterval: TimeSpan.FromMilliseconds(20)
            )
        );
        var child = new TestLayoutViewModel("child");
        root.Children.Add(child);
        child.Value = 10;

        await SaveLayoutsAsync(root, TestContext.Current.CancellationToken);

        Assert.True(
            SpinWait.SpinUntil(
                () => File.Exists(Path.Combine(_storageDirectory, "layout.json")),
                TimeSpan.FromSeconds(1)
            )
        );
        root.Dispose();
    }

    [Fact]
    public void LoadWhenRootAttached_LoadsRegisteredLayouts_WhenParentAssigned()
    {
        using var store = new InMemoryLayoutStore();
        using var root = new TrackingLayoutRootNode("root", store);
        using var child = new TrackingLayoutNode("child");
        store.Save(
            new NavPath(child.Id),
            nameof(TrackingLayoutNode.Value),
            new TestLayoutData { Value = 42 }
        );

        root.AddChild(child);

        Assert.True(SpinWait.SpinUntil(() => child.WasLoaded, TimeSpan.FromSeconds(1)));
        Assert.Equal(42, child.Value);
    }

    private static bool HasSavedLayout(
        LayoutRootViewModel root,
        IViewModel viewModel,
        int expectedValue
    )
    {
        var path = new NavPath(viewModel.GetPathFrom<IViewModel, NavId>(root));
        return root.LayoutManager.Store.TryLoad<TestLayoutData>(
                path,
                nameof(TestLayoutViewModel.Value),
                out var data
            )
            && data.Value == expectedValue;
    }

    private static async ValueTask SaveLayoutsAsync(IViewModel current, CancellationToken cancel)
    {
        cancel.ThrowIfCancellationRequested();

        if (current is TestLayoutViewModel layout)
        {
            await layout.SaveLayoutAsync(cancel);
        }

        foreach (var child in current.GetChildren())
        {
            await SaveLayoutsAsync(child, cancel);
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

        public ValueTask SaveLayoutAsync(CancellationToken cancel)
        {
            return _handler.SaveAsync(new TestLayoutData { Value = Value }, cancel);
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

    private sealed class TestLayoutData
    {
        public int Value { get; set; }
    }

    private sealed class InMemoryLayoutStore : ILayoutStore
    {
        private readonly Dictionary<(NavPath Path, string LayoutId), object> _layouts = new();

        public bool TryLoad<TData>(NavPath path, string layoutId, out TData layoutData)
        {
            if (_layouts.TryGetValue((path, layoutId), out var data) && data is TData typedData)
            {
                layoutData = typedData;
                return true;
            }

            layoutData = default!;
            return false;
        }

        public void Save<TData>(NavPath path, string layoutId, TData layoutData)
        {
            _layouts[(path, layoutId)] = layoutData!;
        }

        public void Flush() { }

        public void Dispose() { }
    }

    private class TrackingLayoutNode
        : ISupportParentChange<TrackingLayoutNode>,
            ISupportRoutedEvents<TrackingLayoutNode>,
            ISupportNavigation<TrackingLayoutNode>,
            ISupportRootTracking<TrackingLayoutNode, TrackingLayoutRootNode>,
            ISupportLayout,
            INotifyPropertyChanged,
            IDisposable
    {
        private readonly List<TrackingLayoutNode> _children = [];
        private readonly Subject<TrackingLayoutNode?> _parentChanged = new();
        private readonly RoutedEventController<TrackingLayoutNode> _events;
        private readonly RootTrackingController<
            TrackingLayoutNode,
            TrackingLayoutRootNode
        > _rootTracking;
        private readonly ILayoutSink<TestLayoutData> _handler;
        private readonly CompositeDisposable _dispose = new();

        public TrackingLayoutNode(string id)
        {
            Id = new NavId(id);
            _events = new RoutedEventController<TrackingLayoutNode>(this);
            _rootTracking = new RootTrackingController<TrackingLayoutNode, TrackingLayoutRootNode>(
                this
            );
            Layout = new LayoutController<TrackingLayoutNode>(this);
            _handler = Layout.Register<TestLayoutData>(
                nameof(Value),
                data =>
                {
                    Value = data.Value;
                    WasLoaded = true;
                }
            );
            Layout.LoadWhenRootAttached(RootTracking).DisposeItWith(_dispose);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public IRoutedEventController<TrackingLayoutNode> Events => _events;

        public NavId Id { get; }

        public TrackingLayoutNode? Parent { get; private set; }

        public Observable<TrackingLayoutNode?> ParentChanged => _parentChanged;

        public IRootTrackingController<TrackingLayoutRootNode> RootTracking => _rootTracking;

        public ILayoutController Layout { get; }

        public int Value { get; private set; }

        public bool WasLoaded { get; private set; }

        public IEnumerable<TrackingLayoutNode> GetChildren()
        {
            return _children;
        }

        public ValueTask<TrackingLayoutNode> Navigate(NavId id)
        {
            return ValueTask.FromResult(_children.FirstOrDefault(x => x.Id == id) ?? this);
        }

        public void AddChild(TrackingLayoutNode child)
        {
            ArgumentNullException.ThrowIfNull(child);
            _children.Add(child);
            child.SetParent(this);
        }

        public void SetParent(TrackingLayoutNode? parent)
        {
            if (ReferenceEquals(Parent, parent))
            {
                return;
            }

            Parent = parent;
            _parentChanged.OnNext(parent);
        }

        public void Dispose()
        {
            _handler.Dispose();
            Layout.Dispose();
            _rootTracking.Dispose();
            _events.Dispose();
            _parentChanged.Dispose();
            _dispose.Dispose();
            PropertyChanged = null;
        }
    }

    private sealed class TrackingLayoutRootNode : TrackingLayoutNode
    {
        public TrackingLayoutRootNode(string id, ILayoutStore store)
            : base(id)
        {
            LayoutManager = new LayoutManager<TrackingLayoutNode>(this, store);
        }

        public LayoutManager<TrackingLayoutNode> LayoutManager { get; }

        public new void Dispose()
        {
            LayoutManager.Dispose();
            base.Dispose();
        }
    }
}
