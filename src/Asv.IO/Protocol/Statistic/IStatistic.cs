namespace Asv.IO;

public interface IStatistic
{
    uint RxBytes { get; }
    uint TxBytes { get; }
    uint RxMessages { get; }
    uint TxMessages { get; }
    uint RxError { get; }
    uint TxError { get; }
    uint ParsedBytes { get; }
    uint ParsedMessages { get; }
    uint UnknownMessages { get; }
    uint MessagePublishError { get; }
    uint BadCrcError { get; }
    uint DeserializeError { get; }
    uint MessageReadNotAllData { get; }
}