using System;
using System.Collections.Immutable;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Asv.IO;

public static partial class ULog
{
    public static readonly Encoding Encoding = Encoding.UTF8;
    
    public static IULogReader CreateReader(ILogger? logger = null)
    {
        var builder = ImmutableDictionary.CreateBuilder<byte, Func<IULogToken>>();
        builder.Add(ULogFlagBitsMessageToken.TokenId, () => new ULogFlagBitsMessageToken());
        builder.Add(ULogFormatMessageToken.TokenId, () => new ULogFormatMessageToken());
        builder.Add(ULogParameterMessageToken.TokenId, () => new ULogParameterMessageToken());
        builder.Add(ULogDefaultParameterMessageToken.TokenId, () => new ULogDefaultParameterMessageToken());
        builder.Add(ULogInformationMessageToken.TokenId, () => new ULogInformationMessageToken());
        return new ULogReader(builder.ToImmutable(),logger);
    }

    public static ValueType GetSimpleValue(ULogType type, byte[] value)
    {
        switch (type)
        {
            case ULogType.Float:
                return BitConverter.ToSingle(value);
            case ULogType.Int32:
                return BitConverter.ToInt32(value);
            case ULogType.UInt32:
                return BitConverter.ToUInt32(value);
            case ULogType.Char:
                return BitConverter.ToChar(value);
            case ULogType.Int8:
                return (sbyte)value[0];
            case ULogType.UInt8:
                return value[0];
            case ULogType.Int16:
                return BitConverter.ToInt16(value);
            case ULogType.UInt16:
                return BitConverter.ToUInt16(value);
            case ULogType.Int64:
                return BitConverter.ToInt64(value);
            case ULogType.UInt64:
                return BitConverter.ToUInt64(value);
            case ULogType.Double:
                return BitConverter.ToDouble(value);
            case ULogType.Bool:
                return value[0] != 0;
            case ULogType.ReferenceType:
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }
}