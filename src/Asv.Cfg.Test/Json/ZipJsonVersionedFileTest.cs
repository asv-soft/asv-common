using System;
using JetBrains.Annotations;

namespace Asv.Cfg.Test;

[TestSubject(typeof(ZipJsonVersionedFile))]
public class ZipJsonVersionedFileTest:ConfigurationBaseTest<ZipJsonVersionedFile>
{
   
    
    protected override IDisposable CreateForTest(out ZipJsonVersionedFile configuration)
    {
        throw new NotImplementedException();        
    }
}