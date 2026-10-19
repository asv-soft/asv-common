using System;
using Xunit;

namespace Asv.Common.Test.Other
{
    public class UintBitArrayTest
    {
        [Fact]
        public void Test1()
        {
            var a = new UintBitArray(0, 32);
            a.SetBitU(5, 5, 0b1111_1);
            Assert.Equal(0b0000_0000_0000_0000_0000_0011_1110_0000U, a.Value);
        }
        [Fact]
        public void GetBitU_AllWidths_ReturnsOnlyRequestedBits()
        {
            var bits = new UintBitArray(uint.MaxValue, 32);
            for (var width = 0; width <= 32; width++)
            {
                var expected = (uint)((1UL << width) - 1);
                Assert.Equal(expected, bits.GetBitU(0, width));
            }
            Assert.Equal(0U, bits.GetBitU(32, 0));
            Assert.Equal(1U, bits.GetBitU(31, 1));
        }

        [Fact]
        public void SetBitU_AllWidths_PreservesOtherBits()
        {
            for (var width = 0; width <= 32; width++)
            {
                var mask = (uint)((1UL << width) - 1);
                var bits = new UintBitArray(uint.MaxValue, 32);
                bits.SetBitU(0, width, 0);
                Assert.Equal(~mask, bits.Value);
                bits.SetBitU(0, width, 0xA5A5A5A5U);
                Assert.Equal(~mask | (0xA5A5A5A5U & mask), bits.Value);
            }
            var shifted = new UintBitArray(0xFFFFFFFFU, 32);
            shifted.SetBitU(8, 24, 0);
            Assert.Equal(0xFFU, shifted.Value);
            shifted.SetBitU(32, 0, uint.MaxValue);
            Assert.Equal(0xFFU, shifted.Value);
        }

        [Theory]
        [InlineData(-1)]
        [InlineData(3)]
        [InlineData(4)]
        public void Indexer_InvalidIndex_ThrowsWithoutChangingValue(int index)
        {
            var bits = new UintBitArray(5, 3);
            Assert.Throws<ArgumentOutOfRangeException>(() => bits[index]);
            Assert.Throws<ArgumentOutOfRangeException>(() => bits[index] = true);
            Assert.Equal(5U, bits.Value);
            Assert.True(bits[0]);
            Assert.False(bits[1]);
            Assert.True(bits[2]);
        }

        [Theory]
        [InlineData(-1, 1)]
        [InlineData(0, -1)]
        [InlineData(1, 32)]
        [InlineData(32, 1)]
        [InlineData(int.MaxValue, 2)]
        public void BitRange_InvalidRange_ThrowsWithoutChangingValue(int index, int size)
        {
            var bits = new UintBitArray(5, 32);
            Assert.Throws<ArgumentOutOfRangeException>(() => bits.GetBitU(index, size));
            Assert.Throws<ArgumentOutOfRangeException>(() => bits.SetBitU(index, size, uint.MaxValue));
            Assert.Equal(5U, bits.Value);
        }
    }
}
