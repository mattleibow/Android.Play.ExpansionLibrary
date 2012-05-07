namespace System.IO.Compression.Zip
{
    public sealed class Crc32
    {
        /// <summary>
        /// Initializes static members of the <see cref="Crc32"/> class. 
        /// Just invoked once in order to create the CRC32 lookup table.
        /// </summary>
        static Crc32()
        {
            // Generate CRC32 table
            Crc32Table = new uint[256];
            for (uint i = 0; i < Crc32Table.Length; i++)
            {
                var c = i;

                for (int j = 0; j < 8; j++)
                {
                    if ((c & 1) != 0)
                    {
                        c = 3988292384 ^ (c >> 1);
                    }
                    else
                    {
                        c >>= 1;
                    }
                }

                Crc32Table[i] = c;
            }
        }

        /// <summary>
        /// Gets the CRC32 Table.
        /// </summary>
        public static uint[] Crc32Table { get; private set; }

        const uint CrcSeed = 0xFFFFFFFF;

        /// <summary>
        /// Returns the CRC32 data checksum computed so far.
        /// </summary>
        public uint Value { get; set; }

        /// <summary>
        /// Resets the CRC32 data checksum as if no update was ever called.
        /// </summary>
        public void Reset()
        {
            Value = 0;
        }

        /// <summary>
        /// Updates the checksum with the int bval.
        /// </summary>
        /// <param name = "value">
        /// the byte is taken as the lower 8 bits of value
        /// </param>
        public void Update(int value)
        {
            Value ^= CrcSeed;
            Value = Crc32Table[(Value ^ value) & 0xFF] ^ (Value >> 8);
            Value ^= CrcSeed;
        }

        /// <summary>
        /// Adds the byte array to the data checksum.
        /// </summary>
        /// <param name = "buffer">
        /// The buffer which contains the data
        /// </param>
        /// <param name = "offset">
        /// The offset in the buffer where the data starts
        /// </param>
        /// <param name = "count">
        /// The number of data bytes to update the CRC with.
        /// </param>
        public void Update(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("count", "Count cannot be less than zero");
            }

            if (offset < 0 || offset + count > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }

            Value ^= CrcSeed;

            while (--count >= 0)
            {
                Value = Crc32Table[(Value ^ buffer[offset++]) & 0xFF] ^ (Value >> 8);
            }

            Value ^= CrcSeed;
        }
    }
}