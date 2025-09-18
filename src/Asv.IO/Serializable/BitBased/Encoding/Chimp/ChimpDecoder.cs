using System;
using System.IO;
using System.Threading.Tasks;
using Asv.Common;

namespace Asv.IO;

/// <summary>
/// Value decoder (UInt64) implementing a Gorilla/Chimp-style XOR compaction scheme
/// with optional reuse of the previously defined significant-bit window.
/// </summary>
/// <remarks>
/// The bitstream is organized as follows:
/// <list type="number">
///   <item>
///     <description><b>First value</b>: 64 bits uncompressed. Stored and returned as-is.</description>
///   </item>
///   <item>
///     <description><b>Subsequent values</b> use a prefix to decide how to decode the XOR payload:
///       <para>
///         <code>
///           p1 = 0                   → Repeat previous 64-bit value (no payload)
///           p1 = 1, p2 = 0           → Reuse last window [L, W] and read W bits
///           p1 = 1, p2 = 1           → Define new window: read L (5 bits), W' (6 bits), set W = W' + 1, then read W bits
///         </code>
///       </para>
///       The <i>window</i> is defined by:
///       <list type="bullet">
///         <item><description><c>L</c> — number of leading zeros in the XOR'd value.</description></item>
///         <item><description><c>W</c> — number of significant bits in the XOR'd value (width in bits).</description></item>
///       </list>
///       Once <c>L</c> and <c>W</c> are known, the <c>XOR</c> payload is aligned into the 64-bit lane by left-shifting it
///       by <c>(64 - L - W)</c>, i.e. placing it starting at bit index <c>L</c>.
///       The current value is then recovered as <c>cur = prev ^ xor</c>.
///       <para/>
///       Special-case: when <c>W == 64</c>, the payload already fills the whole lane and no shift is applied.
///     </description>
///   </item>
/// </list>
/// If <c>p2 == 0</c> but the window has not yet been defined (<c>L &lt; 0</c> or <c>W &le; 0</c>), an
/// <see cref="InvalidDataException"/> is thrown.
/// </remarks>
public sealed class ChimpDecoder(IBitReader input, bool leaveOpen = false)
    : AsyncDisposableOnce, IBitDecoder<ulong>
{
    private bool _first = true;
    private ulong _prevBits;
    private int _l = -1;  // last known leading-zero count (L); -1 => undefined
    private int _w = -1;  // last known width (W);         -1 => undefined

    /// <summary>
    /// Total number of bits consumed from the underlying <see cref="IBitReader"/>.
    /// </summary>
    public long TotalBitsRead => input.TotalBitsRead;

    /// <summary>
    /// Decodes and returns the next 64-bit value from the stream.
    /// </summary>
    /// <returns>The next decoded <see cref="UInt64"/> value.</returns>
    /// <exception cref="InvalidDataException">
    /// Thrown when a "reuse window" instruction is encountered before any window (L, W) has been defined.
    /// </exception>
    public ulong ReadNext()
    {
        // The first value is stored uncompressed (full 64 bits).
        if (_first)
        {
            var b0 = input.ReadBits(64);
            _prevBits = b0;
            _first = false;
            return b0;
        }

        // p1 = 0  → repeat previous 64-bit value (no payload)
        // p1 = 1  → proceed to p2
        var p1 = input.ReadBit();
        if (p1 == 0)
        {
            return _prevBits;
        }

        // p2 = 0  → reuse last window [L, W] and read W bits
        // p2 = 1  → define new window, then read W bits
        var p2 = input.ReadBit();
        if (p2 == 0)
        {
            // Ensure a window was previously defined.
            if (_l < 0 || _w <= 0)
            {
                throw new InvalidDataException("Reuse before window defined.");
            }

            var tWin = 64 - _l - _w;               // trailing zeros (right side) for alignment
            var payload = input.ReadBits(_w);      // read W significant bits
            var xor = (_w == 64) ? payload : (payload << tWin); // align unless full-lane
            var cur = _prevBits ^ xor;             // reconstruct current value
            _prevBits = cur;
            return cur;
        }
        else
        {
            // Define a new window:
            // L: 5 bits (0..31)
            // W: 6 bits stored as (W-1), so actual W is (read + 1) in range [1..64]
            var L = (int)input.ReadBits(5);
            var W = (int)input.ReadBits(6) + 1;

            var tWin = 64 - L - W;                 // trailing zeros for alignment
            var payload = input.ReadBits(W);       // read W significant bits
            var xor = (W == 64) ? payload : (payload << tWin);
            var cur = _prevBits ^ xor;

            // Update the rolling state for potential reuse.
            _prevBits = cur;
            _l = L;
            _w = W;

            return cur;
        }
    }

    protected sealed override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (!leaveOpen)
            {
                input.Dispose();
            }
        }

        base.Dispose(disposing);
    }

    protected sealed override async ValueTask DisposeAsyncCore()
    {
        if (!leaveOpen)
        {
            await input.DisposeAsync();
        }

        await base.DisposeAsyncCore();
    }
}
