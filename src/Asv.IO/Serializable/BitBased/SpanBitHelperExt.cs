using System;

namespace Asv.IO
{
    public static partial class SpanBitHelper
    {
        #region FixedPointS3

        public const int FixedPointS3PositiveInf = 3;
        public const int FixedPointS3NegativeInf = -3;
        public const int FixedPointS3Nan = -4;
        public const int FixedPointS3Max = 2;
        public const int FixedPointS3Min = -2;

        public static double GetFixedPointS3Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 3);
            return value switch
            {
                FixedPointS3Nan => double.NaN,
                FixedPointS3PositiveInf => double.PositiveInfinity,
                FixedPointS3NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS3Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 3);
            return value switch
            {
                FixedPointS3Nan => double.NaN,
                FixedPointS3PositiveInf => double.PositiveInfinity,
                FixedPointS3NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS3Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 3);
            return value switch
            {
                FixedPointS3Nan => double.NaN,
                FixedPointS3PositiveInf => double.PositiveInfinity,
                FixedPointS3NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS3Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS3Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS3Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 3);
            var convertedValue = value switch
            {
                FixedPointS3Nan => double.NaN,
                FixedPointS3PositiveInf => double.PositiveInfinity,
                FixedPointS3NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS3Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 3, FixedPointS3Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 3, FixedPointS3NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 3, FixedPointS3PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS3Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 3, FixedPointS3PositiveInf);
                    break;
                case < FixedPointS3Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 3, FixedPointS3NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 3, intValue);
                    break;
            }
        }

        public static void SetFixedPointS3Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS3Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS3Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS3Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS3Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS3Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS3Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS3Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS4

        public const int FixedPointS4PositiveInf = 7;
        public const int FixedPointS4NegativeInf = -7;
        public const int FixedPointS4Nan = -8;
        public const int FixedPointS4Max = 6;
        public const int FixedPointS4Min = -6;

        public static double GetFixedPointS4Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 4);
            return value switch
            {
                FixedPointS4Nan => double.NaN,
                FixedPointS4PositiveInf => double.PositiveInfinity,
                FixedPointS4NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS4Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 4);
            return value switch
            {
                FixedPointS4Nan => double.NaN,
                FixedPointS4PositiveInf => double.PositiveInfinity,
                FixedPointS4NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS4Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 4);
            return value switch
            {
                FixedPointS4Nan => double.NaN,
                FixedPointS4PositiveInf => double.PositiveInfinity,
                FixedPointS4NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS4Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS4Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS4Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 4);
            var convertedValue = value switch
            {
                FixedPointS4Nan => double.NaN,
                FixedPointS4PositiveInf => double.PositiveInfinity,
                FixedPointS4NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS4Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 4, FixedPointS4Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 4, FixedPointS4NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 4, FixedPointS4PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS4Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 4, FixedPointS4PositiveInf);
                    break;
                case < FixedPointS4Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 4, FixedPointS4NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 4, intValue);
                    break;
            }
        }

        public static void SetFixedPointS4Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS4Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS4Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS4Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS4Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS4Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS4Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS4Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS5

        public const int FixedPointS5PositiveInf = 15;
        public const int FixedPointS5NegativeInf = -15;
        public const int FixedPointS5Nan = -16;
        public const int FixedPointS5Max = 14;
        public const int FixedPointS5Min = -14;

        public static double GetFixedPointS5Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 5);
            return value switch
            {
                FixedPointS5Nan => double.NaN,
                FixedPointS5PositiveInf => double.PositiveInfinity,
                FixedPointS5NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS5Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 5);
            return value switch
            {
                FixedPointS5Nan => double.NaN,
                FixedPointS5PositiveInf => double.PositiveInfinity,
                FixedPointS5NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS5Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 5);
            return value switch
            {
                FixedPointS5Nan => double.NaN,
                FixedPointS5PositiveInf => double.PositiveInfinity,
                FixedPointS5NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS5Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS5Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS5Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 5);
            var convertedValue = value switch
            {
                FixedPointS5Nan => double.NaN,
                FixedPointS5PositiveInf => double.PositiveInfinity,
                FixedPointS5NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS5Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 5, FixedPointS5Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 5, FixedPointS5NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 5, FixedPointS5PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS5Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 5, FixedPointS5PositiveInf);
                    break;
                case < FixedPointS5Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 5, FixedPointS5NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 5, intValue);
                    break;
            }
        }

        public static void SetFixedPointS5Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS5Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS5Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS5Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS5Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS5Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS5Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS5Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS6

        public const int FixedPointS6PositiveInf = 31;
        public const int FixedPointS6NegativeInf = -31;
        public const int FixedPointS6Nan = -32;
        public const int FixedPointS6Max = 30;
        public const int FixedPointS6Min = -30;

        public static double GetFixedPointS6Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 6);
            return value switch
            {
                FixedPointS6Nan => double.NaN,
                FixedPointS6PositiveInf => double.PositiveInfinity,
                FixedPointS6NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS6Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 6);
            return value switch
            {
                FixedPointS6Nan => double.NaN,
                FixedPointS6PositiveInf => double.PositiveInfinity,
                FixedPointS6NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS6Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 6);
            return value switch
            {
                FixedPointS6Nan => double.NaN,
                FixedPointS6PositiveInf => double.PositiveInfinity,
                FixedPointS6NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS6Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS6Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS6Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 6);
            var convertedValue = value switch
            {
                FixedPointS6Nan => double.NaN,
                FixedPointS6PositiveInf => double.PositiveInfinity,
                FixedPointS6NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS6Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 6, FixedPointS6Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 6, FixedPointS6NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 6, FixedPointS6PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS6Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 6, FixedPointS6PositiveInf);
                    break;
                case < FixedPointS6Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 6, FixedPointS6NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 6, intValue);
                    break;
            }
        }

        public static void SetFixedPointS6Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS6Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS6Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS6Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS6Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS6Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS6Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS6Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS7

        public const int FixedPointS7PositiveInf = 63;
        public const int FixedPointS7NegativeInf = -63;
        public const int FixedPointS7Nan = -64;
        public const int FixedPointS7Max = 62;
        public const int FixedPointS7Min = -62;

        public static double GetFixedPointS7Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 7);
            return value switch
            {
                FixedPointS7Nan => double.NaN,
                FixedPointS7PositiveInf => double.PositiveInfinity,
                FixedPointS7NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS7Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 7);
            return value switch
            {
                FixedPointS7Nan => double.NaN,
                FixedPointS7PositiveInf => double.PositiveInfinity,
                FixedPointS7NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS7Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 7);
            return value switch
            {
                FixedPointS7Nan => double.NaN,
                FixedPointS7PositiveInf => double.PositiveInfinity,
                FixedPointS7NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS7Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS7Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS7Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 7);
            var convertedValue = value switch
            {
                FixedPointS7Nan => double.NaN,
                FixedPointS7PositiveInf => double.PositiveInfinity,
                FixedPointS7NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS7Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 7, FixedPointS7Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 7, FixedPointS7NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 7, FixedPointS7PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS7Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 7, FixedPointS7PositiveInf);
                    break;
                case < FixedPointS7Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 7, FixedPointS7NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 7, intValue);
                    break;
            }
        }

        public static void SetFixedPointS7Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS7Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS7Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS7Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS7Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS7Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS7Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS7Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS8

        public const int FixedPointS8PositiveInf = 127;
        public const int FixedPointS8NegativeInf = -127;
        public const int FixedPointS8Nan = -128;
        public const int FixedPointS8Max = 126;
        public const int FixedPointS8Min = -126;

        public static double GetFixedPointS8Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 8);
            return value switch
            {
                FixedPointS8Nan => double.NaN,
                FixedPointS8PositiveInf => double.PositiveInfinity,
                FixedPointS8NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS8Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 8);
            return value switch
            {
                FixedPointS8Nan => double.NaN,
                FixedPointS8PositiveInf => double.PositiveInfinity,
                FixedPointS8NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS8Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 8);
            return value switch
            {
                FixedPointS8Nan => double.NaN,
                FixedPointS8PositiveInf => double.PositiveInfinity,
                FixedPointS8NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS8Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS8Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS8Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 8);
            var convertedValue = value switch
            {
                FixedPointS8Nan => double.NaN,
                FixedPointS8PositiveInf => double.PositiveInfinity,
                FixedPointS8NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS8Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 8, FixedPointS8Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 8, FixedPointS8NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 8, FixedPointS8PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS8Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 8, FixedPointS8PositiveInf);
                    break;
                case < FixedPointS8Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 8, FixedPointS8NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 8, intValue);
                    break;
            }
        }

        public static void SetFixedPointS8Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS8Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS8Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS8Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS8Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS8Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS8Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS8Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS9

        public const int FixedPointS9PositiveInf = 255;
        public const int FixedPointS9NegativeInf = -255;
        public const int FixedPointS9Nan = -256;
        public const int FixedPointS9Max = 254;
        public const int FixedPointS9Min = -254;

        public static double GetFixedPointS9Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 9);
            return value switch
            {
                FixedPointS9Nan => double.NaN,
                FixedPointS9PositiveInf => double.PositiveInfinity,
                FixedPointS9NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS9Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 9);
            return value switch
            {
                FixedPointS9Nan => double.NaN,
                FixedPointS9PositiveInf => double.PositiveInfinity,
                FixedPointS9NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS9Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 9);
            return value switch
            {
                FixedPointS9Nan => double.NaN,
                FixedPointS9PositiveInf => double.PositiveInfinity,
                FixedPointS9NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS9Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS9Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS9Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 9);
            var convertedValue = value switch
            {
                FixedPointS9Nan => double.NaN,
                FixedPointS9PositiveInf => double.PositiveInfinity,
                FixedPointS9NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS9Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 9, FixedPointS9Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 9, FixedPointS9NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 9, FixedPointS9PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS9Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 9, FixedPointS9PositiveInf);
                    break;
                case < FixedPointS9Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 9, FixedPointS9NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 9, intValue);
                    break;
            }
        }

        public static void SetFixedPointS9Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS9Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS9Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS9Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS9Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS9Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS9Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS9Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS10

        public const int FixedPointS10PositiveInf = 511;
        public const int FixedPointS10NegativeInf = -511;
        public const int FixedPointS10Nan = -512;
        public const int FixedPointS10Max = 510;
        public const int FixedPointS10Min = -510;

        public static double GetFixedPointS10Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 10);
            return value switch
            {
                FixedPointS10Nan => double.NaN,
                FixedPointS10PositiveInf => double.PositiveInfinity,
                FixedPointS10NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS10Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 10);
            return value switch
            {
                FixedPointS10Nan => double.NaN,
                FixedPointS10PositiveInf => double.PositiveInfinity,
                FixedPointS10NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS10Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 10);
            return value switch
            {
                FixedPointS10Nan => double.NaN,
                FixedPointS10PositiveInf => double.PositiveInfinity,
                FixedPointS10NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS10Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS10Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS10Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 10);
            var convertedValue = value switch
            {
                FixedPointS10Nan => double.NaN,
                FixedPointS10PositiveInf => double.PositiveInfinity,
                FixedPointS10NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS10Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 10, FixedPointS10Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 10, FixedPointS10NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 10, FixedPointS10PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS10Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 10, FixedPointS10PositiveInf);
                    break;
                case < FixedPointS10Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 10, FixedPointS10NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 10, intValue);
                    break;
            }
        }

        public static void SetFixedPointS10Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS10Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS10Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS10Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS10Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS10Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS10Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS10Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS11

        public const int FixedPointS11PositiveInf = 1023;
        public const int FixedPointS11NegativeInf = -1023;
        public const int FixedPointS11Nan = -1024;
        public const int FixedPointS11Max = 1022;
        public const int FixedPointS11Min = -1022;

        public static double GetFixedPointS11Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 11);
            return value switch
            {
                FixedPointS11Nan => double.NaN,
                FixedPointS11PositiveInf => double.PositiveInfinity,
                FixedPointS11NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS11Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 11);
            return value switch
            {
                FixedPointS11Nan => double.NaN,
                FixedPointS11PositiveInf => double.PositiveInfinity,
                FixedPointS11NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS11Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 11);
            return value switch
            {
                FixedPointS11Nan => double.NaN,
                FixedPointS11PositiveInf => double.PositiveInfinity,
                FixedPointS11NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS11Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS11Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS11Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 11);
            var convertedValue = value switch
            {
                FixedPointS11Nan => double.NaN,
                FixedPointS11PositiveInf => double.PositiveInfinity,
                FixedPointS11NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS11Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 11, FixedPointS11Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 11, FixedPointS11NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 11, FixedPointS11PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS11Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 11, FixedPointS11PositiveInf);
                    break;
                case < FixedPointS11Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 11, FixedPointS11NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 11, intValue);
                    break;
            }
        }

        public static void SetFixedPointS11Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS11Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS11Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS11Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS11Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS11Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS11Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS11Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS12

        public const int FixedPointS12PositiveInf = 2047;
        public const int FixedPointS12NegativeInf = -2047;
        public const int FixedPointS12Nan = -2048;
        public const int FixedPointS12Max = 2046;
        public const int FixedPointS12Min = -2046;

        public static double GetFixedPointS12Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 12);
            return value switch
            {
                FixedPointS12Nan => double.NaN,
                FixedPointS12PositiveInf => double.PositiveInfinity,
                FixedPointS12NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS12Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 12);
            return value switch
            {
                FixedPointS12Nan => double.NaN,
                FixedPointS12PositiveInf => double.PositiveInfinity,
                FixedPointS12NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS12Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 12);
            return value switch
            {
                FixedPointS12Nan => double.NaN,
                FixedPointS12PositiveInf => double.PositiveInfinity,
                FixedPointS12NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS12Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS12Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS12Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 12);
            var convertedValue = value switch
            {
                FixedPointS12Nan => double.NaN,
                FixedPointS12PositiveInf => double.PositiveInfinity,
                FixedPointS12NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS12Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 12, FixedPointS12Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 12, FixedPointS12NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 12, FixedPointS12PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS12Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 12, FixedPointS12PositiveInf);
                    break;
                case < FixedPointS12Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 12, FixedPointS12NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 12, intValue);
                    break;
            }
        }

        public static void SetFixedPointS12Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS12Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS12Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS12Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS12Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS12Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS12Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS12Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS13

        public const int FixedPointS13PositiveInf = 4095;
        public const int FixedPointS13NegativeInf = -4095;
        public const int FixedPointS13Nan = -4096;
        public const int FixedPointS13Max = 4094;
        public const int FixedPointS13Min = -4094;

        public static double GetFixedPointS13Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 13);
            return value switch
            {
                FixedPointS13Nan => double.NaN,
                FixedPointS13PositiveInf => double.PositiveInfinity,
                FixedPointS13NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS13Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 13);
            return value switch
            {
                FixedPointS13Nan => double.NaN,
                FixedPointS13PositiveInf => double.PositiveInfinity,
                FixedPointS13NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS13Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 13);
            return value switch
            {
                FixedPointS13Nan => double.NaN,
                FixedPointS13PositiveInf => double.PositiveInfinity,
                FixedPointS13NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS13Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS13Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS13Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 13);
            var convertedValue = value switch
            {
                FixedPointS13Nan => double.NaN,
                FixedPointS13PositiveInf => double.PositiveInfinity,
                FixedPointS13NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS13Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 13, FixedPointS13Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 13, FixedPointS13NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 13, FixedPointS13PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS13Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 13, FixedPointS13PositiveInf);
                    break;
                case < FixedPointS13Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 13, FixedPointS13NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 13, intValue);
                    break;
            }
        }

        public static void SetFixedPointS13Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS13Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS13Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS13Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS13Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS13Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS13Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS13Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS14

        public const int FixedPointS14PositiveInf = 8191;
        public const int FixedPointS14NegativeInf = -8191;
        public const int FixedPointS14Nan = -8192;
        public const int FixedPointS14Max = 8190;
        public const int FixedPointS14Min = -8190;

        public static double GetFixedPointS14Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 14);
            return value switch
            {
                FixedPointS14Nan => double.NaN,
                FixedPointS14PositiveInf => double.PositiveInfinity,
                FixedPointS14NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS14Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 14);
            return value switch
            {
                FixedPointS14Nan => double.NaN,
                FixedPointS14PositiveInf => double.PositiveInfinity,
                FixedPointS14NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS14Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 14);
            return value switch
            {
                FixedPointS14Nan => double.NaN,
                FixedPointS14PositiveInf => double.PositiveInfinity,
                FixedPointS14NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS14Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS14Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS14Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 14);
            var convertedValue = value switch
            {
                FixedPointS14Nan => double.NaN,
                FixedPointS14PositiveInf => double.PositiveInfinity,
                FixedPointS14NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS14Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 14, FixedPointS14Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 14, FixedPointS14NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 14, FixedPointS14PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS14Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 14, FixedPointS14PositiveInf);
                    break;
                case < FixedPointS14Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 14, FixedPointS14NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 14, intValue);
                    break;
            }
        }

        public static void SetFixedPointS14Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS14Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS14Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS14Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS14Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS14Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS14Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS14Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS15

        public const int FixedPointS15PositiveInf = 16383;
        public const int FixedPointS15NegativeInf = -16383;
        public const int FixedPointS15Nan = -16384;
        public const int FixedPointS15Max = 16382;
        public const int FixedPointS15Min = -16382;

        public static double GetFixedPointS15Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 15);
            return value switch
            {
                FixedPointS15Nan => double.NaN,
                FixedPointS15PositiveInf => double.PositiveInfinity,
                FixedPointS15NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS15Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 15);
            return value switch
            {
                FixedPointS15Nan => double.NaN,
                FixedPointS15PositiveInf => double.PositiveInfinity,
                FixedPointS15NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS15Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 15);
            return value switch
            {
                FixedPointS15Nan => double.NaN,
                FixedPointS15PositiveInf => double.PositiveInfinity,
                FixedPointS15NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS15Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS15Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS15Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 15);
            var convertedValue = value switch
            {
                FixedPointS15Nan => double.NaN,
                FixedPointS15PositiveInf => double.PositiveInfinity,
                FixedPointS15NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS15Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 15, FixedPointS15Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 15, FixedPointS15NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 15, FixedPointS15PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS15Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 15, FixedPointS15PositiveInf);
                    break;
                case < FixedPointS15Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 15, FixedPointS15NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 15, intValue);
                    break;
            }
        }

        public static void SetFixedPointS15Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS15Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS15Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS15Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS15Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS15Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS15Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS15Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS16

        public const int FixedPointS16PositiveInf = 32767;
        public const int FixedPointS16NegativeInf = -32767;
        public const int FixedPointS16Nan = -32768;
        public const int FixedPointS16Max = 32766;
        public const int FixedPointS16Min = -32766;

        public static double GetFixedPointS16Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 16);
            return value switch
            {
                FixedPointS16Nan => double.NaN,
                FixedPointS16PositiveInf => double.PositiveInfinity,
                FixedPointS16NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS16Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 16);
            return value switch
            {
                FixedPointS16Nan => double.NaN,
                FixedPointS16PositiveInf => double.PositiveInfinity,
                FixedPointS16NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS16Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 16);
            return value switch
            {
                FixedPointS16Nan => double.NaN,
                FixedPointS16PositiveInf => double.PositiveInfinity,
                FixedPointS16NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS16Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS16Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS16Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 16);
            var convertedValue = value switch
            {
                FixedPointS16Nan => double.NaN,
                FixedPointS16PositiveInf => double.PositiveInfinity,
                FixedPointS16NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS16Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 16, FixedPointS16Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 16, FixedPointS16NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 16, FixedPointS16PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS16Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 16, FixedPointS16PositiveInf);
                    break;
                case < FixedPointS16Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 16, FixedPointS16NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 16, intValue);
                    break;
            }
        }

        public static void SetFixedPointS16Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS16Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS16Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS16Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS16Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS16Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS16Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS16Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS17

        public const int FixedPointS17PositiveInf = 65535;
        public const int FixedPointS17NegativeInf = -65535;
        public const int FixedPointS17Nan = -65536;
        public const int FixedPointS17Max = 65534;
        public const int FixedPointS17Min = -65534;

        public static double GetFixedPointS17Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 17);
            return value switch
            {
                FixedPointS17Nan => double.NaN,
                FixedPointS17PositiveInf => double.PositiveInfinity,
                FixedPointS17NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS17Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 17);
            return value switch
            {
                FixedPointS17Nan => double.NaN,
                FixedPointS17PositiveInf => double.PositiveInfinity,
                FixedPointS17NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS17Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 17);
            return value switch
            {
                FixedPointS17Nan => double.NaN,
                FixedPointS17PositiveInf => double.PositiveInfinity,
                FixedPointS17NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS17Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS17Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS17Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 17);
            var convertedValue = value switch
            {
                FixedPointS17Nan => double.NaN,
                FixedPointS17PositiveInf => double.PositiveInfinity,
                FixedPointS17NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS17Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 17, FixedPointS17Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 17, FixedPointS17NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 17, FixedPointS17PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS17Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 17, FixedPointS17PositiveInf);
                    break;
                case < FixedPointS17Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 17, FixedPointS17NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 17, intValue);
                    break;
            }
        }

        public static void SetFixedPointS17Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS17Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS17Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS17Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS17Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS17Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS17Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS17Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS18

        public const int FixedPointS18PositiveInf = 131071;
        public const int FixedPointS18NegativeInf = -131071;
        public const int FixedPointS18Nan = -131072;
        public const int FixedPointS18Max = 131070;
        public const int FixedPointS18Min = -131070;

        public static double GetFixedPointS18Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 18);
            return value switch
            {
                FixedPointS18Nan => double.NaN,
                FixedPointS18PositiveInf => double.PositiveInfinity,
                FixedPointS18NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS18Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 18);
            return value switch
            {
                FixedPointS18Nan => double.NaN,
                FixedPointS18PositiveInf => double.PositiveInfinity,
                FixedPointS18NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS18Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 18);
            return value switch
            {
                FixedPointS18Nan => double.NaN,
                FixedPointS18PositiveInf => double.PositiveInfinity,
                FixedPointS18NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS18Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS18Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS18Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 18);
            var convertedValue = value switch
            {
                FixedPointS18Nan => double.NaN,
                FixedPointS18PositiveInf => double.PositiveInfinity,
                FixedPointS18NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS18Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 18, FixedPointS18Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 18, FixedPointS18NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 18, FixedPointS18PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS18Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 18, FixedPointS18PositiveInf);
                    break;
                case < FixedPointS18Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 18, FixedPointS18NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 18, intValue);
                    break;
            }
        }

        public static void SetFixedPointS18Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS18Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS18Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS18Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS18Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS18Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS18Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS18Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS19

        public const int FixedPointS19PositiveInf = 262143;
        public const int FixedPointS19NegativeInf = -262143;
        public const int FixedPointS19Nan = -262144;
        public const int FixedPointS19Max = 262142;
        public const int FixedPointS19Min = -262142;

        public static double GetFixedPointS19Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 19);
            return value switch
            {
                FixedPointS19Nan => double.NaN,
                FixedPointS19PositiveInf => double.PositiveInfinity,
                FixedPointS19NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS19Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 19);
            return value switch
            {
                FixedPointS19Nan => double.NaN,
                FixedPointS19PositiveInf => double.PositiveInfinity,
                FixedPointS19NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS19Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 19);
            return value switch
            {
                FixedPointS19Nan => double.NaN,
                FixedPointS19PositiveInf => double.PositiveInfinity,
                FixedPointS19NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS19Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS19Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS19Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 19);
            var convertedValue = value switch
            {
                FixedPointS19Nan => double.NaN,
                FixedPointS19PositiveInf => double.PositiveInfinity,
                FixedPointS19NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS19Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 19, FixedPointS19Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 19, FixedPointS19NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 19, FixedPointS19PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS19Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 19, FixedPointS19PositiveInf);
                    break;
                case < FixedPointS19Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 19, FixedPointS19NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 19, intValue);
                    break;
            }
        }

        public static void SetFixedPointS19Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS19Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS19Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS19Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS19Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS19Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS19Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS19Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS20

        public const int FixedPointS20PositiveInf = 524287;
        public const int FixedPointS20NegativeInf = -524287;
        public const int FixedPointS20Nan = -524288;
        public const int FixedPointS20Max = 524286;
        public const int FixedPointS20Min = -524286;

        public static double GetFixedPointS20Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 20);
            return value switch
            {
                FixedPointS20Nan => double.NaN,
                FixedPointS20PositiveInf => double.PositiveInfinity,
                FixedPointS20NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS20Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 20);
            return value switch
            {
                FixedPointS20Nan => double.NaN,
                FixedPointS20PositiveInf => double.PositiveInfinity,
                FixedPointS20NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS20Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 20);
            return value switch
            {
                FixedPointS20Nan => double.NaN,
                FixedPointS20PositiveInf => double.PositiveInfinity,
                FixedPointS20NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS20Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS20Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS20Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 20);
            var convertedValue = value switch
            {
                FixedPointS20Nan => double.NaN,
                FixedPointS20PositiveInf => double.PositiveInfinity,
                FixedPointS20NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS20Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 20, FixedPointS20Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 20, FixedPointS20NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 20, FixedPointS20PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS20Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 20, FixedPointS20PositiveInf);
                    break;
                case < FixedPointS20Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 20, FixedPointS20NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 20, intValue);
                    break;
            }
        }

        public static void SetFixedPointS20Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS20Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS20Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS20Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS20Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS20Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS20Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS20Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS21

        public const int FixedPointS21PositiveInf = 1048575;
        public const int FixedPointS21NegativeInf = -1048575;
        public const int FixedPointS21Nan = -1048576;
        public const int FixedPointS21Max = 1048574;
        public const int FixedPointS21Min = -1048574;

        public static double GetFixedPointS21Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 21);
            return value switch
            {
                FixedPointS21Nan => double.NaN,
                FixedPointS21PositiveInf => double.PositiveInfinity,
                FixedPointS21NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS21Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 21);
            return value switch
            {
                FixedPointS21Nan => double.NaN,
                FixedPointS21PositiveInf => double.PositiveInfinity,
                FixedPointS21NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS21Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 21);
            return value switch
            {
                FixedPointS21Nan => double.NaN,
                FixedPointS21PositiveInf => double.PositiveInfinity,
                FixedPointS21NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS21Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS21Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS21Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 21);
            var convertedValue = value switch
            {
                FixedPointS21Nan => double.NaN,
                FixedPointS21PositiveInf => double.PositiveInfinity,
                FixedPointS21NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS21Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 21, FixedPointS21Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 21, FixedPointS21NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 21, FixedPointS21PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS21Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 21, FixedPointS21PositiveInf);
                    break;
                case < FixedPointS21Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 21, FixedPointS21NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 21, intValue);
                    break;
            }
        }

        public static void SetFixedPointS21Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS21Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS21Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS21Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS21Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS21Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS21Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS21Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS22

        public const int FixedPointS22PositiveInf = 2097151;
        public const int FixedPointS22NegativeInf = -2097151;
        public const int FixedPointS22Nan = -2097152;
        public const int FixedPointS22Max = 2097150;
        public const int FixedPointS22Min = -2097150;

        public static double GetFixedPointS22Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 22);
            return value switch
            {
                FixedPointS22Nan => double.NaN,
                FixedPointS22PositiveInf => double.PositiveInfinity,
                FixedPointS22NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS22Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 22);
            return value switch
            {
                FixedPointS22Nan => double.NaN,
                FixedPointS22PositiveInf => double.PositiveInfinity,
                FixedPointS22NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS22Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 22);
            return value switch
            {
                FixedPointS22Nan => double.NaN,
                FixedPointS22PositiveInf => double.PositiveInfinity,
                FixedPointS22NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS22Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS22Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS22Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 22);
            var convertedValue = value switch
            {
                FixedPointS22Nan => double.NaN,
                FixedPointS22PositiveInf => double.PositiveInfinity,
                FixedPointS22NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS22Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 22, FixedPointS22Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 22, FixedPointS22NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 22, FixedPointS22PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS22Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 22, FixedPointS22PositiveInf);
                    break;
                case < FixedPointS22Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 22, FixedPointS22NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 22, intValue);
                    break;
            }
        }

        public static void SetFixedPointS22Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS22Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS22Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS22Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS22Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS22Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS22Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS22Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS23

        public const int FixedPointS23PositiveInf = 4194303;
        public const int FixedPointS23NegativeInf = -4194303;
        public const int FixedPointS23Nan = -4194304;
        public const int FixedPointS23Max = 4194302;
        public const int FixedPointS23Min = -4194302;

        public static double GetFixedPointS23Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 23);
            return value switch
            {
                FixedPointS23Nan => double.NaN,
                FixedPointS23PositiveInf => double.PositiveInfinity,
                FixedPointS23NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS23Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 23);
            return value switch
            {
                FixedPointS23Nan => double.NaN,
                FixedPointS23PositiveInf => double.PositiveInfinity,
                FixedPointS23NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS23Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 23);
            return value switch
            {
                FixedPointS23Nan => double.NaN,
                FixedPointS23PositiveInf => double.PositiveInfinity,
                FixedPointS23NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS23Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS23Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS23Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 23);
            var convertedValue = value switch
            {
                FixedPointS23Nan => double.NaN,
                FixedPointS23PositiveInf => double.PositiveInfinity,
                FixedPointS23NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS23Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 23, FixedPointS23Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 23, FixedPointS23NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 23, FixedPointS23PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS23Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 23, FixedPointS23PositiveInf);
                    break;
                case < FixedPointS23Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 23, FixedPointS23NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 23, intValue);
                    break;
            }
        }

        public static void SetFixedPointS23Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS23Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS23Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS23Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS23Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS23Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS23Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS23Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS24

        public const int FixedPointS24PositiveInf = 8388607;
        public const int FixedPointS24NegativeInf = -8388607;
        public const int FixedPointS24Nan = -8388608;
        public const int FixedPointS24Max = 8388606;
        public const int FixedPointS24Min = -8388606;

        public static double GetFixedPointS24Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 24);
            return value switch
            {
                FixedPointS24Nan => double.NaN,
                FixedPointS24PositiveInf => double.PositiveInfinity,
                FixedPointS24NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS24Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 24);
            return value switch
            {
                FixedPointS24Nan => double.NaN,
                FixedPointS24PositiveInf => double.PositiveInfinity,
                FixedPointS24NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS24Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 24);
            return value switch
            {
                FixedPointS24Nan => double.NaN,
                FixedPointS24PositiveInf => double.PositiveInfinity,
                FixedPointS24NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS24Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS24Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS24Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 24);
            var convertedValue = value switch
            {
                FixedPointS24Nan => double.NaN,
                FixedPointS24PositiveInf => double.PositiveInfinity,
                FixedPointS24NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS24Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 24, FixedPointS24Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 24, FixedPointS24NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 24, FixedPointS24PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS24Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 24, FixedPointS24PositiveInf);
                    break;
                case < FixedPointS24Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 24, FixedPointS24NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 24, intValue);
                    break;
            }
        }

        public static void SetFixedPointS24Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS24Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS24Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS24Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS24Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS24Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS24Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS24Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS25

        public const int FixedPointS25PositiveInf = 16777215;
        public const int FixedPointS25NegativeInf = -16777215;
        public const int FixedPointS25Nan = -16777216;
        public const int FixedPointS25Max = 16777214;
        public const int FixedPointS25Min = -16777214;

        public static double GetFixedPointS25Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 25);
            return value switch
            {
                FixedPointS25Nan => double.NaN,
                FixedPointS25PositiveInf => double.PositiveInfinity,
                FixedPointS25NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS25Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 25);
            return value switch
            {
                FixedPointS25Nan => double.NaN,
                FixedPointS25PositiveInf => double.PositiveInfinity,
                FixedPointS25NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS25Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 25);
            return value switch
            {
                FixedPointS25Nan => double.NaN,
                FixedPointS25PositiveInf => double.PositiveInfinity,
                FixedPointS25NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS25Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS25Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS25Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 25);
            var convertedValue = value switch
            {
                FixedPointS25Nan => double.NaN,
                FixedPointS25PositiveInf => double.PositiveInfinity,
                FixedPointS25NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS25Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 25, FixedPointS25Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 25, FixedPointS25NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 25, FixedPointS25PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS25Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 25, FixedPointS25PositiveInf);
                    break;
                case < FixedPointS25Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 25, FixedPointS25NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 25, intValue);
                    break;
            }
        }

        public static void SetFixedPointS25Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS25Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS25Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS25Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS25Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS25Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS25Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS25Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS26

        public const int FixedPointS26PositiveInf = 33554431;
        public const int FixedPointS26NegativeInf = -33554431;
        public const int FixedPointS26Nan = -33554432;
        public const int FixedPointS26Max = 33554430;
        public const int FixedPointS26Min = -33554430;

        public static double GetFixedPointS26Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 26);
            return value switch
            {
                FixedPointS26Nan => double.NaN,
                FixedPointS26PositiveInf => double.PositiveInfinity,
                FixedPointS26NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS26Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 26);
            return value switch
            {
                FixedPointS26Nan => double.NaN,
                FixedPointS26PositiveInf => double.PositiveInfinity,
                FixedPointS26NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS26Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 26);
            return value switch
            {
                FixedPointS26Nan => double.NaN,
                FixedPointS26PositiveInf => double.PositiveInfinity,
                FixedPointS26NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS26Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS26Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS26Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 26);
            var convertedValue = value switch
            {
                FixedPointS26Nan => double.NaN,
                FixedPointS26PositiveInf => double.PositiveInfinity,
                FixedPointS26NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS26Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 26, FixedPointS26Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 26, FixedPointS26NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 26, FixedPointS26PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS26Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 26, FixedPointS26PositiveInf);
                    break;
                case < FixedPointS26Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 26, FixedPointS26NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 26, intValue);
                    break;
            }
        }

        public static void SetFixedPointS26Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS26Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS26Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS26Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS26Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS26Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS26Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS26Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS27

        public const int FixedPointS27PositiveInf = 67108863;
        public const int FixedPointS27NegativeInf = -67108863;
        public const int FixedPointS27Nan = -67108864;
        public const int FixedPointS27Max = 67108862;
        public const int FixedPointS27Min = -67108862;

        public static double GetFixedPointS27Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 27);
            return value switch
            {
                FixedPointS27Nan => double.NaN,
                FixedPointS27PositiveInf => double.PositiveInfinity,
                FixedPointS27NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS27Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 27);
            return value switch
            {
                FixedPointS27Nan => double.NaN,
                FixedPointS27PositiveInf => double.PositiveInfinity,
                FixedPointS27NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS27Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 27);
            return value switch
            {
                FixedPointS27Nan => double.NaN,
                FixedPointS27PositiveInf => double.PositiveInfinity,
                FixedPointS27NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS27Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS27Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS27Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 27);
            var convertedValue = value switch
            {
                FixedPointS27Nan => double.NaN,
                FixedPointS27PositiveInf => double.PositiveInfinity,
                FixedPointS27NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS27Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 27, FixedPointS27Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 27, FixedPointS27NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 27, FixedPointS27PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS27Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 27, FixedPointS27PositiveInf);
                    break;
                case < FixedPointS27Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 27, FixedPointS27NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 27, intValue);
                    break;
            }
        }

        public static void SetFixedPointS27Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS27Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS27Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS27Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS27Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS27Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS27Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS27Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS28

        public const int FixedPointS28PositiveInf = 134217727;
        public const int FixedPointS28NegativeInf = -134217727;
        public const int FixedPointS28Nan = -134217728;
        public const int FixedPointS28Max = 134217726;
        public const int FixedPointS28Min = -134217726;

        public static double GetFixedPointS28Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 28);
            return value switch
            {
                FixedPointS28Nan => double.NaN,
                FixedPointS28PositiveInf => double.PositiveInfinity,
                FixedPointS28NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS28Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 28);
            return value switch
            {
                FixedPointS28Nan => double.NaN,
                FixedPointS28PositiveInf => double.PositiveInfinity,
                FixedPointS28NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS28Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 28);
            return value switch
            {
                FixedPointS28Nan => double.NaN,
                FixedPointS28PositiveInf => double.PositiveInfinity,
                FixedPointS28NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS28Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS28Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS28Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 28);
            var convertedValue = value switch
            {
                FixedPointS28Nan => double.NaN,
                FixedPointS28PositiveInf => double.PositiveInfinity,
                FixedPointS28NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS28Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 28, FixedPointS28Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 28, FixedPointS28NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 28, FixedPointS28PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS28Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 28, FixedPointS28PositiveInf);
                    break;
                case < FixedPointS28Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 28, FixedPointS28NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 28, intValue);
                    break;
            }
        }

        public static void SetFixedPointS28Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS28Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS28Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS28Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS28Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS28Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS28Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS28Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS29

        public const int FixedPointS29PositiveInf = 268435455;
        public const int FixedPointS29NegativeInf = -268435455;
        public const int FixedPointS29Nan = -268435456;
        public const int FixedPointS29Max = 268435454;
        public const int FixedPointS29Min = -268435454;

        public static double GetFixedPointS29Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 29);
            return value switch
            {
                FixedPointS29Nan => double.NaN,
                FixedPointS29PositiveInf => double.PositiveInfinity,
                FixedPointS29NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS29Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 29);
            return value switch
            {
                FixedPointS29Nan => double.NaN,
                FixedPointS29PositiveInf => double.PositiveInfinity,
                FixedPointS29NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS29Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 29);
            return value switch
            {
                FixedPointS29Nan => double.NaN,
                FixedPointS29PositiveInf => double.PositiveInfinity,
                FixedPointS29NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS29Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS29Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS29Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 29);
            var convertedValue = value switch
            {
                FixedPointS29Nan => double.NaN,
                FixedPointS29PositiveInf => double.PositiveInfinity,
                FixedPointS29NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS29Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 29, FixedPointS29Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 29, FixedPointS29NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 29, FixedPointS29PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS29Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 29, FixedPointS29PositiveInf);
                    break;
                case < FixedPointS29Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 29, FixedPointS29NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 29, intValue);
                    break;
            }
        }

        public static void SetFixedPointS29Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS29Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS29Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS29Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS29Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS29Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS29Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS29Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS30

        public const int FixedPointS30PositiveInf = 536870911;
        public const int FixedPointS30NegativeInf = -536870911;
        public const int FixedPointS30Nan = -536870912;
        public const int FixedPointS30Max = 536870910;
        public const int FixedPointS30Min = -536870910;

        public static double GetFixedPointS30Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 30);
            return value switch
            {
                FixedPointS30Nan => double.NaN,
                FixedPointS30PositiveInf => double.PositiveInfinity,
                FixedPointS30NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS30Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 30);
            return value switch
            {
                FixedPointS30Nan => double.NaN,
                FixedPointS30PositiveInf => double.PositiveInfinity,
                FixedPointS30NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS30Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 30);
            return value switch
            {
                FixedPointS30Nan => double.NaN,
                FixedPointS30PositiveInf => double.PositiveInfinity,
                FixedPointS30NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS30Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS30Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS30Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 30);
            var convertedValue = value switch
            {
                FixedPointS30Nan => double.NaN,
                FixedPointS30PositiveInf => double.PositiveInfinity,
                FixedPointS30NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS30Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 30, FixedPointS30Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 30, FixedPointS30NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 30, FixedPointS30PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS30Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 30, FixedPointS30PositiveInf);
                    break;
                case < FixedPointS30Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 30, FixedPointS30NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 30, intValue);
                    break;
            }
        }

        public static void SetFixedPointS30Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS30Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS30Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS30Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS30Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS30Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS30Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS30Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS31

        public const int FixedPointS31PositiveInf = 1073741823;
        public const int FixedPointS31NegativeInf = -1073741823;
        public const int FixedPointS31Nan = -1073741824;
        public const int FixedPointS31Max = 1073741822;
        public const int FixedPointS31Min = -1073741822;

        public static double GetFixedPointS31Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 31);
            return value switch
            {
                FixedPointS31Nan => double.NaN,
                FixedPointS31PositiveInf => double.PositiveInfinity,
                FixedPointS31NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS31Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 31);
            return value switch
            {
                FixedPointS31Nan => double.NaN,
                FixedPointS31PositiveInf => double.PositiveInfinity,
                FixedPointS31NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS31Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 31);
            return value switch
            {
                FixedPointS31Nan => double.NaN,
                FixedPointS31PositiveInf => double.PositiveInfinity,
                FixedPointS31NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS31Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS31Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS31Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 31);
            var convertedValue = value switch
            {
                FixedPointS31Nan => double.NaN,
                FixedPointS31PositiveInf => double.PositiveInfinity,
                FixedPointS31NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS31Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 31, FixedPointS31Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 31, FixedPointS31NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 31, FixedPointS31PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS31Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 31, FixedPointS31PositiveInf);
                    break;
                case < FixedPointS31Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 31, FixedPointS31NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 31, intValue);
                    break;
            }
        }

        public static void SetFixedPointS31Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS31Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS31Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS31Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS31Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS31Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS31Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS31Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion

        #region FixedPointS32

        public const int FixedPointS32PositiveInf = 2147483647;
        public const int FixedPointS32NegativeInf = -2147483647;
        public const int FixedPointS32Nan = -2147483648;
        public const int FixedPointS32Max = 2147483646;
        public const int FixedPointS32Min = -2147483646;

        public static double GetFixedPointS32Bit(ReadOnlySpan<byte> buffer, ref int bitIndex)
        {
            var value = GetBitS(buffer, ref bitIndex, 32);
            return value switch
            {
                FixedPointS32Nan => double.NaN,
                FixedPointS32PositiveInf => double.PositiveInfinity,
                FixedPointS32NegativeInf => double.NegativeInfinity,
                _ => value,
            };
        }

        public static double GetFixedPointS32Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 32);
            return value switch
            {
                FixedPointS32Nan => double.NaN,
                FixedPointS32PositiveInf => double.PositiveInfinity,
                FixedPointS32NegativeInf => double.NegativeInfinity,
                _ => value * fraction,
            };
        }

        public static double GetFixedPointS32Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset
        )
        {
            var value = GetBitS(buffer, ref bitIndex, 32);
            return value switch
            {
                FixedPointS32Nan => double.NaN,
                FixedPointS32PositiveInf => double.PositiveInfinity,
                FixedPointS32NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
        }

        public static double GetFixedPointS32Bit(
            ReadOnlySpan<byte> buffer,
            ref int bitIndex,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS32Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS32Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            var value = GetBitS(buffer, ref bitIndex, 32);
            var convertedValue = value switch
            {
                FixedPointS32Nan => double.NaN,
                FixedPointS32PositiveInf => double.PositiveInfinity,
                FixedPointS32NegativeInf => double.NegativeInfinity,
                _ => (value * fraction) + offset,
            };
            if (convertedValue > validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (convertedValue < validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            return convertedValue;
        }

        public static void SetFixedPointS32Bit(Span<byte> buffer, ref int bitIndex, double value)
        {
            if (double.IsNaN(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 32, FixedPointS32Nan);
                return;
            }

            if (double.IsNegativeInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 32, FixedPointS32NegativeInf);
                return;
            }
            if (double.IsPositiveInfinity(value))
            {
                SpanBitHelper.SetBitS(buffer, ref bitIndex, 32, FixedPointS32PositiveInf);
                return;
            }
            var intValue = (int)Math.Round(value);
            switch (intValue)
            {
                case > FixedPointS32Max:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 32, FixedPointS32PositiveInf);
                    break;
                case < FixedPointS32Min:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 32, FixedPointS32NegativeInf);
                    break;
                default:
                    SpanBitHelper.SetBitS(buffer, ref bitIndex, 32, intValue);
                    break;
            }
        }

        public static void SetFixedPointS32Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction
        )
        {
            SetFixedPointS32Bit(buffer, ref bitIndex, value / fraction);
        }

        public static void SetFixedPointS32Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset
        )
        {
            SetFixedPointS32Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        public static void SetFixedPointS32Bit(
            Span<byte> buffer,
            ref int bitIndex,
            double value,
            double fraction,
            double offset,
            double validateMax,
            double validateMin
        )
        {
            if (Math.Abs(validateMax - validateMin) <= fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMax > (FixedPointS32Max * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (validateMin < (FixedPointS32Min * fraction) + offset)
            {
                throw new ArgumentOutOfRangeException(nameof(validateMax));
            }

            if (value >= validateMax + fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            if (value <= validateMin - fraction)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            SetFixedPointS32Bit(buffer, ref bitIndex, (value - offset) / fraction);
        }

        #endregion
    }
}
