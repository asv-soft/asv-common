using System;
using System.Reactive.Disposables;
using JetBrains.Annotations;
using Xunit.Abstractions;

namespace Asv.Cfg.Test
{
    [TestSubject(typeof(InMemoryConfiguration))]
    public class InMemoryConfigurationTest(ITestOutputHelper log) : ConfigurationBaseTest<InMemoryConfiguration>
    {
        private readonly ITestOutputHelper _log = log;

        protected override IDisposable CreateForTest(out InMemoryConfiguration configuration)
        {
            configuration = new InMemoryConfiguration(new TestLogger(_log,TimeProvider.System, "IM_MEMORY"));
            var cfg = configuration;
            return Disposable.Create(() => {cfg.Dispose(); });
        }
    }
}