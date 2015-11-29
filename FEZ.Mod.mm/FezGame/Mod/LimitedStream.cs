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
            : base(0) {
            LimitStream = stream;
            LimitOffset = offset;
            LimitLength = length;
            LimitStream.Seek(offset, SeekOrigin.Begin);
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
            switch (origin) {
                case SeekOrigin.Begin:
                    if (LimitOffset + LimitLength <= offset) {
                        throw new Exception("out of something");
                    }
                    return LimitStream.Seek(LimitOffset + offset, SeekOrigin.Begin);
                case SeekOrigin.Current:
                    if (LimitOffset + LimitLength <= Position + offset) {
                        throw new Exception("out of something");
                    }
                    return LimitStream.Seek(offset, SeekOrigin.Current);
                case SeekOrigin.End:
                    if (LimitLength - offset < 0) {
                        throw new Exception("out of something");
                    }
                    return LimitStream.Seek(LimitOffset + LimitLength - offset, SeekOrigin.Begin);
                default:
                    return 0;
            }
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
            long origPosition = LimitStream.Position;
            while (readCompletely < buffer.Length) {
                read = LimitStream.Read(buffer, readCompletely, buffer.Length - readCompletely);
                readCompletely += read;
            }
            LimitStream.Seek(origPosition, SeekOrigin.Begin);
            
            if (!cacheBuffer_) {
                return buffer;
            }
            
            cachedBuffer = buffer;
            cachedOffset = LimitOffset;
            cachedLength = LimitLength;
            return buffer;
        }
    }
}