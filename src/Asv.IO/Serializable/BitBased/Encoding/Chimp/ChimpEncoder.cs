using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Asv.Common;

namespace Asv.IO;

/// <summary>
/// Encoder for a stream of <see cref="UInt64"/> samples using a Gorilla/Chimp-style XOR scheme with
/// a sticky significant-bits window (<c>L</c>, <c>W</c>).
/// </summary>
/// <remarks>
/// Bitstream layout:
/// <list type="number">
///   <item>
///     <description><b>First sample</b>: 64 bits written verbatim.</description>
///   </item>
///   <item>
///     <description><b>Subsequent samples</b> use a two-bit prefix and (optionally) a payload:
///       <para>
///         <code>
///           0                           → repeat previous value (no payload)
///           10 + P(W bits)              → reuse last window [L, W], write only payload
///           11 + L(5) + (W-1)(6) + P    → define new window and write payload
///         </code>
///       </para>
///       The window is defined by:
///       <list type="bullet">
///         <item><description><c>L</c> — leading zeros of <c>xor = prev ^ cur</c> (clamped to 0..31 and stored in 5 bits).</description></item>
///         <item><description><c>W</c> — number of significant bits of <c>xor</c> (stored as <c>W-1</c> in 6 bits, range 1..64).</description></item>
///       </list>
///       Payload <c>P</c> is the significant <c>W</c> bits of <c>xor</c>, aligned so that
///       <c>trailing = 64 - L - W</c>. For <c>W == 64</c> the payload occupies the entire lane (no shift/mask).
///     </description>
///   </item>
/// </list>
/// <para>
/// Reuse condition: when <c>pfx = 10</c>, the previously defined window <c>[L,W]</c> may be reused if
/// <c>leading(xor) ≥ L</c> and <c>sigbits(xor) ≤ W</c>.
/// </para>
/// <para>
/// This encoder only manipulates raw bit patterns of <see cref="UInt64"/>; it does not apply any zigzag
/// or sign conversions.
/// </para>
/// </remarks>
/// <example>
/// Example usage:
/// <code>
/// using var bw = new BitWriterStream(outputStream, leaveOpen: true);
/// using var enc = new ChimpEncoder(bw, leaveOpen: true);
/// enc.Add(first);
/// enc.Add(next);
/// // ...
/// </code>
/// </example>
public sealed class ChimpEncoder(IBitWriter writer, bool leaveOpen = false)
    : AsyncDisposableOnce, IBitEncoder<ulong>
{
    private bool _first = true;
    private ulong _prevBits;

    private int _l = -1; // sticky leading zeros (L); -1 => undefined
    private int _w = -1; // sticky width of significant bits (W); -1 => undefined

    /// <summary>
    /// Gets total number of bits written by the underlying <see cref="IBitWriter"/>.
    /// </summary>
    public long TotalBitsWritten => writer.TotalBitsWritten;

    /// <summary>
    /// Appends a 64-bit value to the encoded stream.
    /// </summary>
    /// <param name="bits">The value to encode (raw 64-bit pattern).</param>
    public void Add(ulong bits)
    {
        // First sample: write 64 bits verbatim.
        if (_first)
        {
            writer.WriteBits(bits, 64);
            _prevBits = bits;
            _first = false;
            return;
        }

        // XOR delta between current and previous value.
        var xor = _prevBits ^ bits;
        if (xor == 0)
        {
            // Prefix '0' → exact repeat of previous value.
            writer.WriteBit(0);
            _prevBits = bits;
            return;
        }

        // Compute window parameters from XOR.
        var leading = BitOperations.LeadingZeroCount(xor);
        var trailing = BitOperations.TrailingZeroCount(xor);
        var sig = 64 - leading - trailing;
        if (sig <= 0)
        {
            sig = 1; // defensive (shouldn't normally happen)
        }

        // Eligible to reuse the last [L, W] if the new XOR fits entirely within that window.
        var reuse = (_l >= 0 && _w > 0) && (leading >= _l) && (sig <= _w);
        if (reuse)
        {
            // Prefix '10' → reuse [L, W], emit only W-bit payload.
            writer.WriteBits(0b10, 2);
            var tWin = 64 - _l - _w;
            var payload = (_w == 64)
                ? xor
                : ((xor >> tWin) & ((1UL << _w) - 1));
            writer.WriteBits(payload, _w);
        }
        else
        {
            // Define a new window:
            // L is clamped to 0..31 (5 bits), W is clamped to 1..64 and encoded as (W-1) in 6 bits.
            var l5 = Math.Min(leading, 31);
            var w = Math.Min(Math.Max(sig, 1), 64);

            // Prefix '11' + L(5) + (W-1)(6) + payload(W).
            writer.WriteBits(0b11, 2);
            writer.WriteBits((ulong)l5, 5);
            writer.WriteBits((ulong)(w - 1), 6);

            var tWin = 64 - l5 - w;
            var payload = (w == 64)
                ? xor
                : ((xor >> tWin) & ((1UL << w) - 1));
            writer.WriteBits(payload, w);

            // Update sticky window.
            _l = l5;
            _w = w;
        }

        _prevBits = bits;
    }

    /// <summary>
    /// Disposes the underlying writer unless <c>leaveOpen</c> is <see langword="true"/>.
    /// </summary>
    /// <param name="disposing">True if called from <see cref="Dispose()"/>.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing && !leaveOpen)
        {
            writer.Dispose();
        }
        base.Dispose(disposing);
    }

    /// <summary>
    /// Asynchronously disposes the underlying writer unless <c>leaveOpen</c> is <see langword="true"/>.
    /// </summary>
    protected override async ValueTask DisposeAsyncCore()
    {
        if (!leaveOpen)
        {
            await writer.DisposeAsync();
        }
        await base.DisposeAsyncCore();
    }
}
