using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ConsoleApp1.Compression.Transformers;

namespace ConsoleApp1.Compression
{
    /// <summary>
    /// Main hybrid compression engine that orchestrates the entire pipeline
    /// Enhanced with multiple compression algorithms and configurable settings
    /// </summary>
    public class HybridCompressor
    {
        private readonly DeduplicationEngine _deduplicationEngine = new DeduplicationEngine();
        private byte[] _globalDictionary = Array.Empty<byte>();
        private CompressionSettings _settings = CompressionSettings.FromPreset(CompressionPreset.Balanced);

        public class CompressionResult
        {
            public byte[] CompressedData { get; set; } = Array.Empty<byte>();
            public long OriginalSize { get; set; }
            public long CompressedSize { get; set; }
            public double CompressionRatio { get; set; }
            public TimeSpan CompressionTime { get; set; }
            public int BlockCount { get; set; }
            public int UniqueBlocks { get; set; }
            public long BytesSavedByDeduplication { get; set; }
            public string AlgorithmUsed { get; set; } = string.Empty;
        }

        /// <summary>
        /// Set compression settings
        /// </summary>
        public void SetSettings(CompressionSettings settings)
        {
            _settings = settings ?? CompressionSettings.FromPreset(CompressionPreset.Balanced);
        }

        /// <summary>
        /// Set compression preset
        /// </summary>
        public void SetPreset(CompressionPreset preset)
        {
            _settings = CompressionSettings.FromPreset(preset);
        }

        public CompressionResult CompressFile(string filePath, int chunkSize = 0)
        {
            if (chunkSize == 0)
                chunkSize = _settings.ChunkSize;

            var startTime = DateTime.Now;

            // Step 1: Chunk the file (content-aware if enabled)
            var blocks = _settings.UseContentAwareChunking
                ? ContentAwareChunker.ChunkFileContentAware(filePath)
                : Chunker.ChunkFile(filePath, chunkSize);
            long originalSize = blocks.Sum(b => b.OriginalSize);

            // Step 2: Deduplicate blocks
            var deduplicationResult = _deduplicationEngine.Deduplicate(blocks);

            // Step 3: Parallel compression of unique blocks
            var uniqueBlocksList = deduplicationResult.UniqueBlocks.ToList();
            var compressedBlocks = new Dictionary<int, byte[]>();
            var blockMetadata = new Dictionary<int, DataType>();
            var blockAlgorithms = new Dictionary<int, LZCompressor.CompressionAlgorithm>();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = _settings.MaxDegreeOfParallelism
            };

            Parallel.ForEach(uniqueBlocksList, parallelOptions, kvp =>
            {
                int blockId = kvp.Key;
                byte[] data = kvp.Value;

                // Analyze data type
                var dataType = DataAnalyzer.DetectDataType(data);

                // Select best algorithm for this specific data type
                var algorithm = DataAnalyzer.SelectBestAlgorithm(dataType, data.Length);

                // Apply transformation based on data type
                byte[] transformed = ApplyTransformation(data, dataType);

                // Apply LZ compression with optimal algorithm for this data type
                byte[] compressed = LZCompressor.Compress(transformed, _globalDictionary, algorithm);

                lock (compressedBlocks)
                {
                    compressedBlocks[blockId] = compressed;
                    blockMetadata[blockId] = dataType;
                    blockAlgorithms[blockId] = algorithm;
                }
            });

            // Step 4: Build archive
            var archiveData = BuildArchive(deduplicationResult.BlockReferences, compressedBlocks, blockMetadata, blockAlgorithms);

            var result = new CompressionResult
            {
                CompressedData = archiveData,
                OriginalSize = originalSize,
                CompressedSize = archiveData.Length,
                CompressionRatio = (double)originalSize / archiveData.Length,
                CompressionTime = DateTime.Now - startTime,
                BlockCount = blocks.Count,
                UniqueBlocks = deduplicationResult.UniqueBlockCount,
                BytesSavedByDeduplication = deduplicationResult.BytesSaved,
                AlgorithmUsed = _settings.Algorithm.ToString()
            };

            return result;
        }

        public byte[] CompressData(byte[] data, int chunkSize = 0)
        {
            if (chunkSize == 0)
                chunkSize = _settings.ChunkSize;

            var startTime = DateTime.Now;

            // Step 1: Chunk the data (content-aware if enabled)
            var blocks = _settings.UseContentAwareChunking
                ? ContentAwareChunker.ChunkDataContentAware(data)
                : Chunker.ChunkData(data, chunkSize);

            // Step 2: Deduplicate blocks
            var deduplicationResult = _deduplicationEngine.Deduplicate(blocks);

            // Step 3: Parallel compression of unique blocks
            var uniqueBlocksList = deduplicationResult.UniqueBlocks.ToList();
            var compressedBlocks = new Dictionary<int, byte[]>();
            var blockMetadata = new Dictionary<int, DataType>();
            var blockAlgorithms = new Dictionary<int, LZCompressor.CompressionAlgorithm>();

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = _settings.MaxDegreeOfParallelism
            };

            Parallel.ForEach(uniqueBlocksList, parallelOptions, kvp =>
            {
                int blockId = kvp.Key;
                byte[] blockData = kvp.Value;

                // Analyze data type
                var dataType = DataAnalyzer.DetectDataType(blockData);

                // Select best algorithm for this specific data type
                var algorithm = DataAnalyzer.SelectBestAlgorithm(dataType, blockData.Length);

                // Apply transformation based on data type
                byte[] transformed = ApplyTransformation(blockData, dataType);

                // Apply LZ compression with optimal algorithm for this data type
                byte[] compressed = LZCompressor.Compress(transformed, _globalDictionary, algorithm);

                lock (compressedBlocks)
                {
                    compressedBlocks[blockId] = compressed;
                    blockMetadata[blockId] = dataType;
                    blockAlgorithms[blockId] = algorithm;
                }
            });

            // Step 4: Build archive
            return BuildArchive(deduplicationResult.BlockReferences, compressedBlocks, blockMetadata, blockAlgorithms);
        }

        public byte[] DecompressData(byte[] compressedData)
        {
            // Parse archive
            var (blockReferences, compressedBlocks, blockMetadata, blockAlgorithms) = ParseArchive(compressedData);

            // Decompress unique blocks
            // NOTE: Using sequential processing to avoid thread-safety issues with shared state
            // The deduplication engine and transformations are not thread-safe when used with shared state
            var decompressedBlocks = new Dictionary<int, byte[]>();

            foreach (var kvp in compressedBlocks)
            {
                int blockId = kvp.Key;
                byte[] compressed = kvp.Value;

                // Get the algorithm used for this specific block
                var algorithm = blockAlgorithms.ContainsKey(blockId)
                    ? blockAlgorithms[blockId]
                    : LZCompressor.CompressionAlgorithm.Zstd;

                // Reverse LZ compression with the algorithm used during compression
                byte[] lzDecoded = LZCompressor.Decompress(compressed, _globalDictionary, algorithm);

                // Reverse transformation using stored metadata
                DataType dataType = blockMetadata.ContainsKey(blockId) ? blockMetadata[blockId] : DataType.Unknown;
                byte[] final = ReverseTransformation(lzDecoded, dataType);

                decompressedBlocks[blockId] = final;
            }

            // Restore blocks using deduplication map
            var restoredBlocks = _deduplicationEngine.Restore(blockReferences, decompressedBlocks);

            // Combine blocks
            return CombineBlocks(restoredBlocks);
        }

        private byte[] ApplyTransformation(byte[] data, DataType dataType)
        {
            return dataType switch
            {
                DataType.Numeric => DeltaEncoder.Encode(data),
                DataType.Repetitive => RLEEncoder.Encode(data),
                _ => data
            };
        }

        private byte[] ReverseTransformation(byte[] data, DataType dataType)
        {
            return dataType switch
            {
                DataType.Numeric => DeltaEncoder.Decode(data),
                DataType.Repetitive => RLEEncoder.Decode(data),
                _ => data
            };
        }

        private byte[] BuildArchive(List<int> blockReferences, Dictionary<int, byte[]> compressedBlocks,
            Dictionary<int, DataType> blockMetadata, Dictionary<int, LZCompressor.CompressionAlgorithm> blockAlgorithms)
        {
            var result = new List<byte>();

            // Magic number
            result.AddRange(new byte[] { 0x48, 0x43, 0x4D, 0x50 }); // "HCMP"

            // Version 3 (per-block algorithm support)
            result.Add(3);

            // Block reference count
            result.AddRange(BitConverter.GetBytes(blockReferences.Count));

            // Block references
            foreach (int blockId in blockReferences)
            {
                result.AddRange(BitConverter.GetBytes(blockId));
            }

            // Unique block count
            result.AddRange(BitConverter.GetBytes(compressedBlocks.Count));

            // Compressed blocks with metadata and algorithm
            foreach (var kvp in compressedBlocks.OrderBy(x => x.Key))
            {
                result.AddRange(BitConverter.GetBytes(kvp.Key)); // Block ID
                result.Add((byte)(blockMetadata.ContainsKey(kvp.Key) ? blockMetadata[kvp.Key] : DataType.Unknown)); // Data type
                result.Add((byte)(blockAlgorithms.ContainsKey(kvp.Key) ? blockAlgorithms[kvp.Key] : LZCompressor.CompressionAlgorithm.Zstd)); // Algorithm
                result.AddRange(BitConverter.GetBytes(kvp.Value.Length)); // Size
                result.AddRange(kvp.Value); // Data
            }

            return result.ToArray();
        }

        private (List<int> BlockReferences, Dictionary<int, byte[]> CompressedBlocks,
            Dictionary<int, DataType> BlockMetadata, Dictionary<int, LZCompressor.CompressionAlgorithm> BlockAlgorithms) ParseArchive(byte[] data)
        {
            int pos = 0;

            // Check magic number
            if (data.Length < 4 || data[0] != 0x48 || data[1] != 0x43 || data[2] != 0x4D || data[3] != 0x50)
            {
                throw new InvalidOperationException("Invalid archive format");
            }
            pos += 4;

            // Version
            byte version = data[pos++];

            // Read global algorithm (version 2 only, deprecated in v3)
            LZCompressor.CompressionAlgorithm globalAlgorithm = LZCompressor.CompressionAlgorithm.Zstd;
            if (version == 2)
            {
                globalAlgorithm = (LZCompressor.CompressionAlgorithm)data[pos++];
            }

            // Block reference count
            int refCount = BitConverter.ToInt32(data, pos);
            pos += 4;

            // Block references
            var blockReferences = new List<int>();
            for (int i = 0; i < refCount; i++)
            {
                blockReferences.Add(BitConverter.ToInt32(data, pos));
                pos += 4;
            }

            // Unique block count
            int uniqueCount = BitConverter.ToInt32(data, pos);
            pos += 4;

            // Compressed blocks with metadata
            var compressedBlocks = new Dictionary<int, byte[]>();
            var blockMetadata = new Dictionary<int, DataType>();
            var blockAlgorithms = new Dictionary<int, LZCompressor.CompressionAlgorithm>();
            
            for (int i = 0; i < uniqueCount; i++)
            {
                int blockId = BitConverter.ToInt32(data, pos);
                pos += 4;

                DataType dataType = (DataType)data[pos++];
                blockMetadata[blockId] = dataType;

                // Read per-block algorithm (version 3+)
                LZCompressor.CompressionAlgorithm blockAlgorithm = globalAlgorithm;
                if (version >= 3)
                {
                    blockAlgorithm = (LZCompressor.CompressionAlgorithm)data[pos++];
                }
                blockAlgorithms[blockId] = blockAlgorithm;

                int size = BitConverter.ToInt32(data, pos);
                pos += 4;

                byte[] blockData = new byte[size];
                Array.Copy(data, pos, blockData, 0, size);
                pos += size;

                compressedBlocks[blockId] = blockData;
            }

            return (blockReferences, compressedBlocks, blockMetadata, blockAlgorithms);
        }

        private byte[] CombineBlocks(List<byte[]> blocks)
        {
            int totalSize = blocks.Sum(b => b.Length);
            byte[] result = new byte[totalSize];
            int offset = 0;

            foreach (var block in blocks)
            {
                Array.Copy(block, 0, result, offset, block.Length);
                offset += block.Length;
            }

            return result;
        }

        public void SetGlobalDictionary(byte[] dictionary)
        {
            _globalDictionary = dictionary;
        }

        public void Reset()
        {
            _deduplicationEngine.Reset();
            _globalDictionary = Array.Empty<byte>();
        }
    }
}
