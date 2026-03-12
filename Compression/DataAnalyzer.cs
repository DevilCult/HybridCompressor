using System;
using System.Linq;

namespace ConsoleApp1.Compression
{
    /// <summary>
    /// Analyzes data blocks to determine optimal compression strategy
    /// Optimized with fast entropy checking and compressed data detection
    /// </summary>
    public static class DataAnalyzer
    {
        public static DataType DetectDataType(byte[] block)
        {
            if (block == null || block.Length == 0)
                return DataType.Unknown;

            // Check for already compressed data FIRST (magic numbers) - avoid recompression
            if (IsAlreadyCompressed(block))
                return DataType.Random;  // Store without recompression

            // Quick entropy check (cheapest test) - avoid wasting time on incompressible data
            double entropy = CalculateEntropy(block);
            if (entropy > 7.5)
                return DataType.Random;  // Don't waste time on incompressible data

            // Check for text (fast) - text compresses VERY well, prioritize detection
            if (IsText(block))
                return DataType.Text;

            // Check for repetitive patterns (medium cost)
            if (IsRepetitive(block))
                return DataType.Repetitive;

            // Check for numeric data (expensive)
            if (IsNumeric(block))
                return DataType.Numeric;

            return DataType.Binary;
        }

        /// <summary>
        /// Select the best compression algorithm based on data type for optimal ratio/speed
        /// </summary>
        public static LZCompressor.CompressionAlgorithm SelectBestAlgorithm(DataType dataType, int dataSize)
        {
            return dataType switch
            {
                // Text compresses extremely well - use maximum compression
                DataType.Text => LZCompressor.CompressionAlgorithm.LZMA2,
                
                // Repetitive data benefits from strong compression
                DataType.Repetitive => LZCompressor.CompressionAlgorithm.Zstd,
                
                // Numeric data with delta encoding - Zstd is fast and effective
                DataType.Numeric => LZCompressor.CompressionAlgorithm.Zstd,
                
                // Binary data - balance speed and compression
                DataType.Binary => dataSize > 1024 * 1024
                    ? LZCompressor.CompressionAlgorithm.Zstd  // Large files: fast
                    : LZCompressor.CompressionAlgorithm.LZMA2, // Small files: better ratio
                
                // Already compressed or random - use fastest (will likely be stored uncompressed)
                DataType.Random => LZCompressor.CompressionAlgorithm.Deflate,
                
                _ => LZCompressor.CompressionAlgorithm.Zstd
            };
        }

        /// <summary>
        /// Check if data is already compressed (avoid recompression overhead)
        /// </summary>
        private static bool IsAlreadyCompressed(byte[] block)
        {
            if (block.Length < 4) return false;

            // Check for common compressed file signatures
            return (block[0] == 0x1F && block[1] == 0x8B) ||  // GZIP
                   (block[0] == 0x50 && block[1] == 0x4B) ||  // ZIP
                   (block[0] == 0x37 && block[1] == 0x7A) ||  // 7Z
                   (block[0] == 0x42 && block[1] == 0x5A) ||  // BZIP2
                   (block[0] == 0x28 && block[1] == 0xB5) ||  // ZSTD
                   (block[0] == 0xFD && block[1] == 0x37) ||  // XZ
                   (block[0] == 0xFF && block[1] == 0xD8) ||  // JPEG
                   (block[0] == 0x89 && block[1] == 0x50);    // PNG
        }

        /// <summary>
        /// Fast entropy calculation using sampling (check every 16th byte for speed)
        /// </summary>
        private static double CalculateEntropy(byte[] block)
        {
            var freq = new int[256];
            int sampleSize = 0;

            // Sample-based entropy for speed (check every 16th byte)
            for (int i = 0; i < block.Length; i += 16)
            {
                freq[block[i]]++;
                sampleSize++;
            }

            double entropy = 0;
            foreach (var count in freq)
            {
                if (count > 0)
                {
                    double p = (double)count / sampleSize;
                    entropy -= p * Math.Log(p, 2);
                }
            }

            return entropy;
        }

        private static bool IsText(byte[] block)
        {
            int printable = 0;
            int whitespace = 0;

            foreach (var b in block)
            {
                if ((b >= 32 && b <= 126) || b == 9 || b == 10 || b == 13)
                {
                    printable++;
                    if (b == 32 || b == 9 || b == 10 || b == 13)
                        whitespace++;
                }
            }

            // Text should have high printable ratio and reasonable whitespace
            return printable > block.Length * 0.85 && whitespace > block.Length * 0.05;
        }

        private static bool IsRepetitive(byte[] block)
        {
            if (block.Length < 16)
                return false;

            // Count runs of repeated bytes
            int runs = 0;
            int runLength = 1;

            for (int i = 1; i < Math.Min(block.Length, 1024); i++)
            {
                if (block[i] == block[i - 1])
                {
                    runLength++;
                }
                else
                {
                    if (runLength >= 4)
                        runs++;
                    runLength = 1;
                }
            }

            return runs > 10;
        }

        private static bool IsNumeric(byte[] block)
        {
            if (block.Length < 16 || block.Length % 4 != 0)
                return false;

            // Check if data looks like integers with small deltas
            int smallDeltas = 0;
            int samples = Math.Min(block.Length / 4, 256);

            for (int i = 4; i < samples * 4; i += 4)
            {
                int val1 = BitConverter.ToInt32(block, i - 4);
                int val2 = BitConverter.ToInt32(block, i);
                long delta = Math.Abs((long)val2 - val1);

                if (delta < 1000)
                    smallDeltas++;
            }

            return smallDeltas > samples * 0.6;
        }

        public static CompressionMethod SelectCompressionMethod(DataType dataType)
        {
            return dataType switch
            {
                DataType.Text => CompressionMethod.Dictionary,
                DataType.Repetitive => CompressionMethod.RLE,
                DataType.Numeric => CompressionMethod.Delta,
                DataType.Binary => CompressionMethod.LZ,
                DataType.Random => CompressionMethod.None,
                _ => CompressionMethod.LZ
            };
        }
    }
}
