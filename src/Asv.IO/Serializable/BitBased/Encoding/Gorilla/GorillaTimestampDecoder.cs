using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Asv.Common;

namespace Asv.IO;

/// <summary>
/// Timestamp decoder (Int64) implementing the Facebook Gorilla time-series scheme.
/// </summary>
/// <remarks>
/// The stream is expected to contain:
/// <list type="number">
///   <item><description><c>t0</c>: the initial timestamp encoded as an unsigned 64-bit integer.</description></item>
///   <item><description><c>Δ1</c>: the first delta (<c>t1 - t0</c>). Depending on <see cref="_firstDelta27Bits"/>,
///   it is read either as a signed 27-bit value (Gorilla-compatible) or a full 64-bit unsigned value.</description></item>
///   <item><description>Subsequent deltas are encoded as <c>Δ = Δ(prev) + DoD</c>, where <c>DoD</c> (delta-of-delta)
///   is prefix-coded with the following bit widths:
///   <para>
///     Prefix tree:
///     <code>
///       '0'                  -> DoD = 0
///       '10'                 -> DoD is signed 7  bits
///       '110'                -> DoD is signed 9  bits
///       '1110'               -> DoD is signed 12 bits
///       '1111'               -> DoD is unsigned 32 bits
///     </code>
///   </para>
///   </description></item>
/// </list>
/// All signed fields are sign-extended to 64-bit integers using two’s complement.
/// </remarks>
public sealed class GorillaTimestampDecoder : AsyncDisposableOnce, IBitDecoder<long>
{
    private readonly IBitReader _br;
    private readonly bool _firstDelta27Bits;
    private readonly bool _leaveOpen;

    // Decoder state machine:
    // 0: read t0
    // 1: read Δ1
    // 2: read subsequent DoD (delta-of-delta) values
    private int _state;
    private long _prevTs;
    private long _prevDelta;

    /// <summary>
    /// Gets the total number of bits read from the underlying bit reader.
    /// </summary>
    public long TotalBitsRead => _br.TotalBitsRead;

    /// <summary>
    /// Initializes a new instance of the <see cref="GorillaTimestampDecoder"/> class.
    /// </summary>
    /// <param name="input">Underlying bit reader providing the Gorilla-encoded stream.</param>
    /// <param name="firstDelta27Bits">
    /// When <see langword="true"/>, expect the first delta Δ1 to be a signed 27-bit value
    /// (exactly as in the original Gorilla paper/implementation). When <see langword="false"/>,
    /// Δ1 is read as an unsigned 64-bit value.
    /// </param>
    /// <param name="leaveOpen"> leaveOpen if set to <see langword="true"/> the <paramref name="input"/> will not be disposed when this instance is disposed.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    public GorillaTimestampDecoder(
        IBitReader input,
        bool firstDelta27Bits = true,
        bool leaveOpen = false
    )
    {
        _br = input ?? throw new ArgumentNullException(nameof(input));
        _firstDelta27Bits = firstDelta27Bits;
        _leaveOpen = leaveOpen;
        _state = 0;
    }

    /// <summary>
    /// Reads the next timestamp value from the stream.
    /// </summary>
    /// <returns>The next decoded timestamp.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the underlying bit stream ends prematurely and the next value cannot be fully decoded.
    /// </exception>
    public long ReadNext()
    {
        switch (_state)
        {
            // State 0: read initial timestamp t0 (unsigned 64 bits)
            case 0:
            {
                var ts0 = (long)_br.ReadBits(64);
                _prevTs = ts0;
                _state = 1;
                return ts0;
            }

            // State 1: read Δ1 (either signed 27 bits or unsigned 64 bits)
            case 1:
            {
                var delta1 = _firstDelta27Bits
                    ? SignExtend(_br.ReadBits(27), 27)
                    : (long)_br.ReadBits(64);

                var ts1 = _prevTs + delta1;
                _prevDelta = delta1;
                _prevTs = ts1;
                _state = 2;
                return ts1;
            }

            // State 2: read DoD using the Gorilla prefix tree and accumulate
            default:
            {
                // Read DoD prefix:
                // '0'                  -> DoD = 0
                // '10'                 -> 7-bit signed
                // '110'                -> 9-bit signed
                // '1110'               -> 12-bit signed
                // '1111'               -> 32-bit unsigned
                var p1 = _br.ReadBit();
                long dod;
                if (p1 == 0)
                {
                    dod = 0;
                }
                else
                {
                    var p2 = _br.ReadBit();
                    if (p2 == 0)
                    {
                        dod = SignExtend(_br.ReadBits(7), 7);
                    }
                    else
                    {
                        var p3 = _br.ReadBit();
                        if (p3 == 0)
                        {
                            dod = SignExtend(_br.ReadBits(9), 9);
                        }
                        else
                        {
                            var p4 = _br.ReadBit();
                            if (p4 == 0)
                            {
                                dod = SignExtend(_br.ReadBits(12), 12);
                            }
                            else
                            {
                                // Note: 32-bit field is stored as unsigned in the stream.
                                // It is added to the (signed) previous delta; the result fits Int64.
                                dod = (long)_br.ReadBits(32);
                            }
                        }
                    }
                }

                var delta = _prevDelta + dod;
                var ts = _prevTs + delta;
                _prevDelta = delta;
                _prevTs = ts;
                return ts;
            }
        }
    }

    /// <summary>
    /// Sign-extends a value of given bit <paramref name="width"/> to a 64-bit signed integer.
    /// </summary>
    /// <param name="bits">The raw value as read from the bit stream.</param>
    /// <param name="width">Bit width of the value (1..64).</param>
    /// <returns>A 64-bit signed integer representing the sign-extended value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="width"/> is not in the range 1..64.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long SignExtend(ulong bits, int width)
    {
        switch (width)
        {
            case <= 0 or > 64:
                throw new ArgumentOutOfRangeException(nameof(width));
            case 64:
                return (long)bits;
        }

        var sign = 1UL << (width - 1);
        if ((bits & sign) == 0)
        {
            // Non-negative: unchanged
            return (long)bits;
        }

        // Negative: fill all higher bits with ones
        var mask = ~((1UL << width) - 1);
        return (long)(bits | mask);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (!_leaveOpen)
            {
                _br.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (!_leaveOpen)
        {
            await _br.DisposeAsync();
        }

        await base.DisposeAsyncCore();
    }
}
