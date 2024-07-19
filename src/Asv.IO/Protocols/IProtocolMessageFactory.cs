using System;
using System.Collections.Generic;

namespace Asv.IO;

public interface IProtocolMessageFactory<TMsgId>:IReadOnlyDictionary<TMsgId,Func<IProtocolMessage>>
{
    
}