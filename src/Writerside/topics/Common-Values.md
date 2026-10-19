# Values, validation, and utilities

## Numeric input

`InvariantNumberParser.TryParse` has overloads for `double`, `int`, and `uint`, including minimum and maximum bounds. Check `ValidationResult.IsSuccess` before using the output value.

The parser accepts decimal input independently of the current numeric culture and supports multipliers such as `k` for thousands and `M` for millions. This is user-input parsing; use the matching [Asv.IO serialization APIs](IO-Serialization.md) for protocol fields.

```csharp
using System.Globalization;
using Asv.Common;

var result = InvariantNumberParser.TryParse("12.5", out double value, 0, 100);
if (result.IsFailed)
{
    var error = result.ValidationException;
    Console.WriteLine(error?.LocalizedMessage ?? error?.Message);
}
else
{
    Console.WriteLine(value.ToString(CultureInfo.InvariantCulture)); // 12.5
}
```

`ValidationResult` distinguishes success from failures such as empty input, invalid characters, a non-number, or an out-of-range value. `LocalizedException` retains a technical `Message` and an optional `LocalizedMessage`. Display the localized message when present and fall back to the technical message.

## Time

`ITimeService.Now` supplies wall-clock `DateTime` values to the time-based collections. `DefaultTimeService` uses `DateTime.Now`; `SetCorrection` is not implemented there. `CorrectedTimeService` provides a separate implementation for corrected time.

Other helpers, including `IncrementalRateCounter`, accept a .NET `TimeProvider`. Inject a controllable provider in tests instead of relying on delays. Keep the timestamp convention of stored data consistent with the time service used to expire it.

## Identifiers and display names

`ShortGuid` offers a compact GUID representation. `SemVersion` handles semantic-version values. These have different purposes from `WordFingerprint`, which creates a localized three-word display phrase.

```csharp
using System.Globalization;
using Asv.Common;

var id = Guid.Parse("c9a646d3-9c61-4cb7-bfcd-ee2522c8f633");
var name = WordFingerprint.Generate(id, CultureInfo.GetCultureInfo("en"));
Console.WriteLine(name);
```

A fingerprint is a display aid, not a unique identifier or security checksum. Preserve the original GUID or input alongside it. Pass a culture explicitly when a display phrase must be reproducible independently of `CurrentUICulture`.

## Other utilities

| Area | Types |
| --- | --- |
| Coordinates and angles | `GeoPoint`, `GeoMath`, `GeoPointLatitude`, `GeoPointLongitude`, `Angle`, `AngleDm`, `AngleMs` |
| Mathematics | `MathEx`, `NormalRandom`, `FloatingPointComparer` |
| Checksums | `X25Crc`, `Crc16Ccit`, `Crc32Mavlink` |
| Text output | `TextTable`, `CsvHelper`, `TextRender` |
| Progress and rates | `CallbackProgress<T>`, `IncrementalRateCounter` |

Choose a checksum implementation that matches the protocol, including its seed and finalization rules. CRC values detect accidental corruption; they do not authenticate data.

`PasswordHash` preserves an existing PBKDF2-SHA1 format: `iterations:base64(salt):base64(hash)`. Validation uses the iteration count and output length stored in that string. Treat this as a compatibility API; changing its algorithm without versioning the format would invalidate existing records.
