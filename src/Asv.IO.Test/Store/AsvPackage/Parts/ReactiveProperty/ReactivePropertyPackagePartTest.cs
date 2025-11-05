using System;
using System.IO;
using System.IO.Packaging;
using System.Threading;
using Asv.Cfg.Test;
using Asv.Common;
using Asv.IO;
using JetBrains.Annotations;
using Xunit;
using Xunit.Abstractions;

namespace Asv.IO.Test.AsvPackage.Parts.ReactiveProperty;

[TestSubject(typeof(ReactivePropertyPackagePart))]
public class ReactivePropertyPackagePartTest(ITestOutputHelper log)
{
    private static readonly Uri PartUri = new Uri("/meta/kvs.json", UriKind.Relative);
    private const string ContentType = "application/json";

    [Fact]
    public void Save_Twice_OverwritesAndWarns()
    {
        var ms = new MemoryStream();
        var pkg = Package.Open(ms, FileMode.Create, FileAccess.ReadWrite);
        var logger = new TestLogger(log, TimeProvider.System, "AsvFilePartTest");
        var ctx = new AsvPackageContext(new Lock(), pkg, logger);
        var part = new ReactivePropertyPackagePart(
            PartUri,
            ContentType,
            CompressionOption.Normal,
            ctx
        );

        var prop1 = part.AddGeoPoint("prop1", GeoPoint.Zero);
        var prop2 = part.AddDouble("prop2", double.NaN);
        var prop3 = part.AddString("prop3", "value1");
        var prop4 = part.AddInt32("prop4", int.MaxValue);
        var prop5 = part.AddInt64("prop5", long.MaxValue);

        part.Load();

        Assert.False(part.HasChanges.CurrentValue);
        var value1 = prop1.Value = GeoPoint.Random();
        Assert.True(part.HasChanges.CurrentValue);
        var value2 = prop2.Value = Random.Shared.NextDouble();
        Assert.True(part.HasChanges.CurrentValue);
        var value3 = prop3.Value = "value2";
        Assert.True(part.HasChanges.CurrentValue);
        var value4 = prop4.Value = Random.Shared.Next();
        Assert.True(part.HasChanges.CurrentValue);
        var value5 = prop5.Value = Random.Shared.NextInt64();
        Assert.True(part.HasChanges.CurrentValue);

        part.Dispose();
        Assert.False(part.HasChanges.CurrentValue);
        pkg.Close();

        // Reopen the package for reading
        ms.Position = 0;
        pkg = Package.Open(ms, FileMode.Open, FileAccess.Read);
        ctx = new AsvPackageContext(new Lock(), pkg, logger);
        part = new ReactivePropertyPackagePart(
            PartUri,
            ContentType,
            CompressionOption.Maximum,
            ctx
        );
        part.Load();

        Assert.Equal(value1, prop1.Value);
        Assert.Equal(value2, prop2.Value);
        Assert.Equal(value3, prop3.Value);
        Assert.Equal(value4, prop4.Value);
        Assert.Equal(value5, prop5.Value);
    }
}
