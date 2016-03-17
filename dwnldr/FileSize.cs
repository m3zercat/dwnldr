using System;

namespace dwnldr
{
    public struct FileSize : IEquatable<FileSize>
    {
        public Int64 Size { get; }

        public FileSize(Int64 size)
        {
            this.Size = size;
        }

        public static implicit operator FileSize(Int64 source)
        {
            return new FileSize(source);
        }

        public static implicit operator Int64(FileSize source)
        {
            return source.Size;
        }

        public static implicit operator String(FileSize source)
        {
            return source.ToString(SizeType.KiloBytes);
        }

        public Boolean Equals(FileSize other)
        {
            return this.Size.Equals(other.Size);
        }
        
        public override String ToString()
        {
            return ToString(null);
        }

        public String ToString(SizeType? sizeType)
        {
            Decimal size = Size;
            if (!sizeType.HasValue)
            {
                sizeType = SizeType.KiloBytes;
            }

            Int32 typeOfSize = (Int32)sizeType.Value;

            while (typeOfSize > 0)
            {
                size = size / 1024;
                typeOfSize--;
            }

            return String.Format("{0:0.0000} {1}", size, sizeType.Value.ToString());
        }
    }
}