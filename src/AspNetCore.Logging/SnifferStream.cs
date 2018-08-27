using System;
using System.IO;

namespace Codestellation.AspNetCore.Logging
{
    public class SnifferStream : Stream
    {
        private readonly Stream _master;
        private readonly Stream _sink;

        public SnifferStream(Stream master, Stream sink)
        {
            if (!sink.CanWrite)
            {
                throw new ArgumentException("Sink stream must be writeable", nameof(sink));
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
            set => _master.Position = value;
        }

        public override void Flush()
        {
            _master.Flush();
            _sink.Flush();
        }

        public override long Seek(long offset, SeekOrigin origin) => _master.Seek(offset, origin);

        public override void SetLength(long value) => _master.SetLength(value);

        public override int Read(byte[] buffer, int offset, int count)
        {
            int bytesRead = _master.Read(buffer, offset, count);
            _sink.Write(buffer, offset, bytesRead);
            return bytesRead;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _master.Write(buffer, offset, count);
            _sink.Write(buffer, offset, count);
        }

        public override void Close()
        {
            _master.Close();
            _sink.Close();
            base.Close();
        }
    }
}