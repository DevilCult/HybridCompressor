using System;

namespace ConsoleApp1.Compression
{
    /// <summary>
    /// Represents a chunk of data with metadata
    /// </summary>
    public class DataBlock
    {
        public int BlockId { get; set; }
        public byte[] Data { get; set; } = Array.Empty<byte>();
        public int OriginalSize { get; set; }
        public int CompressedSize { get; set; }
        public CompressionMethod Method { get; set; }
        public DataType DetectedType { get; set; }
        public uint Checksum { get; set; }
        public long FileOffset { get; set; }
        public byte[] CompressedData { get; set; } = Array.Empty<byte>();
        public string Hash { get; set; } = string.Empty; // For deduplication

        public DataBlock(int blockId, byte[] data, long fileOffset)
        {
            BlockId = blockId;
            Data = data;
            OriginalSize = data.Length;
            FileOffset = fileOffset;
            Checksum = CalculateChecksum(data);
            Hash = CalculateHash(data);
        }

        private uint CalculateChecksum(byte[] data)
        {
            uint checksum = 0;
            foreach (byte b in data)
            {
                checksum = (checksum << 1) | (checksum >> 31);
                checksum ^= b;
            }
            return checksum;
        }

        private string CalculateHash(byte[] data)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(data);
                return Convert.ToBase64String(hash);
            }
        }
    }

    public enum CompressionMethod
    {
        None = 0,
        Text = 1,
        LZ = 2,
        Dictionary = 3,
        Delta = 4,
        RLE = 5,
        Hybrid = 6
    }

    public enum DataType
    {
        Unknown = 0,
        Text = 1,
        Binary = 2,
        Numeric = 3,
        Repetitive = 4,
        Random = 5
    }
}
