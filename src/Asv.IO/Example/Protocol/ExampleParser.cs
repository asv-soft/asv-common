using System;
using System.Buffers;
using System.Threading.Tasks;

namespace Asv.IO;

public class ExampleParser : ProtocolParser<ExampleMessageBase, byte>
{
    private const int MaxMessageSize = byte.MaxValue + 5;
    public const byte SyncByte = 0x0A;

    private State _state = State.Sync;
    private readonly ArrayPool<byte> _pool;
    private readonly byte[] _buffer;
    private byte _size;
    private int _read;

    public ExampleParser(
        IProtocolMessageFactory<ExampleMessageBase, byte> messageFactory,
        IProtocolContext context,
        IStatisticHandler? statisticHandler
    )
        : this(messageFactory, context, statisticHandler, ArrayPool<byte>.Shared) { }

    internal ExampleParser(
        IProtocolMessageFactory<ExampleMessageBase, byte> messageFactory,
        IProtocolContext context,
        IStatisticHandler? statisticHandler,
        ArrayPool<byte> pool
    )
        : base(messageFactory, context, statisticHandler)
    {
        _pool = pool;
        _buffer = pool.Rent(MaxMessageSize);
    }

    public override ProtocolInfo Info => ExampleProtocol.Info;

    private enum State
    {
        Sync,
        SenderId,
        MessageId,
        Size,
        MessageData,
        Crc,
    }

    public override bool Push(byte data)
    {
        ThrowIfDisposed();

        // [SYNC:1][SENDER_ID:1][MSG_ID:1][PAYLOAD_SIZE:1][PAYLOAD:SIZE][CRC:1, RANGE=1..^1]
        switch (_state)
        {
            case State.Sync:
                if (data == SyncByte)
                {
                    _buffer[0] = data;
                    _state = State.SenderId;
                }
                return false;
            case State.SenderId:
                _buffer[1] = data;
                _state = State.MessageId;
                return false;
            case State.MessageId:
                _buffer[2] = data;
                _state = State.Size;
                return false;
            case State.Size:
                _buffer[3] = data;
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
                _buffer[4 + _read] = data;
                _read++;
                if (_size == _read)
                {
                    _state = State.Crc;
                }
                return false;
            case State.Crc:
                _buffer[4 + _size] = data;
                _state = State.Sync;
                try
                {
                    var span = new ReadOnlySpan<byte>(_buffer, 0, _size + 5);
                    InternalParsePacket(
                        _buffer[2], /*MSG_ID*/
                        ref span,
                        false
                    );
                    return true;
                }
                catch (ProtocolParserException ex)
                {
                    InternalOnError(ex);
                    return false;
                }
                catch (Exception ex)
                {
                    InternalOnError(new ProtocolParserException(Info, "Parser ", ex));
                    return false;
                }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public override void Reset()
    {
        ThrowIfDisposed();
        _state = State.Sync;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _pool.Return(_buffer);
        }
        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        _pool.Return(_buffer);
        await base.DisposeAsyncCore().ConfigureAwait(false);
    }
}
