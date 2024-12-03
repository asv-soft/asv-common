using System;
using System.Buffers;

namespace Asv.IO;

public class ExampleParser(IProtocolMessageFactory<ExampleMessageBase,byte> messageFactory, IProtocolContext context, IStatisticHandler? statisticHandler)
    : ProtocolParser<ExampleMessageBase, byte>(messageFactory,context,statisticHandler)
{
    private const int MaxMessageSize = 255;
    public const byte SyncByte = 0x0A;
    
    private State _state = State.Sync;
    private readonly byte[] _buffer = ArrayPool<byte>.Shared.Rent(MaxMessageSize);
    private byte _size;
    private int _read;

    public override ProtocolInfo Info => ExampleProtocol.Info;
    private enum State
    {
        Sync,
        MessageId,
        Size,
        MessageData,
        Crc
    }
    public override bool Push(byte data)
    {
        switch (_state)
        {
            case State.Sync:
                if (data == SyncByte)
                {
                    _buffer[0] = data;
                    _state = State.MessageId;
                }
                return false;
            case State.MessageId:
                _buffer[1] = data;
                _state = State.Size;
                return false;
            case State.Size:
                _buffer[2] = data;
                _size = data;
                _read = 0;
                if (_size == 0)
                {
                    _state = State.Crc;
                }
                else
                {
                    _state = State.MessageData;
                }
                return false;
            case State.MessageData:
                _buffer[3 + _read] = data;
                _read++;
                if (_size == _read)
                {
                    _state = State.Crc;
                }
                return false;
            case State.Crc:
                _buffer[3 + _size] = data;
                _state = State.Sync;
                try
                {
                    var span = new ReadOnlySpan<byte>(_buffer, 0, _size + 4);
                    InternalParsePacket(_buffer[1], ref span, false);
                    return true;
                }
                catch (ProtocolParserException ex)
                {
                    InternalOnError(ex);
                    return false;
                }
                catch (Exception ex)
                {
                    InternalOnError(new ProtocolParserException(Info,"Parser ",ex));
                    return false;
                }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    

    public override void Reset()
    {
        _state = State.Sync;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            ArrayPool<byte>.Shared.Return(_buffer);
        }
        base.Dispose(disposing);
    }
}