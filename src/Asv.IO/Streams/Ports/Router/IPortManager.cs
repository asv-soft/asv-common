using System;
using System.Reactive;

namespace Asv.IO
{
    public interface IPortManager : IDataStream, IDisposable
    {
        IPortInfo[] Ports { get; }
        void Add(PortSettings settings);
        bool Remove(string portId);
        IObservable<Unit> OnConfigChanged { get; }
        void Load(PortManagerSettings settings);
        PortManagerSettings Save();
        void Enable(string portId);
        void Disable(string portId);
    }

    public interface IPortInfo
    {
        string Id { get; }
        PortSettings Settings { get; }
        long RxAcc { get; }
        long TxAcc { get; }
        PortType Type { get; }
        PortState State { get; }
        Exception LastException { get; }
        string Description { get; }
        string Status { get; }
    }

    public class PortSettings
    {
        public string Title { get; set; }
        public string ConnectionString { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class PortManagerSettings
    {
        public PortSettings[] Ports { get; set; } =
            new PortSettings[]
            {
                new()
                {
                    ConnectionString = "tcp://172.16.0.1:7341",
                    IsEnabled = true,
                    Title = "Base station",
                },
            };
    }
}
