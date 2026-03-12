using System;
using System.IO;
using System.IO.Compression;
using ZstdSharp;
using SharpCompress.Compressors.LZMA;

namespace ConsoleApp1.Compression
{
    /// <summary>
    /// Multi-algorithm LZ compressor supporting Deflate, Brotli, Zstd, and LZMA2
    /// </summary>
    public static class LZCompressor
    {
        public enum CompressionAlgorithm
        {
            Deflate,   // Legacy, fast but lower compression
            Brotli,    // Good compression, moderate speed
            Zstd,      // Best speed/compression balance ⭐ (recommended)
            LZMA2      // Best compression, slowest
        }

        public static byte[] Compress(byte[] data, byte[]? dictionary = null, CompressionAlgorithm algorithm = CompressionAlgorithm.Zstd)
        {
            if (data == null || data.Length == 0)
                return data ?? Array.Empty<byte>();

            // Always compress with Zstd - no fallback logic
            return CompressZstd(data);
        }

        public static byte[] Decompress(byte[] data, byte[]? dictionary = null, CompressionAlgorithm algorithm = CompressionAlgorithm.Zstd)
        {
            if (data == null || data.Length == 0)
                return data ?? Array.Empty<byte>();

            // Always decompress with Zstd - no fallback logic
            return DecompressZstd(data);
        }

        #region Deflate (Legacy)
        private static byte[] CompressDeflate(byte[] data)
        {
            using (var outputStream = new MemoryStream())
            {
                using (var deflateStream = new DeflateStream(outputStream, CompressionLevel.Optimal))
                {
                    deflateStream.Write(data, 0, data.Length);
                }
                return outputStream.ToArray();
            }
        }

        private static byte[] DecompressDeflate(byte[] data)
        {
            using (var inputStream = new MemoryStream(data))
            using (var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress))
            using (var outputStream = new MemoryStream())
            {
                deflateStream.CopyTo(outputStream);
                return outputStream.ToArray();
            }
        }
        #endregion

        #region Brotli
        private static byte[] CompressBrotli(byte[] data)
        {
            using (var outputStream = new MemoryStream())
            {
                using (var brotliStream = new BrotliStream(outputStream, CompressionLevel.SmallestSize))
                {
                    brotliStream.Write(data, 0, data.Length);
                }
                return outputStream.ToArray();
            }
        }

        private static byte[] DecompressBrotli(byte[] data)
        {
            using (var inputStream = new MemoryStream(data))
            using (var brotliStream = new BrotliStream(inputStream, CompressionMode.Decompress))
            using (var outputStream = new MemoryStream())
            {
                brotliStream.CopyTo(outputStream);
                return outputStream.ToArray();
            }
        }
        #endregion

        #region Zstd (Recommended)
        private static byte[] CompressZstd(byte[] data)
        {
            using (var compressor = new Compressor(22))  // Level 22 = maximum compression
            {
                return compressor.Wrap(data).ToArray();
            }
        }

        private static byte[] DecompressZstd(byte[] data)
        {
            using (var decompressor = new Decompressor())
            {
                return decompressor.Unwrap(data).ToArray();
            }
        }
        #endregion

        #region LZMA2 (Maximum Compression)
        private static byte[] CompressLZMA2(byte[] data)
        {
            using (var inputStream = new MemoryStream(data))
            using (var outputStream = new MemoryStream())
            {
                // Use default LZMA encoder properties for maximum compression
                var encoder = new LzmaStream(new LzmaEncoderProperties(), false, outputStream);
                inputStream.CopyTo(encoder);
                encoder.Flush();
                encoder.Dispose();
                return outputStream.ToArray();
            }
        }

        private static byte[] DecompressLZMA2(byte[] data)
        {
            using (var inputStream = new MemoryStream(data))
            using (var outputStream = new MemoryStream())
            {
                var decoder = new LzmaStream(new LzmaEncoderProperties(), false, inputStream);
                decoder.CopyTo(outputStream);
                decoder.Dispose();
                return outputStream.ToArray();
            }
        }
        #endregion
    }
}
