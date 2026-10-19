using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Asv.IO
{
    [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Public configuration fields are retained for binary compatibility.")]
    public class TextReaderBaseConfig
    {
        public int MaxMessageSize = 1024;
        public char StartByte = '\n';
        public char StopByte = '\r';
        public readonly Encoding DefaultEncoding = Encoding.UTF8;
    }
}
