using System.Text.RegularExpressions;

namespace Asv.IO;

public static partial class ProtocolHelper
{

    internal static string NormalizeId(string id) => IdNormailizeRegex.Replace(id, "_");

    [GeneratedRegex(@"[^\w]")]
    private static partial Regex MyRegex();
    private static readonly Regex IdNormailizeRegex = MyRegex();

    
    
    
}