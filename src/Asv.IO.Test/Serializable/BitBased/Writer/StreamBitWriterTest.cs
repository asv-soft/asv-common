using System;
using System.IO;
using Asv.IO;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit;

namespace Asv.IO.Test.Serializable.BitBased.Writer;

[TestSubject(typeof(StreamBitWriter))]
public class StreamBitWriterTest
{
    [Fact]
    public void WriteBit_ShouldWriteSingleBit_ToFirstByte()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new StreamBitWriter(stream, leaveOpen: true);

        // Act: Write bits to form 0b10101010 = 0xAA
        writer.WriteBit(1); // bit 7 (MSB)
        writer.WriteBit(0); // bit 6
        writer.WriteBit(1); // bit 5
        writer.WriteBit(0); // bit 4
        writer.WriteBit(1); // bit 3
        writer.WriteBit(0); // bit 2
        writer.WriteBit(1); // bit 1
        writer.WriteBit(0); // bit 0 (LSB)
        writer.Flush();

        // Assert
        writer.TotalBitsWritten.Should().Be(8);
        stream.ToArray().Should().Equal(new byte[] { 0xAA });
    }

    [Fact]
    public void WriteBit_ShouldWriteAcrossMultipleBytes()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new StreamBitWriter(stream, leaveOpen: true);

        // Act: Write 16 bits (0xFF, 0x00)
        for (int i = 0; i < 8; i++)
        {
            writer.WriteBit(1); // First byte: 0xFF
        }
        for (int i = 0; i < 8; i++)
        {
            writer.WriteBit(0); // Second byte: 0x00
        }
        writer.Flush();

        // Assert
        writer.TotalBitsWritten.Should().Be(16);
        stream.ToArray().Should().Equal(new byte[] { 0xFF, 0x00 });
    }

    [Fact]
    public void WriteBits_ShouldWriteSpecifiedNumberOfBits()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new StreamBitWriter(stream, leaveOpen: true);

        // Act: Write 4 bits with value 0b1111, then 4 bits with value 0b0000
        writer.WriteBits(0b1111ul, 4); // First 4 bits: 1111
        writer.WriteBits(0b0000ul, 4); // Last 4 bits: 0000
        writer.Flush();

        // Assert: Should form 0b11110000 = 0xF0
        writer.TotalBitsWritten.Should().Be(8);
        stream.ToArray().Should().Equal(new byte[] { 0xF0 });
    }

    [Fact]
    public void WriteBits_ShouldWriteAcrossBytes()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new StreamBitWriter(stream, leaveOpen: true);

        // Act: Write 12 bits with value 0xAA5 = 0b101010100101
        writer.WriteBits(0xAA5ul, 12);
        writer.Flush();

        // Assert: Should create 0xAA, 0x50 (the 12 bits split across 2 bytes)
        writer.TotalBitsWritten.Should().Be(12);
        stream.ToArray().Should().Equal(new byte[] { 0xAA, 0x50 });
    }

    [Fact]
    public void WriteBits_ShouldHandleFullBytes_OptimizedPath()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new StreamBitWriter(stream, leaveOpen: true);

        // Act: Write 32 bits (4 bytes) when byte-aligned
        writer.WriteBits(0x12345678ul, 32);
        writer.Flush();

        // Assert
        writer.TotalBitsWritten.Should().Be(32);
        stream.ToArray().Should().Equal(new byte[] { 0x12, 0x34, 0x56, 0x78 });
    }

    [Fact]
    public void WriteBits_ShouldThrowArgumentOutOfRangeException_WhenCountExceeds64()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new StreamBitWriter(stream, leaveOpen: true);

        // Act & Assert
        var action = () => writer.WriteBits(0ul, 65);
        action.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("count");
    }

    [Fact]
    public void WriteBits_ShouldHandleZeroBits()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new StreamBitWriter(stream, leaveOpen: true);

        // Act
        writer.WriteBits(0xFFul, 0);
        writer.Flush();

        // Assert
        writer.TotalBitsWritten.Should().Be(0);
        stream.ToArray().Should().BeEmpty();
    }

    [Fact]
    public void WriteBit_ShouldThrowArgumentOutOfRangeException_ForInvalidBitValue()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new StreamBitWriter(stream, leaveOpen: true);

        // Act & Assert: Test invalid bit values
        var action1 = () => writer.WriteBit(-1);
        action1.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("bit");

        var action2 = () => writer.WriteBit(2);
        action2.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("bit");
    }

    [Fact]
    public void Flush_ShouldFlushPartialByte_WithPadding()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new StreamBitWriter(stream, leaveOpen: true);

        // Act: Write 3 bits, then flush
        writer.WriteBit(1);
        writer.WriteBit(0);
        writer.WriteBit(1);
        writer.Flush();

        // Assert: Should pad with zeros to complete the byte
        writer.TotalBitsWritten.Should().Be(3);

        // 101xxxxx -> 10100000 = 0xA0 (assuming padding with zeros)
        stream.Length.Should().Be(1);
        stream.ToArray()[0].Should().Be(0xA0);
    }

    [Fact]
    public void Flush_ShouldDoNothing_WhenByteAligned()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new StreamBitWriter(stream, leaveOpen: true);

        // Act: Write full byte, then flush
        writer.WriteBits(0xFFul, 8);
        var lengthBeforeFlush = stream.Length;
        writer.Flush();

        // Assert: Should not change anything when already byte-aligned
        writer.TotalBitsWritten.Should().Be(8);
        stream.Length.Should().Be(lengthBeforeFlush);
        stream.ToArray().Should().Equal(new byte[] { 0xFF });
    }

    [Fact]
    public void TotalBitsWritten_ShouldTrackBitsCorrectly()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new StreamBitWriter(stream, leaveOpen: true);

        // Act & Assert: Track bits through various operations
        writer.TotalBitsWritten.Should().Be(0);

        writer.WriteBit(1);
        writer.TotalBitsWritten.Should().Be(1);

        writer.WriteBits(0x7Ful, 7);
        writer.TotalBitsWritten.Should().Be(8);

        writer.WriteBits(0xFul, 4);
        writer.TotalBitsWritten.Should().Be(12);

        writer.Flush();
        writer.TotalBitsWritten.Should().Be(12);

        writer.WriteBits(0xAAul, 8);
        writer.TotalBitsWritten.Should().Be(20);
    }

    [Fact]
    public void WriteBits_ShouldHandleMaxValue64Bits()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new StreamBitWriter(stream, leaveOpen: true);

        // Act: Write 64 bits with known pattern
        writer.WriteBits(0x123456789ABCDEF0ul, 64);
        writer.Flush();

        // Assert
        writer.TotalBitsWritten.Should().Be(64);
        stream
            .ToArray()
            .Should()
            .Equal(new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 });
    }

    [Fact]
    public void WriteBits_ShouldHandleMixedOperations()
    {
        // Arrange: Complex scenario with mixed bit/byte operations
        using var stream = new MemoryStream();
        using var writer = new StreamBitWriter(stream, leaveOpen: true);

        // Act: Write 1 bit, then 7 bits, then 8 bits, then 8 bits
        writer.WriteBit(1); // First bit of pattern
        writer.WriteBits(0b0101011ul, 7); // Remaining 7 bits to complete 0xAB
        writer.WriteBits(0xCDul, 8); // Full second byte
        writer.WriteBits(0xEFul, 8); // Full third byte
        writer.Flush();

        // Assert
        writer.TotalBitsWritten.Should().Be(24);
        stream.ToArray().Should().Equal(new byte[] { 0xAB, 0xCD, 0xEF });
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
    public void WriteBits_ShouldHandleVariousBitCounts(int bitCount)
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new StreamBitWriter(stream, leaveOpen: true);

        // Act: Write all 1s for the specified bit count
        var value = bitCount == 64 ? ulong.MaxValue : (1ul << bitCount) - 1;
        writer.WriteBits(value, bitCount);
        writer.Flush();

        // Assert
        writer.TotalBitsWritten.Should().Be(bitCount);

        // Verify the pattern matches expected all-1s for the bit count
        var bytes = stream.ToArray();
        var fullBytes = bitCount / 8;
        var remainingBits = bitCount % 8;

        // Check full bytes (should all be 0xFF)
        for (int i = 0; i < fullBytes; i++)
        {
            bytes[i].Should().Be(0xFF, $"byte {i} should be 0xFF");
        }

        // Check partial byte if any
        if (remainingBits > 0)
        {
            var expectedPartialByte = (byte)(0xFF << (8 - remainingBits));
            bytes[fullBytes].Should().Be(expectedPartialByte, $"partial byte should match pattern");
        }
    }

    [Fact]
    public void Constructor_ShouldAcceptLeaveOpenParameter()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act: Create writer with leaveOpen = true
        using (var writer = new StreamBitWriter(stream, leaveOpen: true))
        {
            writer.WriteBit(1);
            writer.Flush();
        }

        // Assert: Stream should still be open
        stream.CanWrite.Should().BeTrue();
        stream.ToArray().Should().Equal(new byte[] { 0x80 }); // 1 bit in MSB position

        // Cleanup
        stream.Dispose();
    }

    [Fact]
    public void Constructor_ShouldDisposeStreamByDefault()
    {
        // Arrange
        var stream = new MemoryStream();

        // Act: Create writer with default leaveOpen = false
        using (var writer = new StreamBitWriter(stream))
        {
            writer.WriteBit(1);
            writer.Flush();
        }

        // Assert: Stream should be closed/disposed
        stream.CanWrite.Should().BeFalse();
    }

    [Fact]
    public void Dispose_ShouldFlushPendingBits()
    {
        // Arrange
        using var stream = new MemoryStream();
        var writer = new StreamBitWriter(stream, leaveOpen: true);

        // Act: Write some bits but don't explicitly flush
        writer.WriteBit(1);
        writer.WriteBit(0);
        writer.WriteBit(1);
        writer.Dispose();

        // Assert: Dispose should have flushed the pending bits
        stream.ToArray().Should().NotBeEmpty();
        stream.ToArray()[0].Should().Be(0xA0); // 101xxxxx -> 10100000
    }

    [Fact]
    public void Dispose_ShouldBeIdempotent()
    {
        // Arrange
        using var stream = new MemoryStream();
        var writer = new StreamBitWriter(stream, leaveOpen: true);

        // Act: Dispose multiple times
        writer.Dispose();
        writer.Dispose();
        writer.Dispose();

        // Assert: Should not throw
        // This test passes if no exception is thrown
    }

    [Fact]
    public void WriteOperations_ShouldThrowObjectDisposedException_AfterDispose()
    {
        // Arrange
        using var stream = new MemoryStream();
        var writer = new StreamBitWriter(stream, leaveOpen: true);

        // Act: Dispose writer
        writer.Dispose();

        // Assert: Operations should throw ObjectDisposedException
        var writeBitAction = () => writer.WriteBit(1);
        writeBitAction.Should().Throw<ObjectDisposedException>();

        var writeBitsAction = () => writer.WriteBits(0xFFul, 8);
        writeBitsAction.Should().Throw<ObjectDisposedException>();

        var flushAction = () => writer.Flush();
        flushAction.Should().Throw<ObjectDisposedException>();
    }

    [Fact]
    public void TotalBitsWritten_ShouldNotThrow_AfterDispose()
    {
        // Arrange
        using var stream = new MemoryStream();
        var writer = new StreamBitWriter(stream, leaveOpen: true);

        writer.WriteBit(1);
        var expectedBits = writer.TotalBitsWritten;

        // Act: Dispose writer
        writer.Dispose();

        // Assert: TotalBitsWritten should still work and return last known value
        writer.TotalBitsWritten.Should().Be(expectedBits);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_ForNullStream()
    {
        // Act & Assert
        var action = () => new StreamBitWriter(null!);
        action.Should().Throw<ArgumentNullException>().WithParameterName("s");
    }

    [Fact]
    public void WriteOperations_ShouldWorkWithNonSeekableStream()
    {
        // Arrange: Create a non-seekable stream wrapper
        var baseStream = new MemoryStream();
        var nonSeekableStream = new NonSeekableStreamWrapper(baseStream);

        using var writer = new StreamBitWriter(nonSeekableStream, leaveOpen: true);

        // Act: Should work with non-seekable streams
        writer.WriteBit(1);
        writer.WriteBits(0b0101010ul, 7);
        writer.WriteBits(0x55ul, 8);
        writer.Flush();

        // Assert
        writer.TotalBitsWritten.Should().Be(16);
        baseStream.ToArray().Should().Equal(new byte[] { 0xAA, 0x55 });

        // Cleanup
        baseStream.Dispose();
    }

    [Fact]
    public void WriteBits_ShouldIgnoreHigherBits_WhenValueExceedsBitCount()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new StreamBitWriter(stream, leaveOpen: true);

        // Act: Write only 4 bits from a larger value (0xFF -> only lower 4 bits should be used)
        writer.WriteBits(0xFFul, 4); // Should only write 0xF (lower 4 bits)
        writer.WriteBits(0x00ul, 4); // Pad with zeros
        writer.Flush();

        // Assert: Should create 0xF0 (1111 0000)
        writer.TotalBitsWritten.Should().Be(8);
        stream.ToArray().Should().Equal(new byte[] { 0xF0 });
    }

    [Fact]
    public void WriteOperations_ShouldHandleLargeSequences()
    {
        // Arrange
        using var stream = new MemoryStream();
        using var writer = new StreamBitWriter(stream, leaveOpen: true);

        // Act: Write a large sequence of alternating patterns
        for (int i = 0; i < 100; i++)
        {
            writer.WriteBits(0xAAul, 8); // 10101010
            writer.WriteBits(0x55ul, 8); // 01010101
        }
        writer.Flush();

        // Assert
        writer.TotalBitsWritten.Should().Be(1600); // 100 * 16 bits
        stream.Length.Should().Be(200); // 200 bytes

        var bytes = stream.ToArray();
        for (int i = 0; i < 200; i += 2)
        {
            bytes[i].Should().Be(0xAA);
            bytes[i + 1].Should().Be(0x55);
        }
    }

    [Fact]
    public void RoundTrip_ShouldPreserveData_WithStreamBitReader()
    {
        // Arrange: Test round-trip compatibility with StreamBitReader
        var originalData = new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A };

        // Write data
        using var writeStream = new MemoryStream();
        using (var writer = new StreamBitWriter(writeStream, leaveOpen: true))
        {
            foreach (var b in originalData)
            {
                writer.WriteBits(b, 8);
            }
            writer.Flush();
        }

        // Read data back
        writeStream.Position = 0;
        using var reader = new StreamBitReader(writeStream, leaveOpen: true);
        var readData = new byte[originalData.Length];
        for (int i = 0; i < originalData.Length; i++)
        {
            readData[i] = (byte)reader.ReadBits(8);
        }

        // Assert: Data should be preserved
        readData.Should().Equal(originalData);
    }

    // Helper class for testing non-seekable streams
    private class NonSeekableStreamWrapper : Stream
    {
        private readonly Stream _baseStream;

        public NonSeekableStreamWrapper(Stream baseStream)
        {
            _baseStream = baseStream;
        }

        public override bool CanRead => false;
        public override bool CanSeek => false; // Make it non-seekable
        public override bool CanWrite => _baseStream.CanWrite;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => _baseStream.Flush();

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) =>
            throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            _baseStream.Write(buffer, offset, count);

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
