using System;
using System.IO;
using Asv.IO;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit;

namespace Asv.IO.Test.Serializable.BitBased.Reader;

[TestSubject(typeof(StreamBitReader))]
public class StreamBitReaderTest
{
    [Fact]
    public void ReadBit_ShouldReadSingleBit_FromFirstByte()
    {
        // Arrange: 0b10101010 = 0xAA
        var buffer = new byte[] { 0xAA };
        using var stream = new MemoryStream(buffer);
        using var reader = new StreamBitReader(stream);

        // Act & Assert: Read bits from MSB to LSB
        reader.ReadBit().Should().Be(1); // bit 7
        reader.ReadBit().Should().Be(0); // bit 6
        reader.ReadBit().Should().Be(1); // bit 5
        reader.ReadBit().Should().Be(0); // bit 4
        reader.ReadBit().Should().Be(1); // bit 3
        reader.ReadBit().Should().Be(0); // bit 2
        reader.ReadBit().Should().Be(1); // bit 1
        reader.ReadBit().Should().Be(0); // bit 0

        reader.TotalBitsRead.Should().Be(8);
    }

    [Fact]
    public void ReadBit_ShouldReadAcrossMultipleBytes()
    {
        // Arrange: 0xFF, 0x00
        var buffer = new byte[] { 0xFF, 0x00 };
        using var stream = new MemoryStream(buffer);
        using var reader = new StreamBitReader(stream);

        // Act: Read all bits from first byte
        for (int i = 0; i < 8; i++)
        {
            reader.ReadBit().Should().Be(1);
        }

        // Read all bits from second byte
        for (int i = 0; i < 8; i++)
        {
            reader.ReadBit().Should().Be(0);
        }

        // Assert
        reader.TotalBitsRead.Should().Be(16);
    }

    [Fact]
    public void ReadBit_ShouldThrowEndOfStreamException_WhenStreamExhausted()
    {
        // Arrange
        var buffer = new byte[] { 0xFF };
        using var stream = new MemoryStream(buffer);
        using var reader = new StreamBitReader(stream);

        // Act: Read all 8 bits
        for (int i = 0; i < 8; i++)
        {
            reader.ReadBit();
        }

        // Assert: 9th bit should throw
        var action = () => reader.ReadBit();
        action.Should().Throw<EndOfStreamException>();
    }

    [Fact]
    public void ReadBits_ShouldReadSpecifiedNumberOfBits()
    {
        // Arrange: 0b11110000 = 0xF0
        var buffer = new byte[] { 0xF0 };
        using var stream = new MemoryStream(buffer);
        using var reader = new StreamBitReader(stream);

        // Act & Assert
        var result = reader.ReadBits(4);
        result.Should().Be(0b1111ul); // First 4 bits
        reader.TotalBitsRead.Should().Be(4);

        result = reader.ReadBits(4);
        result.Should().Be(0b0000ul); // Last 4 bits
        reader.TotalBitsRead.Should().Be(8);
    }

    [Fact]
    public void ReadBits_ShouldReadAcrossBytes()
    {
        // Arrange: 0xAA, 0x55 = 0b10101010, 0b01010101
        var buffer = new byte[] { 0xAA, 0x55 };
        using var stream = new MemoryStream(buffer);
        using var reader = new StreamBitReader(stream);

        // Act: Read 12 bits (4 from first byte + 8 from second byte)
        var result = reader.ReadBits(12);

        // Assert: 0b101010100101 = 0xAA5
        result.Should().Be(0xAA5ul);
        reader.TotalBitsRead.Should().Be(12);
    }

    [Fact]
    public void ReadBits_ShouldHandleFullBytes_OptimizedPath()
    {
        // Arrange: Test the optimized path for byte-aligned reads
        var buffer = new byte[] { 0x12, 0x34, 0x56, 0x78 };
        using var stream = new MemoryStream(buffer);
        using var reader = new StreamBitReader(stream);

        // Act: Read 32 bits (4 bytes) when byte-aligned
        var result = reader.ReadBits(32);

        // Assert
        result.Should().Be(0x12345678ul);
        reader.TotalBitsRead.Should().Be(32);
    }

    [Fact]
    public void ReadBits_ShouldThrowArgumentOutOfRangeException_WhenCountExceeds64()
    {
        // Arrange
        var buffer = new byte[] { 0xFF };
        using var stream = new MemoryStream(buffer);
        using var reader = new StreamBitReader(stream);

        // Act & Assert
        var action = () => reader.ReadBits(65);
        action.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("count");
    }

    [Fact]
    public void ReadBits_ShouldHandleZeroBits()
    {
        // Arrange
        var buffer = new byte[] { 0xFF };
        using var stream = new MemoryStream(buffer);
        using var reader = new StreamBitReader(stream);

        // Act
        var result = reader.ReadBits(0);

        // Assert
        result.Should().Be(0ul);
        reader.TotalBitsRead.Should().Be(0);
    }

    [Fact]
    public void ReadBits_ShouldThrowEndOfStreamException_WhenNotEnoughData()
    {
        // Arrange
        var buffer = new byte[] { 0xFF }; // Only 8 bits available
        using var stream = new MemoryStream(buffer);
        using var reader = new StreamBitReader(stream);

        // Act & Assert
        var action = () => reader.ReadBits(16); // Try to read 16 bits
        action.Should().Throw<EndOfStreamException>();
    }

    [Fact]
    public void AlignToByte_ShouldAlignToNextByteBoundary()
    {
        // Arrange
        var buffer = new byte[] { 0xFF, 0xAA };
        using var stream = new MemoryStream(buffer);
        using var reader = new StreamBitReader(stream);

        // Act: Read 3 bits, then align
        reader.ReadBits(3);
        reader.TotalBitsRead.Should().Be(3);

        reader.AlignToByte();

        // Assert: Should be aligned to byte 2 (skipping 5 remaining bits from byte 1)
        reader.TotalBitsRead.Should().Be(8);

        // Next read should start from second byte
        var result = reader.ReadBits(8);
        result.Should().Be(0xAAul);
    }

    [Fact]
    public void AlignToByte_ShouldDoNothing_WhenAlreadyAligned()
    {
        // Arrange
        var buffer = new byte[] { 0xFF, 0xAA };
        using var stream = new MemoryStream(buffer);
        using var reader = new StreamBitReader(stream);

        // Act: Read full byte, then align
        reader.ReadBits(8);
        var bitsBeforeAlign = reader.TotalBitsRead;

        reader.AlignToByte();

        // Assert: Should not change position when already aligned
        reader.TotalBitsRead.Should().Be(bitsBeforeAlign);
    }

    [Fact]
    public void TotalBitsRead_ShouldTrackBitsCorrectly()
    {
        // Arrange
        var buffer = new byte[] { 0xFF, 0xAA, 0x55 };
        using var stream = new MemoryStream(buffer);
        using var reader = new StreamBitReader(stream);

        // Act & Assert: Track bits through various operations
        reader.TotalBitsRead.Should().Be(0);

        reader.ReadBit();
        reader.TotalBitsRead.Should().Be(1);

        reader.ReadBits(7);
        reader.TotalBitsRead.Should().Be(8);

        reader.ReadBits(4);
        reader.TotalBitsRead.Should().Be(12);

        reader.AlignToByte();
        reader.TotalBitsRead.Should().Be(16);

        reader.ReadBits(8);
        reader.TotalBitsRead.Should().Be(24);
    }

    [Fact]
    public void Constructor_ShouldAcceptEmptyStream()
    {
        // Arrange & Act
        using var stream = new MemoryStream(Array.Empty<byte>());
        using var reader = new StreamBitReader(stream);

        // Assert
        reader.TotalBitsRead.Should().Be(0);

        // Should throw immediately when trying to read
        var action = () => reader.ReadBit();
        action.Should().Throw<EndOfStreamException>();
    }

    [Fact]
    public void ReadBits_ShouldHandleMaxValue64Bits()
    {
        // Arrange: 8 bytes of known pattern
        var buffer = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 };
        using var stream = new MemoryStream(buffer);
        using var reader = new StreamBitReader(stream);

        // Act: Read all 64 bits
        var result = reader.ReadBits(64);

        // Assert
        result.Should().Be(0x123456789ABCDEF0ul);
        reader.TotalBitsRead.Should().Be(64);
    }

    [Fact]
    public void ReadBits_ShouldHandleMixedOperations()
    {
        // Arrange: Complex scenario with mixed bit/byte operations
        var buffer = new byte[] { 0xAB, 0xCD, 0xEF };
        using var stream = new MemoryStream(buffer);
        using var reader = new StreamBitReader(stream);

        // Act & Assert: Read 1 bit, then 7 bits, then 8 bits, then 8 bits
        reader.ReadBit().Should().Be(1); // First bit of 0xAB (1010 1011)
        reader.ReadBits(7).Should().Be(0b0101011ul); // Remaining 7 bits of 0xAB
        reader.ReadBits(8).Should().Be(0xCDul); // Full second byte
        reader.ReadBits(8).Should().Be(0xEFul); // Full third byte

        reader.TotalBitsRead.Should().Be(24);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(24)]
    [InlineData(32)]
    [InlineData(40)]
    [InlineData(48)]
    [InlineData(56)]
    [InlineData(64)]
    public void ReadBits_ShouldHandleVariousBitCounts(int bitCount)
    {
        // Arrange: Enough bytes for any test case
        var buffer = new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
        using var stream = new MemoryStream(buffer);
        using var reader = new StreamBitReader(stream);

        // Act
        var result = reader.ReadBits(bitCount);

        // Assert: All bits should be 1 for 0xFF pattern
        var expected = bitCount == 64 ? ulong.MaxValue : (1ul << bitCount) - 1;
        result.Should().Be(expected);
        reader.TotalBitsRead.Should().Be(bitCount);
    }

    [Fact]
    public void Constructor_ShouldAcceptLeaveOpenParameter()
    {
        // Arrange
        var buffer = new byte[] { 0xFF };
        var stream = new MemoryStream(buffer);

        // Act: Create reader with leaveOpen = true
        using (var reader = new StreamBitReader(stream, leaveOpen: true))
        {
            reader.ReadBit();
        }

        // Assert: Stream should still be open
        stream.CanRead.Should().BeTrue();

        // Cleanup
        stream.Dispose();
    }

    [Fact]
    public void Constructor_ShouldDisposeStreamByDefault()
    {
        // Arrange
        var buffer = new byte[] { 0xFF };
        var stream = new MemoryStream(buffer);

        // Act: Create reader with default leaveOpen = false
        using (var reader = new StreamBitReader(stream))
        {
            reader.ReadBit();
        }

        // Assert: Stream should be closed/disposed
        stream.CanRead.Should().BeFalse();
    }

    [Fact]
    public void Dispose_ShouldBeIdempotent()
    {
        // Arrange
        var buffer = new byte[] { 0xFF };
        using var stream = new MemoryStream(buffer);
        var reader = new StreamBitReader(stream, leaveOpen: true);

        // Act: Dispose multiple times
        reader.Dispose();
        reader.Dispose();
        reader.Dispose();

        // Assert: Should not throw
        // This test passes if no exception is thrown
    }

    [Fact]
    public void ReadOperations_ShouldThrowObjectDisposedException_AfterDispose()
    {
        // Arrange
        var buffer = new byte[] { 0xFF, 0xAA };
        using var stream = new MemoryStream(buffer);
        var reader = new StreamBitReader(stream, leaveOpen: true);

        // Act: Dispose reader
        reader.Dispose();

        // Assert: Operations should throw ObjectDisposedException
        var readBitAction = () => reader.ReadBit();
        readBitAction.Should().Throw<ObjectDisposedException>();

        var readBitsAction = () => reader.ReadBits(8);
        readBitsAction.Should().Throw<ObjectDisposedException>();

        var alignAction = () => reader.AlignToByte();
        alignAction.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void TotalBitsRead_ShouldNotThrow_AfterDispose()
    {
        // Arrange
        var buffer = new byte[] { 0xFF };
        using var stream = new MemoryStream(buffer);
        var reader = new StreamBitReader(stream, leaveOpen: true);

        reader.ReadBit();
        var expectedBits = reader.TotalBitsRead;

        // Act: Dispose reader
        reader.Dispose();

        // Assert: TotalBitsRead should still work and return last known value
        reader.TotalBitsRead.Should().Be(expectedBits);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_ForNullStream()
    {
        // Act & Assert
        var action = () => new StreamBitReader(null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("s");
    }

    [Fact]
    public void ReadOperations_ShouldWorkWithNonSeekableStream()
    {
        // Arrange: Create a non-seekable stream wrapper
        var buffer = new byte[] { 0xAA, 0x55 };
        var baseStream = new MemoryStream(buffer);
        var nonSeekableStream = new NonSeekableStreamWrapper(baseStream);

        using var reader = new StreamBitReader(nonSeekableStream, leaveOpen: true);

        // Act & Assert: Should work with non-seekable streams
        reader.ReadBit().Should().Be(1);
        reader.ReadBits(7).Should().Be(0b0101010ul);
        reader.ReadBits(8).Should().Be(0x55ul);

        reader.TotalBitsRead.Should().Be(16);

        // Cleanup
        baseStream.Dispose();
    }

    // Helper class for testing non-seekable streams
    private class NonSeekableStreamWrapper : Stream
    {
        private readonly Stream _baseStream;

        public NonSeekableStreamWrapper(Stream baseStream)
        {
            _baseStream = baseStream;
        }

        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => false; // Make it non-seekable
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => _baseStream.Flush();

        public override int Read(byte[] buffer, int offset, int count) =>
            _baseStream.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _baseStream?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
