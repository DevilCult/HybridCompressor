using System;
using System.Collections.Generic;
using System.IO;

namespace ConsoleApp1.Compression
{
    /// <summary>
    /// Content-aware chunking using rolling hash (Rabin fingerprinting)
    /// Finds natural boundaries in data for better deduplication
    /// </summary>
    public static class ContentAwareChunker
    {
        public const int MinChunkSize = 2 * 1024 * 1024;  // 2MB
        public const int AvgChunkSize = 4 * 1024 * 1024;  // 4MB
        public const int MaxChunkSize = 8 * 1024 * 1024;  // 8MB
        
        private const uint RollingHashMask = 0x000FFFFF;  // ~1MB average chunk size

        /// <summary>
        /// Chunk file using content-aware boundaries
        /// </summary>
        public static List<DataBlock> ChunkFileContentAware(string filePath)
        {
            var blocks = new List<DataBlock>();
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536))
            {
                int blockId = 0;
                long fileOffset = 0;
                var buffer = new List<byte>();
                uint rollingHash = 0;
                int bytesInChunk = 0;
                
                int b;
                while ((b = fs.ReadByte()) != -1)
                {
                    buffer.Add((byte)b);
                    bytesInChunk++;
                    
                    // Update rolling hash (simple polynomial rolling hash)
                    rollingHash = (rollingHash << 1) ^ (uint)b;
                    
                    // Check for chunk boundary
                    bool isNaturalBoundary = (rollingHash & RollingHashMask) == 0;
                    bool isMinSizeReached = bytesInChunk >= MinChunkSize;
                    bool isMaxSizeReached = bytesInChunk >= MaxChunkSize;
                    
                    // Create chunk at natural boundary (if min size reached) or at max size
                    if ((isNaturalBoundary && isMinSizeReached) || isMaxSizeReached)
                    {
                        var block = new DataBlock(blockId++, buffer.ToArray(), fileOffset);
                        blocks.Add(block);
                        
                        fileOffset += buffer.Count;
                        buffer.Clear();
                        bytesInChunk = 0;
                        rollingHash = 0;
                    }
                }
                
                // Add remaining data as final chunk
                if (buffer.Count > 0)
                {
                    blocks.Add(new DataBlock(blockId++, buffer.ToArray(), fileOffset));
                }
            }
            
            return blocks;
        }

        /// <summary>
        /// Chunk data using content-aware boundaries
        /// </summary>
        public static List<DataBlock> ChunkDataContentAware(byte[] data)
        {
            var blocks = new List<DataBlock>();
            int blockId = 0;
            long offset = 0;
            var buffer = new List<byte>();
            uint rollingHash = 0;
            int bytesInChunk = 0;
            
            for (int i = 0; i < data.Length; i++)
            {
                byte b = data[i];
                buffer.Add(b);
                bytesInChunk++;
                
                // Update rolling hash
                rollingHash = (rollingHash << 1) ^ (uint)b;
                
                // Check for chunk boundary
                bool isNaturalBoundary = (rollingHash & RollingHashMask) == 0;
                bool isMinSizeReached = bytesInChunk >= MinChunkSize;
                bool isMaxSizeReached = bytesInChunk >= MaxChunkSize;
                
                if ((isNaturalBoundary && isMinSizeReached) || isMaxSizeReached)
                {
                    var block = new DataBlock(blockId++, buffer.ToArray(), offset);
                    blocks.Add(block);
                    
                    offset += buffer.Count;
                    buffer.Clear();
                    bytesInChunk = 0;
                    rollingHash = 0;
                }
            }
            
            // Add remaining data
            if (buffer.Count > 0)
            {
                blocks.Add(new DataBlock(blockId++, buffer.ToArray(), offset));
            }
            
            return blocks;
        }
    }
}
