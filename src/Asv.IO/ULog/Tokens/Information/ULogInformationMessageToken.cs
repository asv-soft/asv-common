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
public class ULogInformationMessageToken : ULogKeyAndValueTokenBase
{
    #region Static

    public static ULogToken Type => ULogToken.Information;
    public const string Name = "Information";
    public const byte TokenId = (byte)'I';

    #endregion

    public override string TokenName => Name;
    public override ULogToken TokenType => Type;
    public override TokenPlaceFlags TokenSection => TokenPlaceFlags.Definition | TokenPlaceFlags.Data;
    
}
