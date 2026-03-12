using System;
using System.IO;
using System.Diagnostics;
using ConsoleApp1.Compression;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║   HYBRID COMPRESSOR - Advanced Multi-Algorithm Compression ║");
            Console.WriteLine("║   Designed to outperform 7-Zip through hybrid techniques  ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            if (args.Length == 0)
            {
                ShowUsage();
                return;
            }

            string command = args[0].ToLower();

            try
            {
                switch (command)
                {
                    case "compress":
                    case "c":
                        if (args.Length < 3)
                        {
                            Console.WriteLine("Error: Missing arguments for compress command");
                            ShowUsage();
                            return;
                        }
                        CompressFile(args[1], args[2]);
                        break;

                    case "decompress":
                    case "d":
                        if (args.Length < 3)
                        {
                            Console.WriteLine("Error: Missing arguments for decompress command");
                            ShowUsage();
                            return;
                        }
                        DecompressFile(args[1], args[2]);
                        break;

                    case "test":
                    case "t":
                        RunTests();
                        break;

                    case "benchmark":
                    case "b":
                        if (args.Length < 2)
                        {
                            Console.WriteLine("Error: Missing file argument for benchmark");
                            ShowUsage();
                            return;
                        }
                        BenchmarkFile(args[1]);
                        break;

                    case "compare":
                    case "cmp":
                        if (args.Length < 2)
                        {
                            Console.WriteLine("Error: Missing file argument for compare");
                            ShowUsage();
                            return;
                        }
                        CompressionBenchmark.RunComparison(args[1]);
                        break;

                    case "batchcompare":
                    case "bcmp":
                        if (args.Length < 2)
                        {
                            Console.WriteLine("Running batch comparison on generated test files...");
                            TestDataGenerator.GenerateTestFiles();
                            var testFiles = TestDataGenerator.GetGeneratedTestFiles();
                            CompressionBenchmark.RunBatchComparison(testFiles);
                        }
                        else
                        {
                            // Use provided files
                            var files = new string[args.Length - 1];
                            Array.Copy(args, 1, files, 0, args.Length - 1);
                            CompressionBenchmark.RunBatchComparison(files);
                        }
                        break;

                    case "generatetests":
                    case "gentests":
                        TestDataGenerator.GenerateTestFiles();
                        break;

                    case "debug":
                        TestDebug.RunDebugTests();
                        break;

                    case "simple":
                        TestSimple.TestChunkPipeline();
                        break;

                    default:
                        Console.WriteLine($"Unknown command: {command}");
                        ShowUsage();
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        static void ShowUsage()
        {
            Console.WriteLine("Usage:");
            Console.WriteLine("  compress <input-file> <output-file>     Compress a file");
            Console.WriteLine("  decompress <input-file> <output-file>   Decompress a file");
            Console.WriteLine("  test                                     Run compression tests");
            Console.WriteLine("  benchmark <file>                         Benchmark compression on a file");
            Console.WriteLine("  compare <file>                           Compare HybridCompressor vs ZIP");
            Console.WriteLine("  batchcompare [files...]                  Batch comparison (generates test files if none provided)");
            Console.WriteLine("  generatetests                            Generate test data files");
            Console.WriteLine();
            Console.WriteLine("Short forms:");
            Console.WriteLine("  c = compress, d = decompress, t = test, b = benchmark");
            Console.WriteLine("  cmp = compare, bcmp = batchcompare, gentests = generatetests");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  ConsoleApp1.exe compress document.txt document.hcmp");
            Console.WriteLine("  ConsoleApp1.exe decompress document.hcmp document.txt");
            Console.WriteLine("  ConsoleApp1.exe test");
            Console.WriteLine("  ConsoleApp1.exe benchmark largefile.bin");
            Console.WriteLine("  ConsoleApp1.exe compare test.txt");
            Console.WriteLine("  ConsoleApp1.exe batchcompare");
            Console.WriteLine("  ConsoleApp1.exe batchcompare file1.txt file2.bin file3.log");
        }

        static void CompressFile(string inputPath, string outputPath)
        {
            if (!File.Exists(inputPath))
            {
                Console.WriteLine($"Error: Input file '{inputPath}' not found");
                return;
            }

            Console.WriteLine($"Compressing: {inputPath}");
            Console.WriteLine($"Output: {outputPath}");
            Console.WriteLine();

            var compressor = new HybridCompressor();
            var stopwatch = Stopwatch.StartNew();

            var result = compressor.CompressFile(inputPath);

            stopwatch.Stop();

            // Write compressed data to file
            File.WriteAllBytes(outputPath, result.CompressedData);

            // Display results
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    COMPRESSION RESULTS                     ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            Console.WriteLine($"Original Size:        {FormatBytes(result.OriginalSize)}");
            Console.WriteLine($"Compressed Size:      {FormatBytes(result.CompressedSize)}");
            Console.WriteLine($"Compression Ratio:    {result.CompressionRatio:F2}x");
            Console.WriteLine($"Space Saved:          {FormatBytes(result.OriginalSize - result.CompressedSize)} ({GetPercentage(result.OriginalSize, result.CompressedSize):F1}%)");
            Console.WriteLine($"Compression Time:     {result.CompressionTime.TotalSeconds:F2} seconds");
            Console.WriteLine($"Speed:                {FormatBytes((long)(result.OriginalSize / result.CompressionTime.TotalSeconds))}/s");
            Console.WriteLine($"Total Blocks:         {result.BlockCount}");
            Console.WriteLine($"Unique Blocks:        {result.UniqueBlocks}");
            Console.WriteLine($"Deduplication Saved:  {FormatBytes(result.BytesSavedByDeduplication)}");
            Console.WriteLine();
            Console.WriteLine("✓ Compression completed successfully!");
        }

        static void DecompressFile(string inputPath, string outputPath)
        {
            if (!File.Exists(inputPath))
            {
                Console.WriteLine($"Error: Input file '{inputPath}' not found");
                return;
            }

            Console.WriteLine($"Decompressing: {inputPath}");
            Console.WriteLine($"Output: {outputPath}");
            Console.WriteLine();

            var compressor = new HybridCompressor();
            var stopwatch = Stopwatch.StartNew();

            byte[] compressedData = File.ReadAllBytes(inputPath);
            byte[] decompressedData = compressor.DecompressData(compressedData);

            stopwatch.Stop();

            // Write decompressed data to file
            File.WriteAllBytes(outputPath, decompressedData);

            // Display results
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                   DECOMPRESSION RESULTS                    ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            Console.WriteLine($"Compressed Size:      {FormatBytes(compressedData.Length)}");
            Console.WriteLine($"Decompressed Size:    {FormatBytes(decompressedData.Length)}");
            Console.WriteLine($"Decompression Time:   {stopwatch.Elapsed.TotalSeconds:F2} seconds");
            Console.WriteLine($"Speed:                {FormatBytes((long)(decompressedData.Length / stopwatch.Elapsed.TotalSeconds))}/s");
            Console.WriteLine();
            Console.WriteLine("✓ Decompression completed successfully!");
        }

        static void RunTests()
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    RUNNING TEST SUITE                      ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            var compressor = new HybridCompressor();

            // Test 1: Text data
            Console.WriteLine("Test 1: Text Data Compression");
            Console.WriteLine("─────────────────────────────────────────────────────────────");
            string textData = "Hello World! ".Repeat(1000);
            TestCompression(compressor, System.Text.Encoding.UTF8.GetBytes(textData), "Text");

            // Test 2: Repetitive binary data
            Console.WriteLine("\nTest 2: Repetitive Binary Data");
            Console.WriteLine("─────────────────────────────────────────────────────────────");
            byte[] repetitiveData = new byte[10000];
            for (int i = 0; i < repetitiveData.Length; i++)
                repetitiveData[i] = (byte)(i % 10);
            TestCompression(compressor, repetitiveData, "Repetitive");

            // Test 3: Numeric data
            Console.WriteLine("\nTest 3: Numeric Data (Sequential Integers)");
            Console.WriteLine("─────────────────────────────────────────────────────────────");
            byte[] numericData = new byte[10000];
            for (int i = 0; i < numericData.Length / 4; i++)
            {
                byte[] intBytes = BitConverter.GetBytes(1000 + i);
                Array.Copy(intBytes, 0, numericData, i * 4, 4);
            }
            TestCompression(compressor, numericData, "Numeric");

            // Test 4: Random data
            Console.WriteLine("\nTest 4: Random Data (Incompressible)");
            Console.WriteLine("─────────────────────────────────────────────────────────────");
            byte[] randomData = new byte[10000];
            new Random(42).NextBytes(randomData);
            TestCompression(compressor, randomData, "Random");

            // Test 5: Deduplication test
            Console.WriteLine("\nTest 5: Deduplication (Repeated Blocks)");
            Console.WriteLine("─────────────────────────────────────────────────────────────");
            byte[] block = System.Text.Encoding.UTF8.GetBytes("This is a repeated block of data. ");
            byte[] deduplicationData = new byte[block.Length * 100];
            for (int i = 0; i < 100; i++)
                Array.Copy(block, 0, deduplicationData, i * block.Length, block.Length);
            TestCompression(compressor, deduplicationData, "Deduplication");

            Console.WriteLine("\n╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                   ALL TESTS COMPLETED                      ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
        }

        static void TestCompression(HybridCompressor compressor, byte[] data, string testName)
        {
            try
            {
                // Compress
                var stopwatch = Stopwatch.StartNew();
                byte[] compressed = compressor.CompressData(data);
                var compressTime = stopwatch.Elapsed;

                // Decompress
                stopwatch.Restart();
                byte[] decompressed = compressor.DecompressData(compressed);
                var decompressTime = stopwatch.Elapsed;

                // Verify
                bool isValid = data.Length == decompressed.Length;
                if (isValid)
                {
                    for (int i = 0; i < data.Length; i++)
                    {
                        if (data[i] != decompressed[i])
                        {
                            isValid = false;
                            break;
                        }
                    }
                }

                double ratio = (double)data.Length / compressed.Length;

                Console.WriteLine($"Original:     {FormatBytes(data.Length)}");
                Console.WriteLine($"Compressed:   {FormatBytes(compressed.Length)}");
                Console.WriteLine($"Ratio:        {ratio:F2}x");
                Console.WriteLine($"Saved:        {GetPercentage(data.Length, compressed.Length):F1}%");
                Console.WriteLine($"Compress:     {compressTime.TotalMilliseconds:F2} ms");
                Console.WriteLine($"Decompress:   {decompressTime.TotalMilliseconds:F2} ms");
                Console.WriteLine($"Verification: {(isValid ? "✓ PASSED" : "✗ FAILED")}");

                compressor.Reset();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Test failed: {ex.Message}");
            }
        }

        static void BenchmarkFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"Error: File '{filePath}' not found");
                return;
            }

            Console.WriteLine($"Benchmarking: {filePath}");
            Console.WriteLine();

            var fileInfo = new FileInfo(filePath);
            Console.WriteLine($"File Size: {FormatBytes(fileInfo.Length)}");
            Console.WriteLine();

            var compressor = new HybridCompressor();

            // Test with different chunk sizes
            int[] chunkSizes = { 256 * 1024, 512 * 1024, 1024 * 1024, 2048 * 1024 };

            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║              CHUNK SIZE BENCHMARK RESULTS                  ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            foreach (int chunkSize in chunkSizes)
            {
                Console.WriteLine($"Chunk Size: {FormatBytes(chunkSize)}");
                Console.WriteLine("─────────────────────────────────────────────────────────────");

                var result = compressor.CompressFile(filePath, chunkSize);

                Console.WriteLine($"Compressed Size:  {FormatBytes(result.CompressedSize)}");
                Console.WriteLine($"Ratio:            {result.CompressionRatio:F2}x");
                Console.WriteLine($"Time:             {result.CompressionTime.TotalSeconds:F2}s");
                Console.WriteLine($"Speed:            {FormatBytes((long)(result.OriginalSize / result.CompressionTime.TotalSeconds))}/s");
                Console.WriteLine($"Unique Blocks:    {result.UniqueBlocks}/{result.BlockCount}");
                Console.WriteLine();

                compressor.Reset();
            }
        }

        static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:F2} {sizes[order]}";
        }

        static double GetPercentage(long original, long compressed)
        {
            if (original == 0) return 0;
            return ((double)(original - compressed) / original) * 100;
        }
    }

    // Extension method for string repetition
    public static class StringExtensions
    {
        public static string Repeat(this string text, int count)
        {
            var result = new System.Text.StringBuilder(text.Length * count);
            for (int i = 0; i < count; i++)
                result.Append(text);
            return result.ToString();
        }
    }
}
