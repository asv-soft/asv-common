using System;
using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace Asv.IO;

public static partial class ULog
{
    public static readonly Encoding Encoding = Encoding.UTF8;
    
    
   
    

    

    
    public static IULogReader CreateReader(ILogger? logger = null)
    {
        var builder = ImmutableDictionary.CreateBuilder<byte, Func<IULogToken>>();
        builder.Add(ULogFlagBitsMessageToken.TokenId, () => new ULogFlagBitsMessageToken());
        builder.Add(ULogFormatMessageToken.TokenId, () => new ULogFormatMessageToken());
        builder.Add(ULogParameterMessageToken.TokenId, () => new ULogParameterMessageToken()); //TODO delete before pr
        builder.Add(ULogInformationMessageToken.TokenId, () => new ULogInformationMessageToken());
        builder.Add(ULogMultiInformationMessageToken.TokenId, () => new ULogMultiInformationMessageToken());
        return new ULogReader(builder.ToImmutable(),logger);
    }


   
    
    

    
    
    
}

