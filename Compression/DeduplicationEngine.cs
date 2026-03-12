using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleApp1.Compression
{
    /// <summary>
    /// Deduplication engine to detect and eliminate duplicate blocks
    /// </summary>
    public class DeduplicationEngine
    {
        private Dictionary<string, int> _hashToBlockId = new Dictionary<string, int>();
        private Dictionary<int, byte[]> _uniqueBlocks = new Dictionary<int, byte[]>();
        private int _nextUniqueId = 0;

        public class DeduplicationResult
        {
            public List<int> BlockReferences { get; set; } = new List<int>();
            public Dictionary<int, byte[]> UniqueBlocks { get; set; } = new Dictionary<int, byte[]>();
            public int OriginalBlocks { get; set; }
            public int UniqueBlockCount { get; set; }
            public long BytesSaved { get; set; }
        }

        public DeduplicationResult Deduplicate(List<DataBlock> blocks)
        {
            var result = new DeduplicationResult
            {
                OriginalBlocks = blocks.Count
            };

            long totalSize = 0;
            long uniqueSize = 0;

            foreach (var block in blocks)
            {
                totalSize += block.Data.Length;

                if (_hashToBlockId.TryGetValue(block.Hash, out int existingId))
                {
                    // Duplicate found - reference existing block
                    result.BlockReferences.Add(existingId);
                }
                else
                {
                    // New unique block
                    int uniqueId = _nextUniqueId++;
                    _hashToBlockId[block.Hash] = uniqueId;
                    _uniqueBlocks[uniqueId] = block.Data;
                    result.BlockReferences.Add(uniqueId);
                    uniqueSize += block.Data.Length;
                }
            }

            result.UniqueBlocks = new Dictionary<int, byte[]>(_uniqueBlocks);
            result.UniqueBlockCount = _uniqueBlocks.Count;
            result.BytesSaved = totalSize - uniqueSize;

            return result;
        }

        public List<byte[]> Restore(List<int> blockReferences, Dictionary<int, byte[]> uniqueBlocks)
        {
            var result = new List<byte[]>();

            foreach (int blockId in blockReferences)
            {
                if (uniqueBlocks.TryGetValue(blockId, out byte[]? data))
                {
                    result.Add(data);
                }
                else
                {
                    throw new InvalidOperationException($"Block {blockId} not found in unique blocks");
                }
            }

            return result;
        }

        public void Reset()
        {
            _hashToBlockId.Clear();
            _uniqueBlocks.Clear();
            _nextUniqueId = 0;
        }
    }
}
