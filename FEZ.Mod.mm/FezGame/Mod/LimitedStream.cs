using System;
using System.IO;

namespace FezGame.Mod {
    public class LimitedStream : MemoryStream {
        
        public Stream LimitStream;
        public long LimitOffset;
        public long LimitLength;
        
        protected byte[] cachedBuffer;
        protected long cachedOffset;
        protected long cachedLength;
        protected bool cacheBuffer_ = true;
        public bool CacheBuffer {
            get {
                return cacheBuffer_;
            }
            set {
                if (!value) {
                    cachedBuffer = null;
                }
                cacheBuffer_ = value;
            }
        }
        
        public override bool CanRead {
            get {
                return LimitStream.CanRead;
            }
        }

        public override bool CanSeek {
            get {
                return LimitStream.CanSeek;
            }
        }

        public override bool CanWrite {
            get {
                return LimitStream.CanWrite;
            }
        }

        public override long Length {
            get {
                return LimitLength;
            }
        }

        public override long Position {
            get {
                return LimitStream.Position - LimitOffset;
            }
            set {
                LimitStream.Position = Position + LimitOffset;
            }
        }
        
        public LimitedStream(Stream stream, long offset, long length)
            : base((int) Math.Max(length, int.MaxValue)) {
            LimitStream = stream;
            LimitOffset = offset;
            LimitLength = length;
        }

        public override void Flush() {
            LimitStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count) {
            if (LimitOffset + LimitLength <= Position + count) {
                throw new Exception("out of something");
            }
            return LimitStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin) {
            if (LimitOffset + LimitLength <= Position + offset) {
                throw new Exception("out of something");
            }
            return LimitStream.Seek(offset, origin);
        }

        public override void SetLength(long value) {
            LimitStream.SetLength(LimitOffset + value + LimitLength);
        }

        public override void Write(byte[] buffer, int offset, int count) {
            if (LimitOffset + LimitLength <= Position + count) {
                throw new Exception("out of something");
            }
            LimitStream.Write(buffer, offset, count);
        }
        
        public override byte[] GetBuffer() {
            if (cachedBuffer != null && cachedOffset == LimitOffset && cachedLength == LimitLength) {
                return cachedBuffer;
            }
            
            byte[] buffer = new byte[LimitLength];
            int read;
            int readCompletely = 0;
            while (readCompletely < buffer.Length) {
                read = LimitStream.Read(buffer, readCompletely, buffer.Length - readCompletely);
                readCompletely += read;
            }
            
            if (!cacheBuffer_) {
                return buffer;
            }
            
            cachedOffset = LimitOffset;
            cachedLength = LimitLength;
            return buffer;
        }
    }
}