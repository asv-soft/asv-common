using Xunit;

namespace Asv.Common.Test
{
    public class UintBitArrayTest
    {
        [Fact]
        public void Test1()
        {
            var a = new UintBitArray(0, 32);
            a.SetBitU(5,5,0b1111_1);
            Assert.Equal(a.Value, (uint)0b0000_0000_0000_0000_0000_0011_1110_0000);
        }
    }
}