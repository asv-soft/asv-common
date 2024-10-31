using System;
using FluentAssertions;
using Xunit;

namespace Asv.IO.Test
{
    public class SpanBitHelperTest2
    {
        public const double Fraction1 = .00000001;
        public const double Offset1 = 0;

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
        public void FixedPointS16Test(double writed, double expected)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS16Bit(writeSpan, ref bitIndex, writed);
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
            SpanBitHelper.FixedPointS16Max * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS16Max * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS16Max * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS16Max * Fraction1 / 3.0,
            Fraction1
        )]
        [InlineData(0, 0, Fraction1)]
        [InlineData(
            SpanBitHelper.FixedPointS16Min * Fraction1 / 2.0,
            SpanBitHelper.FixedPointS16Min * Fraction1 / 2.0,
            Fraction1
        )]
        [InlineData(
            SpanBitHelper.FixedPointS16Min * Fraction1 / 3.0,
            SpanBitHelper.FixedPointS16Min * Fraction1 / 3.0,
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
        public void FixedPointS16TestFraction(double writed, double expected, double fraction)
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            int bitIndex = 0;
            SpanBitHelper.SetFixedPointS16Bit(writeSpan, ref bitIndex, writed, fraction);
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
            (SpanBitHelper.FixedPointS16Max * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS16Max * Fraction1 / 2.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS16Max * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS16Max * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(0 + Offset1, 0 + Offset1, Fraction1, Offset1)]
        [InlineData(
            (SpanBitHelper.FixedPointS16Min * Fraction1 / 3.0) + Offset1,
            (SpanBitHelper.FixedPointS16Min * Fraction1 / 3.0) + Offset1,
            Fraction1,
            Offset1
        )]
        [InlineData(
            (SpanBitHelper.FixedPointS16Min * Fraction1 / 2.0) + Offset1,
            (SpanBitHelper.FixedPointS16Min * Fraction1 / 2.0) + Offset1,
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
            double writed,
            double expected,
            double fraction,
            double offset
        )
        {
            var data = new byte[256];
            var writeSpan = new Span<byte>(data);
            var readSpan = new ReadOnlySpan<byte>(data);
            var bitIndex = 0;
            SpanBitHelper.SetFixedPointS16Bit(writeSpan, ref bitIndex, writed, fraction, offset);
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
            (-Fraction1 * 5) + Offset1
        )]
        [InlineData(
            (-6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-Fraction1 * 5) + Offset1
        )]
        public void FixedPointS16TestMaxMinSetError(
            double writed,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            var bitIndex = 0;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var writeSpan = new Span<byte>(data);
                SpanBitHelper.SetFixedPointS16Bit(
                    writeSpan,
                    ref bitIndex,
                    writed,
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
            (-Fraction1 * 5) + Offset1
        )]
        [InlineData(
            (-6 * Fraction1) + Offset1,
            Fraction1,
            Offset1,
            (Fraction1 * 5) + Offset1,
            (-Fraction1 * 5) + Offset1
        )]
        public void FixedPointS16TestMaxMinGetError(
            double writed,
            double fraction,
            double offset,
            double max,
            double min
        )
        {
            var data = new byte[256];

            int bitIndex = 0;
            var writeSpan = new Span<byte>(data);
            SpanBitHelper.SetFixedPointS16Bit(writeSpan, ref bitIndex, writed, fraction, offset);

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
    }
}
