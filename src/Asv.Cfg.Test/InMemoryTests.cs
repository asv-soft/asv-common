using System;
using System.Reactive.Disposables;
using Xunit.Abstractions;

namespace Asv.Cfg.Test
{
    public class InMemoryTests:ConfigurationTestBase<InMemoryConfiguration>
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public InMemoryTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        protected override IDisposable CreateForTest(out InMemoryConfiguration configuration)
        {
            configuration = new InMemoryConfiguration();
            var cfg = configuration;
            return Disposable.Create(() => {cfg.Dispose(); });
        }
    }
}