using System;
using JetBrains.Annotations;
using R3;
using Xunit.Abstractions;

namespace Asv.Cfg.Test
{
    [TestSubject(typeof(InMemoryConfiguration))]
    public class InMemoryConfigurationTest(ITestOutputHelper log) : ConfigurationBaseTest<InMemoryConfiguration>(log)
    {

        protected override IDisposable CreateForTest(out InMemoryConfiguration configuration)
        {
            configuration = new InMemoryConfiguration(LogFactory.CreateLogger("IM_MEMORY"));
            var cfg = configuration;
            return Disposable.Create(() => {cfg.Dispose(); });
        }
    }
}