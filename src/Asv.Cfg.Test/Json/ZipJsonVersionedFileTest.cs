using System;
using System.IO;
using Asv.Common;
using JetBrains.Annotations;
using R3;
using Xunit.Abstractions;

namespace Asv.Cfg.Test;

[TestSubject(typeof(ZipJsonVersionedFile))]
public class ZipJsonVersionedFileTest(ITestOutputHelper log):ConfigurationBaseTest<ZipJsonVersionedFile>(log)
{
   
    
    protected override IDisposable CreateForTest(out ZipJsonVersionedFile configuration)
    {
        configuration = new ZipJsonVersionedFile(new MemoryStream(), new SemVersion(1,0),"test",true, false, LogFactory.CreateLogger("ZIP_VERSION_JSON_CONFIG"));
        return Disposable.Empty;        
    }
}