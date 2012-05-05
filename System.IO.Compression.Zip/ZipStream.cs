namespace System.IO.Compression.Zip
{
    public class ZipStream : Stream
    {
        private readonly Stream innerStream;
        private readonly ZipFileEntry zipFileEntry;

        public override void Close()
        {
            base.Close();

            if (this.zipFileEntry.Method == Compression.Deflate)
            {
                this.innerStream.Dispose();
            }
        }

        public ZipStream(Stream innerStream, ZipFileEntry zipFileEntry)
        {
            this.innerStream = innerStream;
            this.zipFileEntry = zipFileEntry;
        }

        public override void Flush()
        {
            throw new InvalidOperationException("You cannot modify this stream.");
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long localOffset = -1;
            switch (origin)
            {
                case SeekOrigin.Begin:
                    localOffset = this.zipFileEntry.FileOffset + offset;
                    break;
                case SeekOrigin.Current:
                    break;
                case SeekOrigin.End:
                    localOffset = this.zipFileEntry.FileOffset + this.zipFileEntry.FileSize + offset;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("origin");
            }

            if (localOffset > this.zipFileEntry.FileSize || localOffset < 0)
            {
                throw new EndOfStreamException();
            }

            return this.innerStream.Seek(localOffset, origin) - this.zipFileEntry.FileOffset;
        }

        public override void SetLength(long value)
        {
            throw new InvalidOperationException("You cannot modify this stream.");
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.innerStream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new InvalidOperationException("You cannot modify this stream.");
        }

        public override bool CanRead
        {
            get { return this.innerStream.CanRead; }
        }

        public override bool CanSeek
        {
            get { return this.innerStream.CanSeek; }
        }

        public override bool CanWrite
        {
            get { return false; }
        }

        public override long Length
        {
            get { return this.zipFileEntry.FileSize; }
        }

        public override long Position
        {
            get
            {
                return this.innerStream.Position - this.zipFileEntry.FileOffset;
            }

            set
            {
                this.innerStream.Position = this.zipFileEntry.FileOffset + value;
            }
        }
    }
}
