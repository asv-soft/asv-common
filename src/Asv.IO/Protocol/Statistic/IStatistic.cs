using System;
using Asv.Common;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace Asv.IO;

public interface IStatistic
{
    uint RxBytes { get; }
    uint TxBytes { get; }
    uint RxMessages { get; }
    uint TxMessages { get; }
    uint RxError { get; }
    uint TxError { get; }
    uint DroppedRxMessages { get; }
    uint DroppedTxMessages { get; }
    uint ParsedBytes { get; }
    uint ParsedMessages { get; }
    uint UnknownMessages { get; }
    uint MessagePublishError { get; }
    uint BadCrcError { get; }
    uint DeserializeError { get; }
    uint MessageReadNotAllData { get; }
}

public static class StatisticHelper
{
    public static void PrintRx(this IStatistic src, ILogger logger)
    {
        logger.ZLogDebug(
            $"RX[msg:{src.RxMessages}, bytes:{src.RxBytes.BytesToString()}, err:{src.RxError}, dropped:{src.DroppedRxMessages}]");
    }

    public static void PrintTx(this IStatistic src, ILogger logger)
    {
        logger.ZLogDebug(
            $"TX[msg:{src.TxMessages}, bytes:{src.TxBytes.BytesToString()}, err:{src.TxError}, dropped:{src.DroppedTxMessages}]");
    }

    public static void PrintParsed(this IStatistic src, ILogger logger)
    {
        logger.ZLogDebug(
            $"Parsed[msg:{src.ParsedMessages}, bytes:{src.ParsedBytes.BytesToString()}, pub_err:{src.MessagePublishError}, unknown:{src.UnknownMessages}, crc:{src.BadCrcError}, deserialize:{src.DeserializeError}, read:{src.MessageReadNotAllData}]");
    }
}

public interface ISupportStatistic
{
    IStatistic Statistic { get; }
}