using System;
using System.Collections.Generic;
using System.IO;

namespace ConsoleApp1.Compression
{
    /// <summary>
    /// Splits files into independent blocks for parallel processing
    /// </summary>
    public static class Chunker
    {
        public const int DefaultChunkSize = 4 * 1024 * 1024; // 4MB chunks for better compression

        public static List<DataBlock> ChunkFile(string filePath, int chunkSize = DefaultChunkSize)
        {
            var blocks = new List<DataBlock>();
            
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192))
            {
                int blockId = 0;
                long fileOffset = 0;
                byte[] buffer = new byte[chunkSize];
                int bytesRead;

                while ((bytesRead = fs.Read(buffer, 0, chunkSize)) > 0)
                {
                    byte[] blockData = new byte[bytesRead];
                    Array.Copy(buffer, blockData, bytesRead);

                    var block = new DataBlock(blockId++, blockData, fileOffset);
                    blocks.Add(block);

                    fileOffset += bytesRead;
                }
            }

            return blocks;
        }

        public static List<DataBlock> ChunkData(byte[] data, int chunkSize = DefaultChunkSize)
        {
            var blocks = new List<DataBlock>();
            int blockId = 0;
            long fileOffset = 0;

            for (int i = 0; i < data.Length; i += chunkSize)
            {
                int size = Math.Min(chunkSize, data.Length - i);
                byte[] blockData = new byte[size];
                Array.Copy(data, i, blockData, 0, size);

                var block = new DataBlock(blockId++, blockData, fileOffset);
                blocks.Add(block);

                fileOffset += size;
            }

            return blocks;
        }
    }
}
