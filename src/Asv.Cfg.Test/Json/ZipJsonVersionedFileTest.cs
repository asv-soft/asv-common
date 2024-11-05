using System;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using Asv.Cfg;
using JetBrains.Annotations;
using Xunit;

namespace Asv.Cfg.Test;

[TestSubject(typeof(ZipJsonVersionedFile))]
public class ZipJsonVersionedFileTest:ConfigurationBaseTest<ZipJsonVersionedFile>
{
   
    
    protected override IDisposable CreateForTest(out ZipJsonVersionedFile configuration)
    {
        throw new NotImplementedException();        
    }
}