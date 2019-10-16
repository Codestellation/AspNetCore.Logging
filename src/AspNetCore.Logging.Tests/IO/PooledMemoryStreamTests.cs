using System;
using System.Buffers;
using System.IO;
using Codestellation.AspNetCore.Logging.IO;
using NUnit.Framework;

namespace Codestellation.AspNetCore.Logging.Tests.IO
{
    [TestFixture]
    public class PooledMemoryStreamTests
    {
        private static readonly Random Rnd = new Random(42);
        private PooledMemoryStream _pooledStream;
        private MemoryStream _memoryStream;

        [SetUp]
        public void Setup()
        {
            _pooledStream = new PooledMemoryStream(ArrayPool<byte>.Shared, 16);
            _memoryStream = new MemoryStream();
        }

        [TearDown]
        public void TearDown()
        {
            _pooledStream.Dispose();
            _memoryStream.Dispose();
        }

        [Test]
        [TestCase(0, 0)]
        [TestCase(0, 3)]
        [TestCase(0, 30)]
        [TestCase(0, 300)]
        [TestCase(5, 3)]
        [TestCase(5, 30)]
        [TestCase(5, 300)]
        public void Simple_Write(int offset, int count) => Write(offset, count);

        [Test]
        public void Simple_WriteByte() => WriteByte();

        [Test]
        [TestCase(0, 0)]
        [TestCase(0, 3)]
        [TestCase(0, 30)]
        [TestCase(0, 300)]
        [TestCase(5, 3)]
        [TestCase(5, 30)]
        [TestCase(5, 300)]
        public void Read_empty(int offset, int count) => Read(offset, count);

        [Test]
        public void ReadByte_empty() => ReadByte();

        [Test]
        [TestCase(5)]
        [TestCase(50)]
        public void SetPosition_for_empty(int position) => SetPosition(position);

        [Test]
        [TestCase(5)]
        [TestCase(70)]
        public void SetPosition_for_not_empty(int position)
        {
            Write(0, 50);
            SetPosition(position);
        }

        [Test]
        [TestCase(5)]
        [TestCase(50)]
        public void SetLength_for_empty(int length) => SetLength(length);

        [Test]
        [TestCase(5)]
        [TestCase(70)]
        public void SetLength_for_not_empty(int length)
        {
            Write(0, 30);
            SetLength(length);
        }

        [Test]
        [TestCase(10, 2, 3)]
        [TestCase(10, 2, 30)]
        [TestCase(100, 2, 3)]
        [TestCase(100, 2, 30)]
        public void SetPosition_and_Read_empty(int position, int offset, int count)
        {
            SetPosition(position);
            Read(offset, count);
        }

        [Test]
        [TestCase(10)]
        [TestCase(100)]
        public void SetPosition_and_ReadByte_empty(int position)
        {
            SetPosition(position);
            ReadByte();
        }

        [Test]
        [TestCase(10, 2, 3)]
        [TestCase(10, 2, 30)]
        [TestCase(100, 2, 3)]
        [TestCase(100, 2, 30)]
        public void SetLength_and_Read_empty(int length, int offset, int count)
        {
            SetLength(length);
            Read(offset, count);
        }

        [Test]
        [TestCase(10)]
        [TestCase(100)]
        public void SetLength_and_ReadByte_empty(int length)
        {
            SetLength(length);
            ReadByte();
        }

        [Test]
        [TestCase(10, 2, 3)]
        [TestCase(10, 2, 30)]
        [TestCase(100, 2, 3)]
        [TestCase(100, 2, 30)]
        public void SetPosition_and_Write(int position, int offset, int count)
        {
            SetPosition(position);
            Write(offset, count);
        }

        [Test]
        [TestCase(10)]
        [TestCase(100)]
        public void SetPosition_and_WriteByte(int position)
        {
            SetPosition(position);
            WriteByte();
        }

        [Test]
        [TestCase(10, 2, 3)]
        [TestCase(10, 2, 30)]
        [TestCase(100, 2, 3)]
        [TestCase(100, 2, 30)]
        public void SetLength_and_Write(int length, int offset, int count)
        {
            SetLength(length);
            Write(offset, count);
        }

        [Test]
        [TestCase(10)]
        [TestCase(100)]
        public void SetLength_and_WriteByte(int length)
        {
            SetLength(length);
            WriteByte();
        }

        [Test]
        [TestCase(0, 0, 5)]
        [TestCase(0, 3, 5)]
        [TestCase(0, 0, 25)]
        [TestCase(0, 3, 25)]
        [TestCase(10, 0, 5)]
        [TestCase(10, 3, 5)]
        [TestCase(10, 0, 25)]
        [TestCase(10, 3, 25)]
        [TestCase(40, 0, 25)]
        [TestCase(40, 3, 25)]
        public void Read_not_empty(int position, int offset, int count)
        {
            Write(0, 50);
            SetPosition(position);
            Read(offset, count);
        }

        [Test]
        [TestCase(0)]
        [TestCase(10)]
        [TestCase(40)]
        public void ReadByte_not_empty(int position)
        {
            Write(0, 50);
            SetPosition(position);
            ReadByte();
        }

        [Test]
        public void Multiple_Write_and_Read()
        {
            Write(0, 10);
            Write(0, 15);
            Write(0, 25);
            SetPosition(0);
            Read(0, 5);
            Read(0, 20);
            Read(0, 10);
        }

        [Test]
        [TestCase(6, 0, 4)]
        [TestCase(6, 0, 40)]
        [TestCase(6, 5, 4)]
        [TestCase(6, 5, 6)]
        [TestCase(20, 10, 8)]
        [TestCase(20, 10, 100)]
        public void Read_trimmed(int oldLength, int newLength, int readCount)
        {
            Write(0, oldLength);
            SetLength(newLength);
            Read(0, readCount);
        }

        [Test]
        [TestCase(6, 0)]
        [TestCase(6, 5)]
        [TestCase(20, 10)]
        public void ReadByte_trimmed(int oldLength, int newLength)
        {
            Write(0, oldLength);
            SetLength(newLength);
            ReadByte();
        }

        [Test]
        [TestCase(6, 0, 3)]
        [TestCase(6, 0, 30)]
        [TestCase(6, 2, 3)]
        [TestCase(6, 2, 20)]
        [TestCase(20, 10, 5)]
        [TestCase(20, 10, 100)]
        public void Write_trimmed(int oldLength, int newLength, int writeCount)
        {
            Write(0, oldLength);
            SetLength(newLength);
            Write(0, writeCount);
        }

        [Test]
        [TestCase(6, 0)]
        [TestCase(6, 2)]
        [TestCase(20, 10)]
        [TestCase(20, 10)]
        public void WriteByte_trimmed(int oldLength, int newLength)
        {
            Write(0, oldLength);
            SetLength(newLength);
            WriteByte();
        }

        [Test]
        public void Multiple_WriteByte_and_ReadByte()
        {
            WriteByte();
            WriteByte();
            WriteByte();
            SetPosition(0);
            ReadByte();
            ReadByte();
            ReadByte();
        }

        [Test]
        [TestCase(10, 8, 5)]
        [TestCase(20, 20, 20)]
        [TestCase(20, 40, 20)]
        [TestCase(50, 20, 10)]
        public void SetLength_SetPosition_Write(int length, int position, int count)
        {
            SetLength(length);
            SetPosition(position);
            Write(0, count);
        }

        [Test]
        [TestCase(10, 8)]
        [TestCase(20, 20)]
        [TestCase(20, 40)]
        [TestCase(50, 20)]
        public void SetLength_SetPosition_WriteByte(int length, int position)
        {
            SetLength(length);
            SetPosition(position);
            WriteByte();
        }

        [Test]
        [TestCase(5, 2)]
        [TestCase(5, 7)]
        [TestCase(25, 15)]
        [TestCase(25, 35)]
        public void SetPosition_SetLength(int position, int length)
        {
            SetPosition(position);
            SetLength(length);
        }

        private void Write(int offset, int count)
        {
            var buffer = new byte[offset + count];
            Rnd.NextBytes(buffer);

            _pooledStream.Write(buffer, offset, count);
            _memoryStream.Write(buffer, offset, count);

            AssertState($"Write {count}");
        }

        private void WriteByte()
        {
            byte c = (byte)Rnd.Next();
            _pooledStream.WriteByte(c);
            _memoryStream.WriteByte(c);

            AssertState("WriteByte");
        }

        private void SetPosition(int position)
        {
            _pooledStream.Position = position;
            _memoryStream.Position = position;

            AssertState($"SetPosition {position}");
        }

        private void SetLength(int length)
        {
            _pooledStream.SetLength(length);
            _memoryStream.SetLength(length);

            AssertState($"SetLength {length}");
        }

        private void Read(int offset, int count)
        {
            var expectedBuffer = new byte[offset + count];
            int expectedCount = _memoryStream.Read(expectedBuffer, offset, count);

            var actualBuffer = new byte[offset + count];
            int actualCount = _pooledStream.Read(actualBuffer, offset, count);

            Assert.That(actualCount, Is.EqualTo(expectedCount));
            Assert.That(actualBuffer, Is.EqualTo(expectedBuffer));

            AssertState($"Read {count}");
        }

        private void ReadByte()
        {
            int expected = _memoryStream.ReadByte();
            int actual = _pooledStream.ReadByte();

            Assert.That(actual, Is.EqualTo(expected));

            AssertState("ReadByte");
        }

        private void AssertState(string operation)
        {
            Console.Write($"[{operation}]: position={_pooledStream.Position}|{_memoryStream.Position}, chunks={_pooledStream.ChunksCount}, length={_pooledStream.Length}|{_memoryStream.Length}");

            Assert.That(_pooledStream.Position, Is.EqualTo(_memoryStream.Position));
            Assert.That(_pooledStream.Length, Is.EqualTo(_memoryStream.Length));

            byte[] actual = _pooledStream.ToArray();
            byte[] expected = _memoryStream.ToArray();
            Assert.That(actual, Is.EqualTo(expected));

            Console.WriteLine(" ...[OK]");
        }
    }
}