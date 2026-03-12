using System;
using System.Linq;
using ConsoleApp1.Compression;
using ConsoleApp1.Compression.Transformers;

namespace ConsoleApp1
{
    public static class TestSimple
    {
        public static void TestChunkPipeline()
        {
            Console.WriteLine("=== CHUNK PIPELINE TEST ===\n");
            
            // First test LZ in isolation
            Console.WriteLine("Testing LZ Compressor alone:");
            string text = "Hello World! ".Repeat(100); // 1300 bytes
            byte[] fullData = System.Text.Encoding.UTF8.GetBytes(text);
            var chunks = Chunker.ChunkData(fullData, 512);
            
            var chunk0 = chunks[0].Data;
            Console.WriteLine($"Chunk 0: {chunk0.Length} bytes");
            byte[] lzComp = LZCompressor.Compress(chunk0);
            Console.WriteLine($"LZ Compressed: {lzComp.Length} bytes");
            byte[] lzDecomp = LZCompressor.Decompress(lzComp);
            Console.WriteLine($"LZ Decompressed: {lzDecomp.Length} bytes");
            Console.WriteLine($"Match: {chunk0.SequenceEqual(lzDecomp)}");
            if (!chunk0.SequenceEqual(lzDecomp))
            {
                Console.WriteLine($"First 20 orig: {string.Join(",", chunk0.Take(20))}");
                Console.WriteLine($"First 20 decomp: {string.Join(",", lzDecomp.Take(20))}");
                Console.WriteLine($"Last 20 orig: {string.Join(",", chunk0.Skip(chunk0.Length - 20))}");
                Console.WriteLine($"Last 20 decomp: {string.Join(",", lzDecomp.Skip(lzDecomp.Length - 20))}");
            }
            Console.WriteLine();
            
            Console.WriteLine($"Created {chunks.Count} chunks:");
            foreach (var chunk in chunks)
            {
                Console.WriteLine($"  Chunk {chunk.BlockId}: {chunk.Data.Length} bytes");
                
                // Test each chunk through the full pipeline
                var dataType = DataAnalyzer.DetectDataType(chunk.Data);
                Console.WriteLine($"    Detected type: {dataType}");
                
                // Apply transformation
                byte[] transformed = dataType switch
                {
                    DataType.Numeric => DeltaEncoder.Encode(chunk.Data),
                    DataType.Repetitive => RLEEncoder.Encode(chunk.Data),
                    _ => chunk.Data
                };
                
                // LZ compress
                byte[] lzCompressed = LZCompressor.Compress(transformed);
                
                // ANS encode
                byte[] ansEncoded = ANSEncoder.Encode(lzCompressed);
                
                // Now reverse
                byte[] ansDecoded = ANSEncoder.Decode(ansEncoded);
                byte[] lzDecompressed = LZCompressor.Decompress(ansDecoded);
                
                // Reverse transformation
                byte[] final = dataType switch
                {
                    DataType.Numeric => DeltaEncoder.Decode(lzDecompressed),
                    DataType.Repetitive => RLEEncoder.Decode(lzDecompressed),
                    _ => lzDecompressed
                };
                
                bool match = chunk.Data.SequenceEqual(final);
                Console.WriteLine($"    Original: {chunk.Data.Length}, Final: {final.Length}, Match: {match}");
                
                if (!match)
                {
                    Console.WriteLine($"    ERROR: Chunk {chunk.BlockId} failed!");
                    Console.WriteLine($"    Transformed: {transformed.Length}, LZ: {lzCompressed.Length}, ANS: {ansEncoded.Length}");
                    Console.WriteLine($"    ANS decoded: {ansDecoded.Length}, LZ decoded: {lzDecompressed.Length}, Final: {final.Length}");
                }
            }
        }
    }
}
