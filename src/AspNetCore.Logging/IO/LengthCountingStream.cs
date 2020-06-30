using System;
using System.IO;

namespace Codestellation.AspNetCore.Logging.IO
{
    public class LengthCountingStream : Stream
    {
        private long _position;
        private long _length;

        public override void Flush() {}

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

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

        public override void SetLength(long value) => _length = value;

        public override void Write(byte[] buffer, int offset, int count)
        {
            _position += count;
            _length = Math.Max(_position, _length);
        }

        public override bool CanRead => false;
        public override bool CanSeek => true;
        public override bool CanWrite => true;
        public override long Length => _length;
        public override long Position
        {
            get => _position;
            set => _position = value;
        }
    }
}