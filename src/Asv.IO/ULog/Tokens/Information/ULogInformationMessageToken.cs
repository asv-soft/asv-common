using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Asv.IO;

/// <summary>
/// 'I': Information Message
///
/// The Information message defines a dictionary type definition key : value pair for any information,
/// including but not limited to Hardware version, Software version, Build toolchain for the software, etc.
/// </summary>
public class ULogInformationMessageToken : KeyValueTokenBase
{
    public static ULogToken Token => ULogToken.Information;
    public const string TokenName = "Information";
    public const byte TokenId = (byte)'I';

    public override string Name => TokenName;
    public override ULogToken Type => Token;
    public override TokenPlaceFlags Section => TokenPlaceFlags.Definition | TokenPlaceFlags.Data;
    
}
