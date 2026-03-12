using System;
using System.Linq;
using ConsoleApp1.Compression;
using ConsoleApp1.Compression.Transformers;

namespace ConsoleApp1
{
    public static class TestDebug
    {
        public static void RunDebugTests()
        {
            Console.WriteLine("=== DEBUG TESTS ===\n");

            // Test 1: RLE Encoder
            Console.WriteLine("Test 1: RLE Encoder");
            byte[] rleInput = new byte[] { 1, 1, 1, 1, 1, 2, 2, 2, 2, 3 };
            byte[] rleEncoded = RLEEncoder.Encode(rleInput);
            byte[] rleDecoded = RLEEncoder.Decode(rleEncoded);
            Console.WriteLine($"Input:   {string.Join(",", rleInput)}");
            Console.WriteLine($"Encoded: {string.Join(",", rleEncoded)}");
            Console.WriteLine($"Decoded: {string.Join(",", rleDecoded)}");
            Console.WriteLine($"Match: {rleInput.SequenceEqual(rleDecoded)}");
            
            // Test RLE with text (like our actual data)
            byte[] textInput = System.Text.Encoding.UTF8.GetBytes("Hello World! Hello World! ");
            byte[] textEncoded = RLEEncoder.Encode(textInput);
            byte[] textDecoded = RLEEncoder.Decode(textEncoded);
            Console.WriteLine($"Text RLE - Input len: {textInput.Length}, Encoded: {textEncoded.Length}, Decoded: {textDecoded.Length}, Match: {textInput.SequenceEqual(textDecoded)}\n");

            // Test 2: Delta Encoder
            Console.WriteLine("Test 2: Delta Encoder");
            byte[] deltaInput = new byte[16];
            for (int i = 0; i < 4; i++)
            {
                byte[] intBytes = BitConverter.GetBytes(1000 + i);
                Array.Copy(intBytes, 0, deltaInput, i * 4, 4);
            }
            byte[] deltaEncoded = DeltaEncoder.Encode(deltaInput);
            byte[] deltaDecoded = DeltaEncoder.Decode(deltaEncoded);
            Console.WriteLine($"Input length:   {deltaInput.Length}");
            Console.WriteLine($"Decoded length: {deltaDecoded.Length}");
            Console.WriteLine($"Match: {deltaInput.SequenceEqual(deltaDecoded)}\n");

            // Test 3: LZ Compressor
            Console.WriteLine("Test 3: LZ Compressor");
            byte[] lzInput = System.Text.Encoding.UTF8.GetBytes("ABCABCABC");
            byte[] lzCompressed = LZCompressor.Compress(lzInput);
            byte[] lzDecompressed = LZCompressor.Decompress(lzCompressed);
            Console.WriteLine($"Input:        {System.Text.Encoding.UTF8.GetString(lzInput)}");
            Console.WriteLine($"Decompressed: {System.Text.Encoding.UTF8.GetString(lzDecompressed)}");
            Console.WriteLine($"Match: {lzInput.SequenceEqual(lzDecompressed)}\n");

            // Test 4: ANS Encoder
            Console.WriteLine("Test 4: ANS Encoder");
            byte[] ansInput = System.Text.Encoding.UTF8.GetBytes("Hello World!");
            byte[] ansEncoded = ANSEncoder.Encode(ansInput);
            byte[] ansDecoded = ANSEncoder.Decode(ansEncoded);
            Console.WriteLine($"Input:   {System.Text.Encoding.UTF8.GetString(ansInput)}");
            Console.WriteLine($"Decoded: {System.Text.Encoding.UTF8.GetString(ansDecoded)}");
            Console.WriteLine($"Match: {ansInput.SequenceEqual(ansDecoded)}\n");

            // Test 5: Full pipeline (simple)
            Console.WriteLine("Test 5: Full Pipeline (Small)");
            byte[] pipelineInput = System.Text.Encoding.UTF8.GetBytes("Test data");
            var compressor = new HybridCompressor();
            byte[] compressed = compressor.CompressData(pipelineInput, 1024);
            byte[] decompressed = compressor.DecompressData(compressed);
            Console.WriteLine($"Input:        '{System.Text.Encoding.UTF8.GetString(pipelineInput)}'");
            Console.WriteLine($"Input length: {pipelineInput.Length}");
            Console.WriteLine($"Comp length:  {compressed.Length}");
            Console.WriteLine($"Decomp len:   {decompressed.Length}");
            if (decompressed.Length > 0 && decompressed.Length <= 100)
            {
                Console.WriteLine($"Decompressed: '{System.Text.Encoding.UTF8.GetString(decompressed)}'");
            }
            Console.WriteLine($"Match: {pipelineInput.SequenceEqual(decompressed)}\n");

            // Test 6: Full pipeline (larger - multiple chunks)
            Console.WriteLine("Test 6: Full Pipeline (Large - Multiple Chunks)");
            string largeText = "Hello World! ".Repeat(100); // 1300 bytes
            byte[] largeInput = System.Text.Encoding.UTF8.GetBytes(largeText);
            compressor.Reset();
            
            // Test chunking first
            var chunks = Chunker.ChunkData(largeInput, 512);
            Console.WriteLine($"Number of chunks: {chunks.Count}");
            foreach (var chunk in chunks)
            {
                Console.WriteLine($"  Chunk {chunk.BlockId}: {chunk.OriginalSize} bytes");
            }
            
            byte[] largeCompressed = compressor.CompressData(largeInput, 512); // 512 byte chunks = 3 chunks
            byte[] largeDecompressed = compressor.DecompressData(largeCompressed);
            Console.WriteLine($"Input length:  {largeInput.Length}");
            Console.WriteLine($"Comp length:   {largeCompressed.Length}");
            Console.WriteLine($"Decomp length: {largeDecompressed.Length}");
            Console.WriteLine($"Match: {largeInput.SequenceEqual(largeDecompressed)}");
            if (!largeInput.SequenceEqual(largeDecompressed))
            {
                Console.WriteLine($"Difference: {largeInput.Length - largeDecompressed.Length} bytes");
                Console.WriteLine($"Last 20 bytes of input:  {string.Join(",", largeInput.Skip(largeInput.Length - 20))}");
                if (largeDecompressed.Length >= 20)
                    Console.WriteLine($"Last 20 bytes of output: {string.Join(",", largeDecompressed.Skip(largeDecompressed.Length - 20))}");
            }
            Console.WriteLine();
        }
    }
}
