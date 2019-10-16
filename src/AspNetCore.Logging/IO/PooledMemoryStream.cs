using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Codestellation.AspNetCore.Logging.IO
{
    public class PooledMemoryStream : Stream
    {
        private readonly ArrayPool<byte> _pool;

        private readonly List<byte[]> _chunks;

        private readonly int _chunkSize;

        private int _position;

        private int _length;

        public PooledMemoryStream(ArrayPool<byte> pool, int chunkSize)
        {
            _pool = pool;
            _chunkSize = chunkSize;
            _chunks = new List<byte[]>();
        }
        
        public override void Flush() { }

        public override Task FlushAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        
        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_length <= _position)
            {
                return 0;
            }

            int remainder = Math.Min(count, _length - _position);
            int totalBytesRead = 0;

            while (remainder > 0)
            {
                int bytesCount = ReadChunk(buffer, offset, remainder);
                totalBytesRead += bytesCount;
                remainder -= bytesCount;
                offset += bytesCount;
                _position += bytesCount;
            }

            return totalBytesRead;
        }

        private int ReadChunk(byte[] buffer, int bufferOffset, int count)
        {
            int chunkIndex = _position / _chunkSize;
            int chunkOffset = _position % _chunkSize;

            byte[] chunk = _chunks[chunkIndex];

            count = Math.Min(count, _length - _position);
            count = Math.Min(count, _chunkSize - chunkOffset);

            Buffer.BlockCopy(chunk, chunkOffset, buffer, bufferOffset, count);
            return count;
        }

        public override int ReadByte()
        {
            if (_length <= _position)
            {
                return -1;
            }

            int chunkIndex = _position / _chunkSize;
            int chunkOffset = _position % _chunkSize;

            byte[] chunk = _chunks[chunkIndex];
            var c = chunk[chunkOffset];

            _position++;
            return c;
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            int bytesCount = Read(buffer, offset, count);
            return Task.FromResult(bytesCount);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                {
                    _position = (int)offset;
                    break;
                }
                case SeekOrigin.Current:
                {
                    _position += (int)offset;
                    break;
                }
                case SeekOrigin.End:
                {
                    _position = _length + (int)offset;
                    break;
                }
            }

            return _position;
        }

        public override void SetLength(long length)
        {
            if (length == _length)
            {
                return;
            }

            if (length < _length)
            {
                int newChunksCount = (int)(length / _chunkSize) + 1;
                int oldChunksCount = _chunks.Count;

                if (oldChunksCount != newChunksCount)
                {
                    for (int i = newChunksCount; i < oldChunksCount; i++)
                    {
                        byte[] chunk = _chunks[i];
                        _pool.Return(chunk);
                    }

                    _chunks.RemoveRange(newChunksCount, oldChunksCount - newChunksCount);
                }
            }
            else
            {
                int lastChunkIndex = (int) (length / _chunkSize);
                EnsureChunk(lastChunkIndex);
            }

            _length = (int)length;

            if (_length < _position)
            {
                _position = _length;
            }
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count == 0)
            {
                return;
            }

            int remainder = count;

            while (remainder > 0)
            {
                int bytesCount = WriteChunk(buffer, offset, remainder);
                offset += bytesCount;
                _position += bytesCount;
                _length = Math.Max(_position, _length);
                remainder -= bytesCount;
            }
        }

        private int WriteChunk(byte[] buffer, int bufferOffset, int count)
        {
            int chunkIndex = _position / _chunkSize;
            int chunkOffset = _position % _chunkSize;

            EnsureChunk(chunkIndex);

            byte[] chunk = _chunks[chunkIndex];

            count = Math.Min(count, _chunkSize - chunkOffset);

            Buffer.BlockCopy(buffer, bufferOffset, chunk, chunkOffset, count);
            return count;
        }

        public override void WriteByte(byte value)
        {
            int chunkIndex = _position / _chunkSize;
            int chunkOffset = _position % _chunkSize;

            EnsureChunk(chunkIndex);

            byte[] chunk = _chunks[chunkIndex];
            chunk[chunkOffset] = value;

            _position++;
            _length = Math.Max(_position, _length);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<int>(cancellationToken);
            }

            Write(buffer, offset, count);
            return Task.CompletedTask;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override long Length => _length;

        public override long Position
        {
            get => _position;
            set => _position = (int)value;
        }

        public int ChunksCount => _chunks.Count;

        public byte[] ToArray()
        {
            if (_length == 0)
            {
                return Array.Empty<byte>();
            }

            var buffer = new byte[_length];
            WriteChunksTo(buffer);
            return buffer;
        }

        public string GetString(Encoding encoding)
        {
            if (_chunks.Count == 0)
            {
                return null;
            }

            if (_chunks.Count == 1)
            {
                return encoding.GetString(_chunks[0], 0, _length);
            }


            byte[] buffer = null;
            try
            {
                buffer = _pool.Rent(_length);
                WriteChunksTo(buffer);
                return encoding.GetString(buffer, 0, _length);
            }
            finally
            {
                if (buffer != null)
                {
                    _pool.Return(buffer);
                }
            }
        }

        private void WriteChunksTo(byte[] buffer)
        {
            int chunksCount = _chunks.Count;
            for (int i = 0; i < chunksCount - 1; i++)
            {
                AppendChunk(buffer, i, _chunkSize);
            }

            int lastChunkBytes = _length - (chunksCount - 1) * _chunkSize;
            AppendChunk(buffer, chunksCount - 1, lastChunkBytes); // last chunk
        }

        private void AppendChunk(byte[] result, int chunkIndex, int bytesCount)
        {
            byte[] chunk = _chunks[chunkIndex];
            int offset = _chunkSize * chunkIndex;
            Buffer.BlockCopy(chunk, 0, result, offset, bytesCount);
        }

        private void EnsureChunk(int chunkIndex)
        {
            int addCount = chunkIndex - _chunks.Count + 1;

            for (int i = 0; i < addCount; i++)
            {
                byte[] chunk = _pool.Rent(_chunkSize);
                Array.Clear(chunk, 0, _chunkSize);
                _chunks.Add(chunk);
            }
        }

        protected override void Dispose(bool disposing)
        {
            foreach (byte[] chunk in _chunks)
            {
                _pool.Return(chunk);
            }

            _chunks.Clear();
        }

        ~PooledMemoryStream()
        {
            Dispose(false);
        }
    }
}