using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Asv.Common;

namespace Asv.IO;

/// <summary>
/// Encoder for a sequence of timestamps (<see cref="long"/>) using the Facebook Gorilla scheme.
/// </summary>
/// <remarks>
/// Bit layout (Gorilla-compatible):
/// <list type="number">
///   <item><description><b>t0</b>: absolute timestamp, 64 bits (written verbatim).</description></item>
///   <item><description><b>Δ1</b>: first delta (<c>t1 - t0</c>). By default, encoded as a 27-bit two's-complement value;
///   can be switched to a full 64-bit field via <paramref name="firstDelta27Bits"/> = <c>false</c>.</description></item>
///   <item><description><b>Next values</b>: encode <c>DoD = Δ - Δ(prev)</c> with a prefix:
///     <code>
///       '0'                  → DoD = 0
///       '10'                 → 7-bit  two's complement  (range: -64  … +63)
///       '110'                → 9-bit  two's complement  (range: -256 … +255)
///       '1110'               → 12-bit two's complement  (range: -2048… +2047)
///       '1111'               → 32-bit raw field (lower 32 bits of DoD are written)
///     </code>
///     Two's-complement packing is performed by masking to the target width.
///   </description></item>
/// </list>
/// </remarks>
public sealed class GorillaTimestampEncoder
    : AsyncDisposableOnce, IBitEncoder<long>
{
    private readonly IBitWriter _bw;
    private readonly bool _firstDelta27Bits;
    private readonly bool _leaveOpen;

    // Encoder state machine:
    // 0: expect t0 (absolute)
    // 1: expect t1 → write Δ1
    // 2: subsequent timestamps → write DoD-prefixed deltas
    private int _state;
    private long _prevTs;
    private long _prevDelta;

    /// <summary>
    /// Gets total number of bits written to the underlying <see cref="IBitWriter"/>.
    /// </summary>
    public long TotalBitsWritten => _bw.TotalBitsWritten;

    /// <summary>
    /// Initializes a new instance of the <see cref="GorillaTimestampEncoder"/> class.
    /// </summary>
    /// <param name="output">Destination bit writer.</param>
    /// <param name="firstDelta27Bits">
    /// If <see langword="true"/> (default), the first delta Δ1 is encoded as a 27-bit two's-complement value.
    /// If <see langword="false"/>, Δ1 is encoded as a full 64-bit field.
    /// </param>
    /// <param name="leaveOpen">
    /// If <see langword="true"/>, the <paramref name="output"/> is not disposed when this encoder is disposed.
    /// </param>
    public GorillaTimestampEncoder(IBitWriter output, bool firstDelta27Bits = true, bool leaveOpen = false)
    {
        _bw = output;
        _firstDelta27Bits = firstDelta27Bits;
        _leaveOpen = leaveOpen;
        _state = 0;
    }

    /// <summary>
    /// Appends a timestamp to the encoded stream.
    /// </summary>
    /// <param name="ts">Timestamp to encode.</param>
    public void Add(long ts)
    {
        switch (_state)
        {
            case 0:
                // Absolute t0: 64 bits verbatim.
                _bw.WriteBits((ulong)ts, 64);
                _prevTs = ts;
                _state = 1;
                return;

            case 1:
            {
                var delta = ts - _prevTs;
                if (_firstDelta27Bits)
                {
                    // Δ1: 27-bit two's complement.
                    _bw.WriteBits(EncodeSigned(delta, 27), 27);
                }
                else
                {
                    // Δ1: full 64-bit field (raw bit pattern of 'delta').
                    _bw.WriteBits((ulong)delta, 64);
                }

                _prevDelta = delta;
                _prevTs = ts;
                _state = 2;
                return;
            }

            default:
            {
                var delta = ts - _prevTs;
                var dod = delta - _prevDelta;

                switch (dod)
                {
                    case 0:
                        _bw.WriteBit(0); // '0'
                        break;
                    case >= -64 and <= 63:
                        _bw.WriteBits(0b10, 2);                   // '10'
                        _bw.WriteBits(EncodeSigned(dod, 7), 7);   // 7-bit two's complement
                        break;
                    case >= -256 and <= 255:
                        _bw.WriteBits(0b110, 3);                  // '110'
                        _bw.WriteBits(EncodeSigned(dod, 9), 9);   // 9-bit two's complement
                        break;
                    case >= -2048 and <= 2047:
                        _bw.WriteBits(0b1110, 4);                 // '1110'
                        _bw.WriteBits(EncodeSigned(dod, 12), 12); // 12-bit two's complement
                        break;
                    default:
                        _bw.WriteBits(0b1111, 4);                 // '1111'
                        
                        // Write lower 32 bits of DoD (unchecked truncation).
                        _bw.WriteBits((ulong)dod, 32);
                        break;
                }

                _prevDelta = delta;
                _prevTs = ts;
                return;
            }
        }
    }

    /// <summary>
    /// Packs a signed <paramref name="v"/> into a fixed-width two's-complement field.
    /// </summary>
    /// <param name="v">Signed value to encode.</param>
    /// <param name="width">Target width in bits (1..64).</param>
    /// <returns>Unsigned representation containing the lower <paramref name="width"/> bits of <paramref name="v"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ulong EncodeSigned(long v, int width)
    {
        // Two's-complement narrowing to the specified width:
        // keep the lower 'width' bits (mask).
        return (ulong)v & (width == 64 ? ulong.MaxValue : ((1UL << width) - 1));
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_leaveOpen)
        {
            _bw.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        if (!_leaveOpen)
        {
            await _bw.DisposeAsync();
        }
        await base.DisposeAsyncCore();
    }
}
