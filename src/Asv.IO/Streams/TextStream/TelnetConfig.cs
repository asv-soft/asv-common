using System.Text;

namespace Asv.IO
{
    public class TelnetConfig
    {
        public string ConnectionString = "tcp://10.10.4.78:23";
        public int MaxMessageSize = 10*1024;
        public readonly Encoding DefaultEncoding = Encoding.ASCII;
    }
}