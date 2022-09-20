using System;
using System.IO.Ports;

namespace Asv.IO
{
    public class SerialPortConfig
    {
        public int DataBits { get; set; } = 8;
        public int BoundRate { get; set; } = 115200;
        public Parity Parity { get; set; } = Parity.None;
        public StopBits StopBits { get; set; } = StopBits.One;
        public string PortName { get; set; }
        public int WriteTimeout { get; set; } = 200;
        public int WriteBufferSize { get; set; } = 40960;

        public static bool TryParseFromUri(Uri uri, out SerialPortConfig opt)
        {
            if (!"serial".Equals(uri.Scheme, StringComparison.InvariantCultureIgnoreCase))
            {
                opt = null;
                return false;
            }

            var coll = PortFactory.ParseQueryString(uri.Query);
            opt = new SerialPortConfig
            {
                PortName = uri.LocalPath,
                WriteTimeout = int.Parse(coll["wrt"] ?? "1000"),
                BoundRate = int.Parse(coll["br"] ?? "57600"),
                WriteBufferSize = int.Parse(coll["ws"] ?? "40960"),
                Parity = (Parity)Enum.Parse(typeof(Parity), coll["parity"] ?? Parity.None.ToString()),
                DataBits = int.Parse(coll["dataBits"] ?? "8"),
                StopBits = (StopBits)Enum.Parse(typeof(StopBits), coll["stopBits"] ?? StopBits.One.ToString()),
            };
            return true;
        }

        public override string ToString()
        {
            return $"Serial {PortName} ({BoundRate})";
            //return $"serial:{PortName}?br={BoundRate}&wrt={WriteTimeout}&parity={Parity}&dataBits={DataBits}&stopBits={StopBits}";
        }
    }
}
