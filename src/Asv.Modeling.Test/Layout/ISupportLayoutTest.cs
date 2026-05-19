using System.Buffers;
using System.Buffers.Binary;
using Asv.Common;
using JetBrains.Annotations;
using ObservableCollections;
using R3;

namespace Asv.Modeling.Test;

[TestSubject(typeof(ISupportLayout<>))]
public class ISupportLayoutTest : IDisposable
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
        public TestLayoutViewModel(string id)
            : base(id)
        {
            Children.SetParent<IViewModel, IViewModel>(this).AddTo(ref DisposableBag);
            Children.DisposeRemovedItems().AddTo(ref DisposableBag);
            Layout = new LayoutController<IViewModel>(this).DisposeItWith(Disposable);
            Layout
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

        public ObservableList<IViewModel> Children { get; } = new();

        public ILayoutController Layout { get; }

        public int Value { get; set; }

        public bool WasLoaded { get; private set; }

        public override IEnumerable<IViewModel> GetChildren()
        {
            return Children;
        }
    }

    private sealed class TestLayoutData : ILayoutData
    {
        public int Value { get; set; }

        public void Serialize(IBufferWriter<byte> writer)
        {
            var span = writer.GetSpan(sizeof(int));
            BinaryPrimitives.WriteInt32LittleEndian(span, Value);
            writer.Advance(sizeof(int));
        }

        public void Deserialize(ReadOnlySequence<byte> data)
        {
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            data.CopyTo(buffer);
            Value = BinaryPrimitives.ReadInt32LittleEndian(buffer);
        }
    }
}
