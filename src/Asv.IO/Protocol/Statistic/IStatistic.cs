using System;
using System.Buffers;
using Asv.Common;
using DotNext.Buffers;
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
            $"RX[msg:{src.RxMessages}, bytes:{src.RxBytes.BytesToString()}, err:{src.RxError}, dropped:{src.DroppedRxMessages}]"
        );
    }

    public static void PrintTx(this IStatistic src, ILogger logger)
    {
        logger.ZLogDebug(
            $"TX[msg:{src.TxMessages}, bytes:{src.TxBytes.BytesToString()}, err:{src.TxError}, dropped:{src.DroppedTxMessages}]"
        );
    }

    public static void PrintParsed(this IStatistic src, ILogger logger)
    {
        logger.ZLogDebug(
            $"Parsed[msg:{src.ParsedMessages}, bytes:{src.ParsedBytes.BytesToString()}, pub_err:{src.MessagePublishError}, unknown:{src.UnknownMessages}, crc:{src.BadCrcError}, deserialize:{src.DeserializeError}, read:{src.MessageReadNotAllData}]"
        );
    }

    public static string PrintTable(this IStatistic stat)
    {
        using var writer = new PoolingArrayBufferWriter<char>(ArrayPool<char>.Shared);
        const int nameWidth = 24;

        void AddLine(string name, uint value)
        {
            var line = $"{name, -nameWidth} {value}\n";
            writer.Write(line.AsSpan());
        }

        AddLine(nameof(stat.RxBytes), stat.RxBytes);
        AddLine(nameof(stat.TxBytes), stat.TxBytes);
        AddLine(nameof(stat.RxMessages), stat.RxMessages);
        AddLine(nameof(stat.TxMessages), stat.TxMessages);
        AddLine(nameof(stat.RxError), stat.RxError);
        AddLine(nameof(stat.TxError), stat.TxError);
        AddLine(nameof(stat.DroppedRxMessages), stat.DroppedRxMessages);
        AddLine(nameof(stat.DroppedTxMessages), stat.DroppedTxMessages);
        AddLine(nameof(stat.ParsedBytes), stat.ParsedBytes);
        AddLine(nameof(stat.ParsedMessages), stat.ParsedMessages);
        AddLine(nameof(stat.UnknownMessages), stat.UnknownMessages);
        AddLine(nameof(stat.MessagePublishError), stat.MessagePublishError);
        AddLine(nameof(stat.BadCrcError), stat.BadCrcError);
        AddLine(nameof(stat.DeserializeError), stat.DeserializeError);
        AddLine(nameof(stat.MessageReadNotAllData), stat.MessageReadNotAllData);

        return writer.ToString();
    }
}

public interface ISupportStatistic
{
    IStatistic Statistic { get; }
}
