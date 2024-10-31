using System;
using FluentAssertions;
using Xunit;

namespace Asv.IO.Test
{
    public class SpanBitHelperTest
    {
        private const double Fraction1 = .00000001;
        private const double Offset1 = 0;

        #region FixedPointS3

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS3Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS3Max, SpanBitHelper.FixedPointS3Max)]
        [InlineData(SpanBitHelper.FixedPointS3Max / 2.0, SpanBitHelper.FixedPointS3Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS3Max / 3.0, SpanBitHelper.FixedPointS3Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS3Min, SpanBitHelper.FixedPointS3Min)]
        [InlineData(SpanBitHelper.FixedPointS3Min / 2.0, SpanBitHelper.FixedPointS3Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS3Min / 3.0, SpanBitHelper.FixedPointS3Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS3Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS3Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS3Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(3, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS3Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(3, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS3Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS3Max * Fraction1,
            SpanBitHelper.FixedPointS3Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS3Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS3Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS3Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS3Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS3Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS3Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS3Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS3Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS3Min * Fraction1,
            SpanBitHelper.FixedPointS3Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS3Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS3TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS3Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(3, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS3Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(3, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS3Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS3Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS3Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS3Max * Fraction1) / 2.0) + Offset1,
            ((SpanBitHelper.FixedPointS3Max * Fraction1) / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS3Max * Fraction1) / 3.0) + Offset1,
            ((SpanBitHelper.FixedPointS3Max * Fraction1) / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            ((SpanBitHelper.FixedPointS3Min * Fraction1) / 3.0) + Offset1,
            ((SpanBitHelper.FixedPointS3Min * Fraction1) / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS3Min * Fraction1) / 2.0) + Offset1,
            ((SpanBitHelper.FixedPointS3Min * Fraction1) / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS3Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS3Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS3Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS3TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS3Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(3, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS3Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(3, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        [InlineData(
            (-(6 * Fraction1)) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        public void FixedPointS3TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS3Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        [InlineData(
            (-(6 * Fraction1)) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        public void FixedPointS3TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS3Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS3Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS4

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS4Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS4Max, SpanBitHelper.FixedPointS4Max)]
        [InlineData(SpanBitHelper.FixedPointS4Max / 2.0, SpanBitHelper.FixedPointS4Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS4Max / 3.0, SpanBitHelper.FixedPointS4Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS4Min, SpanBitHelper.FixedPointS4Min)]
        [InlineData(SpanBitHelper.FixedPointS4Min / 2.0, SpanBitHelper.FixedPointS4Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS4Min / 3.0, SpanBitHelper.FixedPointS4Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS4Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS4Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS4Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(4, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS4Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(4, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS4Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS4Max * Fraction1,
            SpanBitHelper.FixedPointS4Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS4Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS4Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS4Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS4Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS4Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS4Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS4Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS4Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS4Min * Fraction1,
            SpanBitHelper.FixedPointS4Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS4Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS4TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS4Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(4, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS4Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(4, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS4Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS4Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS4Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS4Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS4Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS4Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS4Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS4Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS4Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS4Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS4Min * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS4Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS4Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS4Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS4TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS4Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(4, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS4Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(4, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        [InlineData(
            (-(6 * Fraction1)) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        public void FixedPointS4TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS4Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        [InlineData(
            (-(6 * Fraction1)) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        public void FixedPointS4TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS4Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS4Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS5

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS5Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS5Max, SpanBitHelper.FixedPointS5Max)]
        [InlineData(SpanBitHelper.FixedPointS5Max / 2.0, SpanBitHelper.FixedPointS5Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS5Max / 3.0, SpanBitHelper.FixedPointS5Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS5Min, SpanBitHelper.FixedPointS5Min)]
        [InlineData(SpanBitHelper.FixedPointS5Min / 2.0, SpanBitHelper.FixedPointS5Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS5Min / 3.0, SpanBitHelper.FixedPointS5Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS5Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS5Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS5Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(5, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS5Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(5, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS5Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS5Max * Fraction1,
            SpanBitHelper.FixedPointS5Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS5Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS5Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS5Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS5Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS5Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS5Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS5Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS5Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS5Min * Fraction1,
            SpanBitHelper.FixedPointS5Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS5Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS5TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS5Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(5, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS5Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(5, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS5Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS5Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS5Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS5Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS5Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS5Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS5Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS5Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS5Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS5Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS5Min * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS5Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS5Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS5Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS5TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS5Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(5, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS5Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(5, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        [InlineData(
            (-(6 * Fraction1)) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        public void FixedPointS5TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS5Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        [InlineData(
            (-(6 * Fraction1)) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        public void FixedPointS5TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS5Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS5Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS6

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS6Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS6Max, SpanBitHelper.FixedPointS6Max)]
        [InlineData(SpanBitHelper.FixedPointS6Max / 2.0, SpanBitHelper.FixedPointS6Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS6Max / 3.0, SpanBitHelper.FixedPointS6Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS6Min, SpanBitHelper.FixedPointS6Min)]
        [InlineData(SpanBitHelper.FixedPointS6Min / 2.0, SpanBitHelper.FixedPointS6Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS6Min / 3.0, SpanBitHelper.FixedPointS6Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS6Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS6Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS6Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(6, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS6Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(6, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS6Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS6Max * Fraction1,
            SpanBitHelper.FixedPointS6Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS6Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS6Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS6Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS6Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS6Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS6Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS6Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS6Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS6Min * Fraction1,
            SpanBitHelper.FixedPointS6Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS6Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS6TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS6Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(6, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS6Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(6, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS6Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS6Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS6Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS6Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS6Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS6Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS6Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS6Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS6Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS6Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS6Min * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS6Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS6Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS6Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS6TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS6Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(6, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS6Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(6, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        [InlineData(
            (-(6 * Fraction1)) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        public void FixedPointS6TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS6Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        [InlineData(
            (-(6 * Fraction1)) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        public void FixedPointS6TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS6Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS6Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS7

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS7Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS7Max, SpanBitHelper.FixedPointS7Max)]
        [InlineData(SpanBitHelper.FixedPointS7Max / 2.0, SpanBitHelper.FixedPointS7Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS7Max / 3.0, SpanBitHelper.FixedPointS7Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS7Min, SpanBitHelper.FixedPointS7Min)]
        [InlineData(SpanBitHelper.FixedPointS7Min / 2.0, SpanBitHelper.FixedPointS7Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS7Min / 3.0, SpanBitHelper.FixedPointS7Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS7Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS7Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS7Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(7, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS7Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(7, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS7Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS7Max * Fraction1,
            SpanBitHelper.FixedPointS7Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS7Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS7Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS7Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS7Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS7Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS7Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS7Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS7Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS7Min * Fraction1,
            SpanBitHelper.FixedPointS7Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS7Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS7TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS7Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(7, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS7Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(7, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS7Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS7Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS7Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS7Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS7Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS7Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS7Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS7Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS7Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS7Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS7Min * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS7Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS7Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS7Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS7TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS7Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(7, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS7Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(7, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        [InlineData(
            (-(6 * Fraction1)) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        public void FixedPointS7TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS7Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        [InlineData(
            (-(6 * Fraction1)) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        public void FixedPointS7TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS7Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS7Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS8

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS8Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS8Max, SpanBitHelper.FixedPointS8Max)]
        [InlineData(SpanBitHelper.FixedPointS8Max / 2.0, SpanBitHelper.FixedPointS8Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS8Max / 3.0, SpanBitHelper.FixedPointS8Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS8Min, SpanBitHelper.FixedPointS8Min)]
        [InlineData(SpanBitHelper.FixedPointS8Min / 2.0, SpanBitHelper.FixedPointS8Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS8Min / 3.0, SpanBitHelper.FixedPointS8Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS8Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS8Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS8Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(8, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS8Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(8, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS8Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS8Max * Fraction1,
            SpanBitHelper.FixedPointS8Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS8Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS8Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS8Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS8Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS8Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS8Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS8Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS8Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS8Min * Fraction1,
            SpanBitHelper.FixedPointS8Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS8Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS8TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS8Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(8, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS8Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(8, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS8Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS8Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS8Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS8Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS8Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS8Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS8Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS8Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS8Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS8Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS8Min * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS8Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS8Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS8Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS8TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS8Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(8, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS8Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(8, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        [InlineData(
            (-(6 * Fraction1)) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        public void FixedPointS8TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS8Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        [InlineData(
            (-(6 * Fraction1)) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        public void FixedPointS8TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS8Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS8Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS9

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS9Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS9Max, SpanBitHelper.FixedPointS9Max)]
        [InlineData(SpanBitHelper.FixedPointS9Max / 2.0, SpanBitHelper.FixedPointS9Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS9Max / 3.0, SpanBitHelper.FixedPointS9Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS9Min, SpanBitHelper.FixedPointS9Min)]
        [InlineData(SpanBitHelper.FixedPointS9Min / 2.0, SpanBitHelper.FixedPointS9Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS9Min / 3.0, SpanBitHelper.FixedPointS9Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS9Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS9Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS9Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(9, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS9Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(9, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS9Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS9Max * Fraction1,
            SpanBitHelper.FixedPointS9Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS9Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS9Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS9Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS9Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS9Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS9Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS9Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS9Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS9Min * Fraction1,
            SpanBitHelper.FixedPointS9Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS9Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS9TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS9Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(9, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS9Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(9, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS9Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS9Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS9Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS9Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS9Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS9Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS9Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS9Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS9Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS9Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS9Min * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS9Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS9Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS9Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS9TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS9Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(9, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS9Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(9, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        [InlineData(
            (-(6 * Fraction1)) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        public void FixedPointS9TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS9Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        [InlineData(
            (-(6 * Fraction1)) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        public void FixedPointS9TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS9Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS9Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS10

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS10Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS10Max, SpanBitHelper.FixedPointS10Max)]
        [InlineData(SpanBitHelper.FixedPointS10Max / 2.0, SpanBitHelper.FixedPointS10Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS10Max / 3.0, SpanBitHelper.FixedPointS10Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS10Min, SpanBitHelper.FixedPointS10Min)]
        [InlineData(SpanBitHelper.FixedPointS10Min / 2.0, SpanBitHelper.FixedPointS10Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS10Min / 3.0, SpanBitHelper.FixedPointS10Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS10Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS10Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS10Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(10, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS10Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(10, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS10Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS10Max * Fraction1,
            SpanBitHelper.FixedPointS10Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS10Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS10Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS10Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS10Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS10Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS10Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS10Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS10Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS10Min * Fraction1,
            SpanBitHelper.FixedPointS10Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS10Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS10TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS10Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(10, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS10Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(10, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS10Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS10Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS10Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS10Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS10Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS10Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS10Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS10Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS10Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS10Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS10Min * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS10Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS10Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS10Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS10TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS10Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(10, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS10Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(10, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        [InlineData(
            (-(6 * Fraction1)) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        public void FixedPointS10TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS10Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        [InlineData(
            (-(6 * Fraction1)) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        public void FixedPointS10TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS10Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS10Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS11

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS11Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS11Max, SpanBitHelper.FixedPointS11Max)]
        [InlineData(SpanBitHelper.FixedPointS11Max / 2.0, SpanBitHelper.FixedPointS11Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS11Max / 3.0, SpanBitHelper.FixedPointS11Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS11Min, SpanBitHelper.FixedPointS11Min)]
        [InlineData(SpanBitHelper.FixedPointS11Min / 2.0, SpanBitHelper.FixedPointS11Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS11Min / 3.0, SpanBitHelper.FixedPointS11Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS11Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS11Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS11Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(11, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS11Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(11, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS11Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS11Max * Fraction1,
            SpanBitHelper.FixedPointS11Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS11Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS11Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS11Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS11Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS11Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS11Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS11Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS11Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS11Min * Fraction1,
            SpanBitHelper.FixedPointS11Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS11Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS11TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS11Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(11, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS11Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(11, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS11Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS11Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS11Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS11Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS11Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS11Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS11Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS11Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS11Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS11Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS11Min * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS11Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS11Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS11Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS11TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS11Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(11, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS11Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(11, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        [InlineData(
            (-(6 * Fraction1)) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        public void FixedPointS11TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS11Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        [InlineData(
            (-(6 * Fraction1)) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        public void FixedPointS11TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS11Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS11Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS12

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS12Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS12Max, SpanBitHelper.FixedPointS12Max)]
        [InlineData(SpanBitHelper.FixedPointS12Max / 2.0, SpanBitHelper.FixedPointS12Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS12Max / 3.0, SpanBitHelper.FixedPointS12Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS12Min, SpanBitHelper.FixedPointS12Min)]
        [InlineData(SpanBitHelper.FixedPointS12Min / 2.0, SpanBitHelper.FixedPointS12Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS12Min / 3.0, SpanBitHelper.FixedPointS12Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS12Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS12Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS12Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(12, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS12Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(12, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS12Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS12Max * Fraction1,
            SpanBitHelper.FixedPointS12Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS12Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS12Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS12Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS12Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS12Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS12Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS12Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS12Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS12Min * Fraction1,
            SpanBitHelper.FixedPointS12Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS12Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS12TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS12Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(12, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS12Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(12, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS12Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS12Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS12Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS12Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS12Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS12Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS12Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS12Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS12Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS12Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS12Min * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS12Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS12Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS12Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS12TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS12Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(12, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS12Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(12, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        [InlineData(
            (-(6 * Fraction1)) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        public void FixedPointS12TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS12Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        [InlineData(
            (-(6 * Fraction1)) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        public void FixedPointS12TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS12Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS12Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS13

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS13Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS13Max, SpanBitHelper.FixedPointS13Max)]
        [InlineData(SpanBitHelper.FixedPointS13Max / 2.0, SpanBitHelper.FixedPointS13Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS13Max / 3.0, SpanBitHelper.FixedPointS13Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS13Min, SpanBitHelper.FixedPointS13Min)]
        [InlineData(SpanBitHelper.FixedPointS13Min / 2.0, SpanBitHelper.FixedPointS13Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS13Min / 3.0, SpanBitHelper.FixedPointS13Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS13Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS13Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS13Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(13, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS13Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(13, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS13Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS13Max * Fraction1,
            SpanBitHelper.FixedPointS13Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS13Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS13Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS13Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS13Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS13Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS13Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS13Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS13Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS13Min * Fraction1,
            SpanBitHelper.FixedPointS13Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS13Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS13TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS13Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(13, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS13Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(13, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS13Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS13Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS13Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS13Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS13Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS13Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS13Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS13Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS13Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS13Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS13Min * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS13Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS13Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS13Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS13TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS13Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(13, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS13Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(13, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        [InlineData(
            (-(6 * Fraction1)) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        public void FixedPointS13TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS13Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        [InlineData(
            (-(6 * Fraction1)) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        public void FixedPointS13TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS13Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS13Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS14

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS14Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS14Max, SpanBitHelper.FixedPointS14Max)]
        [InlineData(SpanBitHelper.FixedPointS14Max / 2.0, SpanBitHelper.FixedPointS14Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS14Max / 3.0, SpanBitHelper.FixedPointS14Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS14Min, SpanBitHelper.FixedPointS14Min)]
        [InlineData(SpanBitHelper.FixedPointS14Min / 2.0, SpanBitHelper.FixedPointS14Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS14Min / 3.0, SpanBitHelper.FixedPointS14Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS14Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS14Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS14Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(14, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS14Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(14, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS14Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS14Max * Fraction1,
            SpanBitHelper.FixedPointS14Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS14Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS14Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS14Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS14Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS14Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS14Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS14Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS14Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS14Min * Fraction1,
            SpanBitHelper.FixedPointS14Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS14Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS14TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS14Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(14, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS14Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(14, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS14Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS14Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS14Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS14Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS14Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS14Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS14Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS14Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS14Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS14Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS14Min * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS14Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS14Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS14Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS14TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS14Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(14, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS14Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(14, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS14TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS14Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        [InlineData(
            (-(6 * Fraction1)) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        public void FixedPointS14TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS14Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS14Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS15

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS15Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS15Max, SpanBitHelper.FixedPointS15Max)]
        [InlineData(SpanBitHelper.FixedPointS15Max / 2.0, SpanBitHelper.FixedPointS15Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS15Max / 3.0, SpanBitHelper.FixedPointS15Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS15Min, SpanBitHelper.FixedPointS15Min)]
        [InlineData(SpanBitHelper.FixedPointS15Min / 2.0, SpanBitHelper.FixedPointS15Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS15Min / 3.0, SpanBitHelper.FixedPointS15Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS15Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS15Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS15Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(15, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS15Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(15, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS15Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS15Max * Fraction1,
            SpanBitHelper.FixedPointS15Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS15Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS15Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS15Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS15Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS15Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS15Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS15Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS15Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS15Min * Fraction1,
            SpanBitHelper.FixedPointS15Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS15Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS15TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS15Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(15, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS15Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(15, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS15Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS15Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS15Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS15Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS15Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS15Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS15Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS15Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS15Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS15Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS15Min * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS15Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS15Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS15Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS15TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS15Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(15, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS15Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(15, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        [InlineData(
            (-(6 * Fraction1)) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-(Fraction1 * 5)) + Offset1
        )]
        public void FixedPointS15TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS15Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS15TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS15Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS15Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS16

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS16Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS16Max, SpanBitHelper.FixedPointS16Max)]
        [InlineData(SpanBitHelper.FixedPointS16Max / 2.0, SpanBitHelper.FixedPointS16Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS16Max / 3.0, SpanBitHelper.FixedPointS16Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS16Min, SpanBitHelper.FixedPointS16Min)]
        [InlineData(SpanBitHelper.FixedPointS16Min / 2.0, SpanBitHelper.FixedPointS16Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS16Min / 3.0, SpanBitHelper.FixedPointS16Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS16Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS16Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS16Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(16, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS16Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(16, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS16Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS16Max * Fraction1,
            SpanBitHelper.FixedPointS16Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS16Max * Fraction1) / 2.0,
            (SpanBitHelper.FixedPointS16Max * Fraction1) / 2.0,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS16Max * Fraction1) / 3.0,
            (SpanBitHelper.FixedPointS16Max * Fraction1) / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            (SpanBitHelper.FixedPointS16Min * Fraction1) / 2.0,
            (SpanBitHelper.FixedPointS16Min * Fraction1) / 2.0,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS16Min * Fraction1) / 3.0,
            (SpanBitHelper.FixedPointS16Min * Fraction1) / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS16Min * Fraction1,
            SpanBitHelper.FixedPointS16Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS16Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS16TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS16Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(16, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS16Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(16, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS16Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS16Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS16Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS16Max * Fraction1) / 2.0) + Offset1,
            ((SpanBitHelper.FixedPointS16Max * Fraction1) / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS16Max * Fraction1) / 3.0) + Offset1,
            ((SpanBitHelper.FixedPointS16Max * Fraction1) / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            ((SpanBitHelper.FixedPointS16Min * Fraction1) / 3.0) + Offset1,
            ((SpanBitHelper.FixedPointS16Min * Fraction1) / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS16Min * Fraction1) / 2.0) + Offset1,
            ((SpanBitHelper.FixedPointS16Min * Fraction1) / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS16Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS16Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS16Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS16TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS16Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(16, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS16Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(16, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS16TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS16Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS16TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS16Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS16Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS17

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS17Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS17Max, SpanBitHelper.FixedPointS17Max)]
        [InlineData(SpanBitHelper.FixedPointS17Max / 2.0, SpanBitHelper.FixedPointS17Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS17Max / 3.0, SpanBitHelper.FixedPointS17Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS17Min, SpanBitHelper.FixedPointS17Min)]
        [InlineData(SpanBitHelper.FixedPointS17Min / 2.0, SpanBitHelper.FixedPointS17Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS17Min / 3.0, SpanBitHelper.FixedPointS17Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS17Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS17Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS17Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(17, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS17Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(17, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS17Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS17Max * Fraction1,
            SpanBitHelper.FixedPointS17Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS17Max * Fraction1) / 2.0,
            (SpanBitHelper.FixedPointS17Max * Fraction1) / 2.0,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS17Max * Fraction1) / 3.0,
            SpanBitHelper.FixedPointS17Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS17Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS17Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS17Min * Fraction1) / 3.0,
            SpanBitHelper.FixedPointS17Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS17Min * Fraction1,
            SpanBitHelper.FixedPointS17Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS17Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS17TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS17Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(17, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS17Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(17, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS17Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS17Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS17Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS17Max * Fraction1) / 2.0) + Offset1,
            ((SpanBitHelper.FixedPointS17Max * Fraction1) / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS17Max * Fraction1) / 3.0) + Offset1,
            ((SpanBitHelper.FixedPointS17Max * Fraction1) / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            ((SpanBitHelper.FixedPointS17Min * Fraction1) / 3.0) + Offset1,
            ((SpanBitHelper.FixedPointS17Min * Fraction1) / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS17Min * Fraction1) / 2.0) + Offset1,
            ((SpanBitHelper.FixedPointS17Min * Fraction1) / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS17Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS17Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS17Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS17TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS17Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(17, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS17Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(17, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS17TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS17Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS17TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS17Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS17Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS18

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS18Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS18Max, SpanBitHelper.FixedPointS18Max)]
        [InlineData(SpanBitHelper.FixedPointS18Max / 2.0, SpanBitHelper.FixedPointS18Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS18Max / 3.0, SpanBitHelper.FixedPointS18Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS18Min, SpanBitHelper.FixedPointS18Min)]
        [InlineData(SpanBitHelper.FixedPointS18Min / 2.0, SpanBitHelper.FixedPointS18Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS18Min / 3.0, SpanBitHelper.FixedPointS18Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS18Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS18Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS18Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(18, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS18Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(18, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS18Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS18Max * Fraction1,
            SpanBitHelper.FixedPointS18Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS18Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS18Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS18Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS18Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS18Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS18Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS18Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS18Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS18Min * Fraction1,
            SpanBitHelper.FixedPointS18Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS18Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS18TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS18Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(18, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS18Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(18, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS18Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS18Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS18Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS18Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS18Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS18Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS18Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS18Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS18Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS18Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS18Min * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS18Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS18Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS18Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS18TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS18Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(18, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS18Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(18, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS18TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS18Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS18TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS18Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS18Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS19

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS19Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS19Max, SpanBitHelper.FixedPointS19Max)]
        [InlineData(SpanBitHelper.FixedPointS19Max / 2.0, SpanBitHelper.FixedPointS19Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS19Max / 3.0, SpanBitHelper.FixedPointS19Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS19Min, SpanBitHelper.FixedPointS19Min)]
        [InlineData(SpanBitHelper.FixedPointS19Min / 2.0, SpanBitHelper.FixedPointS19Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS19Min / 3.0, SpanBitHelper.FixedPointS19Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS19Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS19Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS19Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(19, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS19Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(19, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS19Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS19Max * Fraction1,
            SpanBitHelper.FixedPointS19Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS19Max * Fraction1) / 2.0,
            (SpanBitHelper.FixedPointS19Max * Fraction1) / 2.0,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS19Max * Fraction1) / 3.0,
            (SpanBitHelper.FixedPointS19Max * Fraction1) / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS19Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS19Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS19Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS19Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS19Min * Fraction1,
            SpanBitHelper.FixedPointS19Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS19Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS19TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS19Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(19, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS19Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(19, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS19Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS19Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS19Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS19Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS19Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS19Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS19Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS19Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS19Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS19Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS19Min * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS19Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS19Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS19Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS19TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS19Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(19, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS19Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(19, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS19TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS19Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS19TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS19Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS19Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS20

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS20Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS20Max, SpanBitHelper.FixedPointS20Max)]
        [InlineData(SpanBitHelper.FixedPointS20Max / 2.0, SpanBitHelper.FixedPointS20Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS20Max / 3.0, SpanBitHelper.FixedPointS20Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS20Min, SpanBitHelper.FixedPointS20Min)]
        [InlineData(SpanBitHelper.FixedPointS20Min / 2.0, SpanBitHelper.FixedPointS20Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS20Min / 3.0, SpanBitHelper.FixedPointS20Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS20Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS20Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS20Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(20, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS20Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(20, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS20Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS20Max * Fraction1,
            SpanBitHelper.FixedPointS20Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS20Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS20Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS20Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS20Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS20Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS20Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS20Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS20Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS20Min * Fraction1,
            SpanBitHelper.FixedPointS20Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS20Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS20TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS20Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(20, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS20Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(20, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS20Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS20Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS20Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS20Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS20Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS20Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS20Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS20Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS20Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS20Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS20Min * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS20Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS20Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS20Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS20TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS20Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(20, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS20Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(20, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS20TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS20Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS20TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS20Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS20Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS21

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS21Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS21Max, SpanBitHelper.FixedPointS21Max)]
        [InlineData(SpanBitHelper.FixedPointS21Max / 2.0, SpanBitHelper.FixedPointS21Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS21Max / 3.0, SpanBitHelper.FixedPointS21Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS21Min, SpanBitHelper.FixedPointS21Min)]
        [InlineData(SpanBitHelper.FixedPointS21Min / 2.0, SpanBitHelper.FixedPointS21Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS21Min / 3.0, SpanBitHelper.FixedPointS21Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS21Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS21Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS21Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(21, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS21Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(21, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS21Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS21Max * Fraction1,
            SpanBitHelper.FixedPointS21Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS21Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS21Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS21Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS21Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS21Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS21Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS21Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS21Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS21Min * Fraction1,
            SpanBitHelper.FixedPointS21Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS21Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS21TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS21Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(21, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS21Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(21, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS21Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS21Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS21Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS21Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS21Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS21Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS21Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS21Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS21Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS21Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS21Min * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS21Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS21Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS21Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS21TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS21Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(21, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS21Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(21, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS21TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS21Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS21TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS21Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS21Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS22

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS22Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS22Max, SpanBitHelper.FixedPointS22Max)]
        [InlineData(SpanBitHelper.FixedPointS22Max / 2.0, SpanBitHelper.FixedPointS22Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS22Max / 3.0, SpanBitHelper.FixedPointS22Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS22Min, SpanBitHelper.FixedPointS22Min)]
        [InlineData(SpanBitHelper.FixedPointS22Min / 2.0, SpanBitHelper.FixedPointS22Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS22Min / 3.0, SpanBitHelper.FixedPointS22Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS22Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS22Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS22Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(22, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS22Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(22, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS22Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS22Max * Fraction1,
            SpanBitHelper.FixedPointS22Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS22Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS22Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS22Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS22Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS22Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS22Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS22Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS22Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS22Min * Fraction1,
            SpanBitHelper.FixedPointS22Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS22Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS22TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS22Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(22, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS22Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(22, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS22Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS22Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS22Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS22Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS22Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS22Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS22Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS22Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS22Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS22Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS22Min * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS22Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS22Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS22Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS22TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS22Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(22, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS22Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(22, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS22TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS22Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS22TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS22Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS22Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS23

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS23Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS23Max, SpanBitHelper.FixedPointS23Max)]
        [InlineData(SpanBitHelper.FixedPointS23Max / 2.0, SpanBitHelper.FixedPointS23Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS23Max / 3.0, SpanBitHelper.FixedPointS23Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS23Min, SpanBitHelper.FixedPointS23Min)]
        [InlineData(SpanBitHelper.FixedPointS23Min / 2.0, SpanBitHelper.FixedPointS23Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS23Min / 3.0, SpanBitHelper.FixedPointS23Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS23Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS23Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS23Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(23, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS23Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(23, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS23Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS23Max * Fraction1,
            SpanBitHelper.FixedPointS23Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS23Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS23Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS23Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS23Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS23Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS23Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS23Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS23Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS23Min * Fraction1,
            SpanBitHelper.FixedPointS23Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS23Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS23TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS23Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(23, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS23Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(23, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS23Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS23Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS23Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS23Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS23Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS23Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS23Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS23Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS23Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS23Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS23Min * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS23Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS23Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS23Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS23TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS23Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(23, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS23Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(23, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS23TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS23Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS23TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS23Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS23Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS24

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS24Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS24Max, SpanBitHelper.FixedPointS24Max)]
        [InlineData(SpanBitHelper.FixedPointS24Max / 2.0, SpanBitHelper.FixedPointS24Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS24Max / 3.0, SpanBitHelper.FixedPointS24Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS24Min, SpanBitHelper.FixedPointS24Min)]
        [InlineData(SpanBitHelper.FixedPointS24Min / 2.0, SpanBitHelper.FixedPointS24Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS24Min / 3.0, SpanBitHelper.FixedPointS24Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS24Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS24Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS24Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(24, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS24Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(24, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS24Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS24Max * Fraction1,
            SpanBitHelper.FixedPointS24Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS24Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS24Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS24Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS24Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS24Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS24Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS24Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS24Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS24Min * Fraction1,
            SpanBitHelper.FixedPointS24Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS24Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS24TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS24Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(24, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS24Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(24, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS24Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS24Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS24Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS24Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS24Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS24Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS24Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS24Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS24Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS24Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS24Min * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS24Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS24Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS24Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS24TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS24Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(24, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS24Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(24, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS24TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS24Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS24TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS24Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS24Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS25

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS25Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS25Max, SpanBitHelper.FixedPointS25Max)]
        [InlineData(SpanBitHelper.FixedPointS25Max / 2.0, SpanBitHelper.FixedPointS25Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS25Max / 3.0, SpanBitHelper.FixedPointS25Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS25Min, SpanBitHelper.FixedPointS25Min)]
        [InlineData(SpanBitHelper.FixedPointS25Min / 2.0, SpanBitHelper.FixedPointS25Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS25Min / 3.0, SpanBitHelper.FixedPointS25Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS25Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS25Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS25Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(25, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS25Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(25, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS25Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS25Max * Fraction1,
            SpanBitHelper.FixedPointS25Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS25Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS25Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS25Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS25Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS25Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS25Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS25Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS25Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS25Min * Fraction1,
            SpanBitHelper.FixedPointS25Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS25Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS25TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS25Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(25, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS25Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(25, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS25Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS25Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS25Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS25Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS25Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS25Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS25Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS25Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS25Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS25Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS25Min * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS25Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS25Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS25Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS25TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS25Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(25, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS25Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(25, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS25TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS25Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS25TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS25Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS25Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS26

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS26Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS26Max, SpanBitHelper.FixedPointS26Max)]
        [InlineData(SpanBitHelper.FixedPointS26Max / 2.0, SpanBitHelper.FixedPointS26Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS26Max / 3.0, SpanBitHelper.FixedPointS26Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS26Min, SpanBitHelper.FixedPointS26Min)]
        [InlineData(SpanBitHelper.FixedPointS26Min / 2.0, SpanBitHelper.FixedPointS26Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS26Min / 3.0, SpanBitHelper.FixedPointS26Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS26Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS26Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS26Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(26, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS26Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(26, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS26Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS26Max * Fraction1,
            SpanBitHelper.FixedPointS26Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS26Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS26Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS26Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS26Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS26Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS26Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS26Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS26Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS26Min * Fraction1,
            SpanBitHelper.FixedPointS26Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS26Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS26TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS26Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(26, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS26Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(26, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS26Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS26Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS26Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS26Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS26Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS26Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS26Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS26Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS26Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS26Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS26Min * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS26Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS26Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS26Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS26TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS26Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(26, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS26Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(26, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS26TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS26Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS26TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS26Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS26Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS27

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS27Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS27Max, SpanBitHelper.FixedPointS27Max)]
        [InlineData(SpanBitHelper.FixedPointS27Max / 2.0, SpanBitHelper.FixedPointS27Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS27Max / 3.0, SpanBitHelper.FixedPointS27Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS27Min, SpanBitHelper.FixedPointS27Min)]
        [InlineData(SpanBitHelper.FixedPointS27Min / 2.0, SpanBitHelper.FixedPointS27Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS27Min / 3.0, SpanBitHelper.FixedPointS27Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS27Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS27Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS27Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(27, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS27Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(27, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS27Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS27Max * Fraction1,
            SpanBitHelper.FixedPointS27Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS27Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS27Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS27Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS27Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS27Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS27Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS27Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS27Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS27Min * Fraction1,
            SpanBitHelper.FixedPointS27Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS27Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS27TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS27Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(27, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS27Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(27, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS27Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS27Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS27Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS27Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS27Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS27Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS27Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS27Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS27Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS27Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS27Min * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS27Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS27Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS27Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS27TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS27Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(27, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS27Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(27, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS27TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS27Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS27TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS27Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS27Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS28

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS28Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS28Max, SpanBitHelper.FixedPointS28Max)]
        [InlineData(SpanBitHelper.FixedPointS28Max / 2.0, SpanBitHelper.FixedPointS28Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS28Max / 3.0, SpanBitHelper.FixedPointS28Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS28Min, SpanBitHelper.FixedPointS28Min)]
        [InlineData(SpanBitHelper.FixedPointS28Min / 2.0, SpanBitHelper.FixedPointS28Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS28Min / 3.0, SpanBitHelper.FixedPointS28Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS28Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS28Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS28Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(28, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS28Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(28, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS28Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS28Max * Fraction1,
            SpanBitHelper.FixedPointS28Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS28Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS28Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS28Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS28Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS28Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS28Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS28Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS28Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS28Min * Fraction1,
            SpanBitHelper.FixedPointS28Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS28Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS28TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS28Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(28, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS28Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(28, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS28Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS28Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS28Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS28Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS28Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS28Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS28Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS28Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS28Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS28Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS28Min * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS28Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS28Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS28Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS28TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS28Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(28, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS28Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(28, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS28TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS28Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS28TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS28Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS28Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS29

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS29Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS29Max, SpanBitHelper.FixedPointS29Max)]
        [InlineData(SpanBitHelper.FixedPointS29Max / 2.0, SpanBitHelper.FixedPointS29Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS29Max / 3.0, SpanBitHelper.FixedPointS29Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS29Min, SpanBitHelper.FixedPointS29Min)]
        [InlineData(SpanBitHelper.FixedPointS29Min / 2.0, SpanBitHelper.FixedPointS29Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS29Min / 3.0, SpanBitHelper.FixedPointS29Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS29Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS29Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS29Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(29, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS29Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(29, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS29Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS29Max * Fraction1,
            SpanBitHelper.FixedPointS29Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS29Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS29Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS29Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS29Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS29Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS29Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS29Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS29Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS29Min * Fraction1,
            SpanBitHelper.FixedPointS29Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS29Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS29TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS29Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(29, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS29Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(29, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS29Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS29Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS29Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS29Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS29Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS29Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS29Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS29Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS29Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS29Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS29Min * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS29Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS29Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS29Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS29TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS29Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(29, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS29Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(29, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS29TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS29Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS29TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS29Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS29Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS30

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS30Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS30Max, SpanBitHelper.FixedPointS30Max)]
        [InlineData(SpanBitHelper.FixedPointS30Max / 2.0, SpanBitHelper.FixedPointS30Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS30Max / 3.0, SpanBitHelper.FixedPointS30Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS30Min, SpanBitHelper.FixedPointS30Min)]
        [InlineData(SpanBitHelper.FixedPointS30Min / 2.0, SpanBitHelper.FixedPointS30Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS30Min / 3.0, SpanBitHelper.FixedPointS30Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS30Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS30Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS30Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(30, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS30Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(30, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS30Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS30Max * Fraction1,
            SpanBitHelper.FixedPointS30Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS30Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS30Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS30Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS30Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS30Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS30Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS30Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS30Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS30Min * Fraction1,
            SpanBitHelper.FixedPointS30Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS30Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS30TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS30Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(30, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS30Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(30, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS30Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS30Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS30Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS30Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS30Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS30Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS30Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS30Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS30Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS30Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS30Min * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS30Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS30Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS30Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS30TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS30Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(30, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS30Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(30, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS30TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS30Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS30TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS30Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS30Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS31

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS31Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS31Max, SpanBitHelper.FixedPointS31Max)]
        [InlineData(SpanBitHelper.FixedPointS31Max / 2.0, SpanBitHelper.FixedPointS31Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS31Max / 3.0, SpanBitHelper.FixedPointS31Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS31Min, SpanBitHelper.FixedPointS31Min)]
        [InlineData(SpanBitHelper.FixedPointS31Min / 2.0, SpanBitHelper.FixedPointS31Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS31Min / 3.0, SpanBitHelper.FixedPointS31Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS31Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS31Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS31Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(31, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS31Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(31, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS31Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS31Max * Fraction1,
            SpanBitHelper.FixedPointS31Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS31Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS31Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS31Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS31Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS31Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS31Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS31Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS31Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS31Min * Fraction1,
            SpanBitHelper.FixedPointS31Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS31Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS31TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS31Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(31, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS31Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(31, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS31Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS31Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS31Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS31Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS31Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS31Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS31Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS31Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS31Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS31Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS31Min * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS31Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS31Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS31Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS31TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS31Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(31, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS31Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(31, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS31TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS31Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS31TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS31Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS31Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion

        #region FixedPointS32

        [Theory]
        [InlineData(SpanBitHelper.FixedPointS32Max + 1, double.PositiveInfinity)]
        [InlineData(SpanBitHelper.FixedPointS32Max, SpanBitHelper.FixedPointS32Max)]
        [InlineData(SpanBitHelper.FixedPointS32Max / 2.0, SpanBitHelper.FixedPointS32Max / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS32Max / 3.0, SpanBitHelper.FixedPointS32Max / 3.0)]
        [InlineData(0.0, 0.0)]
        [InlineData(SpanBitHelper.FixedPointS32Min, SpanBitHelper.FixedPointS32Min)]
        [InlineData(SpanBitHelper.FixedPointS32Min / 2.0, SpanBitHelper.FixedPointS32Min / 2.0)]
        [InlineData(SpanBitHelper.FixedPointS32Min / 3.0, SpanBitHelper.FixedPointS32Min / 3.0)]
        [InlineData(SpanBitHelper.FixedPointS32Min - 1, double.NegativeInfinity)]
        [InlineData(double.NaN, double.NaN)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity)]
        public void FixedPointS32Test(double wrote, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS32Bit(writeSpan, ref bitIndex, wrote);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(32, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS32Bit(readSpan, ref bitIndex);

            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, 1.0);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(32, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (SpanBitHelper.FixedPointS32Max + 1) * Fraction1,
            double.PositiveInfinity,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS32Max * Fraction1,
            SpanBitHelper.FixedPointS32Max * Fraction1,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS32Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS32Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS32Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS32Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS32Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS32Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS32Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS32Min * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS32Min * Fraction1,
            SpanBitHelper.FixedPointS32Min * Fraction1,
            Fraction1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS32Min - 1) * Fraction1,
            double.NegativeInfinity,
            Fraction1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1)]
        public void FixedPointS32TestFraction(double wrote, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS32Bit(writeSpan, ref bitIndex, wrote, fraction);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(32, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS32Bit(readSpan, ref bitIndex, fraction);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(32, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            ((SpanBitHelper.FixedPointS32Max + 1) * Fraction1) + Offset1,
            double.PositiveInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS32Max * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS32Max * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS32Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS32Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS32Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS32Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS32Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS32Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS32Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS32Min * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS32Min * Fraction1) + Offset1,
            (SpanBitHelper.FixedPointS32Min * Fraction1) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            ((SpanBitHelper.FixedPointS32Min - 1) * Fraction1) + Offset1,
            double.NegativeInfinity,
            Fraction1,
            Offset1
        )]
        [InlineData(double.NaN, double.NaN, Fraction1, Offset1)]
        [InlineData(double.PositiveInfinity, double.PositiveInfinity, Fraction1, Offset1)]
        [InlineData(double.NegativeInfinity, double.NegativeInfinity, Fraction1, Offset1)]
        public void FixedPointS32TestFractionWithOffset(
            double wrote,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS32Bit(writeSpan, ref bitIndex, wrote, fraction, offset);
            Assert.Equal(data.Length, writeSpan.Length); // Check thant didn't slice span
            Assert.Equal(32, bitIndex); // check correct bit length

            bitIndex = 0;
            var value = SpanBitHelper.GetFixedPointS32Bit(readSpan, ref bitIndex, fraction, offset);
            if (double.IsNaN(expected))
            {
                Assert.Equal(expected, value);
            }
            else
            {
                value.Should().BeApproximately(expected, fraction);
            }

            Assert.Equal(data.Length, readSpan.Length); // Check thant didn't slice span
            Assert.Equal(32, bitIndex); // check correct bit length
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS32TestMaxMinSetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS32Bit(
                    writeSpan,
                    ref bitIndex,
                    wrote,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        [Theory]
        [InlineData(
            (6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        [InlineData(
            -(6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            -(Fraction1 * 5) + Offset1
        )]
        public void FixedPointS32TestMaxMinGetError(
            double wrote,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS32Bit(writeSpan, ref bitIndex, wrote, fraction, offset);

            bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var readSpan = new ReadOnlySpan<byte>(data);
                SpanBitHelper.GetFixedPointS32Bit(
                    readSpan,
                    ref bitIndex,
                    fraction,
                    offset,
                    max,
                    min
                );
            });
        }

        #endregion
    }
}
