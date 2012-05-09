namespace System.IO.Compression.Zip
{
    /// <summary>
    /// Compression method enumeration
    /// </summary>
    public enum Compression : ushort
    {
        /// <summary>
        /// Uncompressed storage
        /// </summary> 
        Store = 0, 

        /// <summary>
        /// Deflate compression method
        /// </summary>
        Deflate = 8
    }
}