using System;

namespace ConsoleApp1.Compression
{
    /// <summary>
    /// Compression presets for different use cases
    /// </summary>
    public enum CompressionPreset
    {
        Fastest,      // Zstd level 1, 2MB chunks - for real-time compression
        Fast,         // Zstd level 10, 4MB chunks - for backups
        Balanced,     // Zstd max, 4MB chunks - default, best speed/ratio
        Best,         // LZMA2, 8MB chunks, content-aware - for archival
        Ultra         // LZMA2 max, 16MB chunks, dictionary training - maximum compression
    }

    /// <summary>
    /// Configurable compression settings
    /// </summary>
    public class CompressionSettings
    {
        public CompressionPreset Preset { get; set; } = CompressionPreset.Balanced;
        public int ChunkSize { get; set; } = 4 * 1024 * 1024;
        public LZCompressor.CompressionAlgorithm Algorithm { get; set; } = LZCompressor.CompressionAlgorithm.Zstd;
        public bool UseContentAwareChunking { get; set; } = false;
        public bool UseDictionaryTraining { get; set; } = false;
        public int MaxDegreeOfParallelism { get; set; } = Environment.ProcessorCount;

        /// <summary>
        /// Create settings from a preset
        /// </summary>
        public static CompressionSettings FromPreset(CompressionPreset preset)
        {
            return preset switch
            {
                CompressionPreset.Fastest => new CompressionSettings
                {
                    Preset = preset,
                    ChunkSize = 2 * 1024 * 1024,
                    Algorithm = LZCompressor.CompressionAlgorithm.Deflate,
                    UseContentAwareChunking = false,
                    UseDictionaryTraining = false
                },
                CompressionPreset.Fast => new CompressionSettings
                {
                    Preset = preset,
                    ChunkSize = 4 * 1024 * 1024,
                    Algorithm = LZCompressor.CompressionAlgorithm.Zstd,
                    UseContentAwareChunking = false,
                    UseDictionaryTraining = false
                },
                CompressionPreset.Best => new CompressionSettings
                {
                    Preset = preset,
                    ChunkSize = 8 * 1024 * 1024,
                    Algorithm = LZCompressor.CompressionAlgorithm.LZMA2,
                    UseContentAwareChunking = true,
                    UseDictionaryTraining = false
                },
                CompressionPreset.Ultra => new CompressionSettings
                {
                    Preset = preset,
                    ChunkSize = 16 * 1024 * 1024,
                    Algorithm = LZCompressor.CompressionAlgorithm.LZMA2,
                    UseContentAwareChunking = true,
                    UseDictionaryTraining = true
                },
                _ => new CompressionSettings  // Balanced (default)
                {
                    Preset = preset,
                    ChunkSize = 4 * 1024 * 1024,
                    Algorithm = LZCompressor.CompressionAlgorithm.Zstd,
                    UseContentAwareChunking = false,
                    UseDictionaryTraining = false
                }
            };
        }

        /// <summary>
        /// Get a human-readable description of the preset
        /// </summary>
        public string GetDescription()
        {
            return Preset switch
            {
                CompressionPreset.Fastest => "Fastest compression - ideal for real-time use",
                CompressionPreset.Fast => "Fast compression - good for backups",
                CompressionPreset.Balanced => "Balanced - best speed/compression ratio (recommended)",
                CompressionPreset.Best => "Best compression - ideal for archival",
                CompressionPreset.Ultra => "Ultra compression - maximum compression ratio",
                _ => "Custom settings"
            };
        }
    }
}
