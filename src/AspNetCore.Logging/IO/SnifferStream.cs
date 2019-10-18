using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Codestellation.AspNetCore.Logging.IO
{
    public class SnifferStream : Stream
    {
        private readonly Stream _master;
        private readonly Stream _sink;

        public SnifferStream(Stream master, Stream sink)
        {
            if (!sink.CanWrite)
            {
                throw new ArgumentException("Sink stream must be writable", nameof(sink));
            }

            _master = master;
            _sink = sink;
        }

        public override bool CanRead => _master.CanRead;

        public override bool CanSeek => _master.CanSeek;

        public override bool CanWrite => _master.CanWrite;

        public override long Length => _master.Length;

        public override long Position
        {
            get => _master.Position;
            set
            {
                _master.Position = value;
                _sink.Position = value;
            }
        }

        public override void Flush()
        {
            _master.Flush();
            _sink.Flush();
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
            => Task.WhenAll(
                _master.FlushAsync(cancellationToken),
                _sink.FlushAsync(cancellationToken));

        public override long Seek(long offset, SeekOrigin origin)
        {
            long position = _master.Seek(offset, origin);
            _sink.Seek(offset, origin);
            return position;
        }

        public override void SetLength(long value)
        {
            _master.SetLength(value);
            _sink.SetLength(value);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = _master.Read(buffer, offset, count);
            if (bytesRead > 0)
            {
                _sink.Write(buffer, offset, bytesRead);
            }

            return bytesRead;
        }

        public override int ReadByte()
        {
            int value = _master.ReadByte();
            if (value > 0)
            {
                _sink.WriteByte((byte)value);
            }

            return value;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            Task<int> readAsync = _master.ReadAsync(buffer, offset, count, cancellationToken);
            int bytesRead = await readAsync;
            await _sink.WriteAsync(buffer, offset, bytesRead, cancellationToken);
            return bytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _master.Write(buffer, offset, count);
            _sink.Write(buffer, offset, count);
        }

        public override void WriteByte(byte value)
        {
            _master.WriteByte(value);
            _sink.WriteByte(value);
        }

        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => Task.WhenAll(
                _master.WriteAsync(buffer, offset, count, cancellationToken),
                _sink.WriteAsync(buffer, offset, count, cancellationToken));
    }
}