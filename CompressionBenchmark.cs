using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using ConsoleApp1.Compression;
using SharpCompress.Writers;
using SharpCompress.Writers.Zip;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace ConsoleApp1
{
    public class BenchmarkResult
    {
        public string CompressorName { get; set; }
        public long OriginalSize { get; set; }
        public long CompressedSize { get; set; }
        public double CompressionRatio { get; set; }
        public double CompressionTimeMs { get; set; }
        public double DecompressionTimeMs { get; set; }
        public double CompressionSpeedMBps { get; set; }
        public double DecompressionSpeedMBps { get; set; }
        public double SpaceSavedPercent { get; set; }
        public bool VerificationPassed { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class CompressionBenchmark
    {
        private const int WARMUP_RUNS = 1;
        private const int BENCHMARK_RUNS = 3;

        public static void RunComparison(string inputFile)
        {
            if (!File.Exists(inputFile))
            {
                Console.WriteLine($"Error: File '{inputFile}' not found");
                return;
            }

            var fileInfo = new FileInfo(inputFile);
            Console.WriteLine("╔════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║          COMPRESSION BENCHMARK: HybridCompressor vs ZIP                ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();
            Console.WriteLine($"Test File: {Path.GetFileName(inputFile)}");
            Console.WriteLine($"File Size: {FormatBytes(fileInfo.Length)}");
            Console.WriteLine($"File Type: {Path.GetExtension(inputFile)}");
            Console.WriteLine();

            // Read original data once
            byte[] originalData = File.ReadAllBytes(inputFile);

            // Benchmark results
            var results = new List<BenchmarkResult>();

            // Test HybridCompressor
            Console.WriteLine("Testing HybridCompressor...");
            var hcResult = BenchmarkHybridCompressor(originalData, inputFile);
            results.Add(hcResult);

            // Test ZIP (Deflate - standard)
            Console.WriteLine("Testing ZIP (Deflate - Standard)...");
            var zipDeflateResult = BenchmarkZip(originalData, inputFile, CompressionType.Deflate);
            results.Add(zipDeflateResult);

            // Test ZIP (BZip2)
            Console.WriteLine("Testing ZIP (BZip2)...");
            var zipBZip2Result = BenchmarkZip(originalData, inputFile, CompressionType.BZip2);
            results.Add(zipBZip2Result);

            // Test ZIP (LZMA)
            Console.WriteLine("Testing ZIP (LZMA)...");
            var zipLZMAResult = BenchmarkZip(originalData, inputFile, CompressionType.LZMA);
            results.Add(zipLZMAResult);

            Console.WriteLine();
            DisplayComparisonTable(results);
            DisplayWinner(results);
            GenerateMarkdownReport(results, inputFile, fileInfo.Length);
        }

        public static void RunBatchComparison(string[] testFiles)
        {
            Console.WriteLine("╔════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║               BATCH COMPRESSION BENCHMARK SUITE                        ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════════════╝");
            Console.WriteLine();

            var allResults = new Dictionary<string, List<BenchmarkResult>>();

            foreach (var file in testFiles)
            {
                if (!File.Exists(file))
                {
                    Console.WriteLine($"Skipping missing file: {file}");
                    continue;
                }

                Console.WriteLine($"\n{'═',70}");
                Console.WriteLine($"Testing: {Path.GetFileName(file)}");
                Console.WriteLine($"{'═',70}\n");

                byte[] originalData = File.ReadAllBytes(file);
                var results = new List<BenchmarkResult>
                {
                    BenchmarkHybridCompressor(originalData, file),
                    BenchmarkZip(originalData, file, CompressionType.Deflate),
                    BenchmarkZip(originalData, file, CompressionType.BZip2),
                    BenchmarkZip(originalData, file, CompressionType.LZMA)
                };

                allResults[file] = results;
                DisplayComparisonTable(results);
            }

            Console.WriteLine("\n\n╔════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                        OVERALL SUMMARY                                 ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════════════╝\n");

            DisplayOverallSummary(allResults);
            GenerateBatchMarkdownReport(allResults);
        }

        private static BenchmarkResult BenchmarkHybridCompressor(byte[] originalData, string inputFile)
        {
            var result = new BenchmarkResult
            {
                CompressorName = "HybridCompressor",
                OriginalSize = originalData.Length
            };

            try
            {
                var compressor = new HybridCompressor();
                byte[] compressed = null;
                byte[] decompressed = null;

                // Warmup
                for (int i = 0; i < WARMUP_RUNS; i++)
                {
                    compressed = compressor.CompressData(originalData);
                    compressor.Reset();
                }

                // Compression benchmark
                var compressTimes = new List<double>();
                for (int i = 0; i < BENCHMARK_RUNS; i++)
                {
                    compressor.Reset();
                    var sw = Stopwatch.StartNew();
                    compressed = compressor.CompressData(originalData);
                    sw.Stop();
                    compressTimes.Add(sw.Elapsed.TotalMilliseconds);
                }

                result.CompressedSize = compressed.Length;
                result.CompressionTimeMs = compressTimes.Average();

                // Decompression benchmark
                var decompressTimes = new List<double>();
                for (int i = 0; i < BENCHMARK_RUNS; i++)
                {
                    compressor.Reset();
                    var sw = Stopwatch.StartNew();
                    decompressed = compressor.DecompressData(compressed);
                    sw.Stop();
                    decompressTimes.Add(sw.Elapsed.TotalMilliseconds);
                }

                result.DecompressionTimeMs = decompressTimes.Average();

                // Calculate metrics
                result.CompressionRatio = (double)originalData.Length / compressed.Length;
                result.CompressionSpeedMBps = (originalData.Length / (1024.0 * 1024.0)) / (result.CompressionTimeMs / 1000.0);
                result.DecompressionSpeedMBps = (originalData.Length / (1024.0 * 1024.0)) / (result.DecompressionTimeMs / 1000.0);
                result.SpaceSavedPercent = ((double)(originalData.Length - compressed.Length) / originalData.Length) * 100;

                // Verify
                result.VerificationPassed = VerifyData(originalData, decompressed);
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                result.VerificationPassed = false;
            }

            return result;
        }

        private static BenchmarkResult BenchmarkZip(byte[] originalData, string inputFile, CompressionType compressionType)
        {
            var compressorName = $"ZIP ({compressionType})";
            var result = new BenchmarkResult
            {
                CompressorName = compressorName,
                OriginalSize = originalData.Length
            };

            try
            {
                byte[] compressed = null;
                byte[] decompressed = null;

                // Warmup
                for (int i = 0; i < WARMUP_RUNS; i++)
                {
                    compressed = CompressWithZip(originalData, Path.GetFileName(inputFile), compressionType);
                }

                // Compression benchmark
                var compressTimes = new List<double>();
                for (int i = 0; i < BENCHMARK_RUNS; i++)
                {
                    var sw = Stopwatch.StartNew();
                    compressed = CompressWithZip(originalData, Path.GetFileName(inputFile), compressionType);
                    sw.Stop();
                    compressTimes.Add(sw.Elapsed.TotalMilliseconds);
                }

                result.CompressedSize = compressed.Length;
                result.CompressionTimeMs = compressTimes.Average();

                // Decompression benchmark
                var decompressTimes = new List<double>();
                for (int i = 0; i < BENCHMARK_RUNS; i++)
                {
                    var sw = Stopwatch.StartNew();
                    decompressed = DecompressZip(compressed);
                    sw.Stop();
                    decompressTimes.Add(sw.Elapsed.TotalMilliseconds);
                }

                result.DecompressionTimeMs = decompressTimes.Average();

                // Calculate metrics
                result.CompressionRatio = (double)originalData.Length / compressed.Length;
                result.CompressionSpeedMBps = (originalData.Length / (1024.0 * 1024.0)) / (result.CompressionTimeMs / 1000.0);
                result.DecompressionSpeedMBps = (originalData.Length / (1024.0 * 1024.0)) / (result.DecompressionTimeMs / 1000.0);
                result.SpaceSavedPercent = ((double)(originalData.Length - compressed.Length) / originalData.Length) * 100;

                // Verify
                result.VerificationPassed = VerifyData(originalData, decompressed);
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                result.VerificationPassed = false;
            }

            return result;
        }

        private static byte[] CompressWithZip(byte[] data, string entryName, CompressionType compressionType)
        {
            using (var outputStream = new MemoryStream())
            {
                using (var writer = WriterFactory.Open(outputStream, ArchiveType.Zip, new WriterOptions(compressionType)
                {
                    LeaveStreamOpen = true
                }))
                {
                    using (var entryStream = new MemoryStream(data))
                    {
                        writer.Write(entryName, entryStream);
                    }
                }
                return outputStream.ToArray();
            }
        }

        private static byte[] DecompressZip(byte[] compressedData)
        {
            using (var inputStream = new MemoryStream(compressedData))
            using (var reader = ReaderFactory.Open(inputStream))
            {
                if (reader.MoveToNextEntry())
                {
                    using (var entryStream = reader.OpenEntryStream())
                    using (var outputStream = new MemoryStream())
                    {
                        entryStream.CopyTo(outputStream);
                        return outputStream.ToArray();
                    }
                }
            }
            return null;
        }

        private static bool VerifyData(byte[] original, byte[] decompressed)
        {
            if (original.Length != decompressed.Length)
                return false;

            for (int i = 0; i < original.Length; i++)
            {
                if (original[i] != decompressed[i])
                    return false;
            }
            return true;
        }

        private static void DisplayComparisonTable(List<BenchmarkResult> results)
        {
            Console.WriteLine("\n╔════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                         COMPARISON RESULTS                             ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════════════╝\n");

            // Header
            Console.WriteLine($"{"Compressor",-20} {"Size",-12} {"Ratio",-8} {"Saved",-8} {"C.Time",-10} {"D.Time",-10} {"C.Speed",-12} {"Status",-8}");
            Console.WriteLine(new string('─', 100));

            foreach (var result in results)
            {
                if (result.ErrorMessage != null)
                {
                    Console.WriteLine($"{result.CompressorName,-20} ERROR: {result.ErrorMessage}");
                    continue;
                }

                Console.WriteLine($"{result.CompressorName,-20} " +
                    $"{FormatBytes(result.CompressedSize),-12} " +
                    $"{result.CompressionRatio:F2}x{"",-4} " +
                    $"{result.SpaceSavedPercent:F1}%{"",-4} " +
                    $"{result.CompressionTimeMs:F0}ms{"",-6} " +
                    $"{result.DecompressionTimeMs:F0}ms{"",-6} " +
                    $"{result.CompressionSpeedMBps:F2} MB/s{"",-4} " +
                    $"{(result.VerificationPassed ? "✓ PASS" : "✗ FAIL"),-8}");
            }

            Console.WriteLine();
        }

        private static void DisplayWinner(List<BenchmarkResult> results)
        {
            var validResults = results.Where(r => r.VerificationPassed && r.ErrorMessage == null).ToList();
            if (!validResults.Any()) return;

            var bestRatio = validResults.OrderByDescending(r => r.CompressionRatio).First();
            var fastestCompression = validResults.OrderByDescending(r => r.CompressionSpeedMBps).First();
            var fastestDecompression = validResults.OrderByDescending(r => r.DecompressionSpeedMBps).First();
            var smallestSize = validResults.OrderBy(r => r.CompressedSize).First();

            Console.WriteLine("╔════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                              WINNERS                                   ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════════════╝\n");

            Console.WriteLine($"🏆 Best Compression Ratio:    {bestRatio.CompressorName,-20} ({bestRatio.CompressionRatio:F2}x)");
            Console.WriteLine($"🏆 Smallest File Size:        {smallestSize.CompressorName,-20} ({FormatBytes(smallestSize.CompressedSize)})");
            Console.WriteLine($"⚡ Fastest Compression:        {fastestCompression.CompressorName,-20} ({fastestCompression.CompressionSpeedMBps:F2} MB/s)");
            Console.WriteLine($"⚡ Fastest Decompression:      {fastestDecompression.CompressorName,-20} ({fastestDecompression.DecompressionSpeedMBps:F2} MB/s)");
            Console.WriteLine();
        }

        private static void DisplayOverallSummary(Dictionary<string, List<BenchmarkResult>> allResults)
        {
            var compressorNames = allResults.Values.First().Select(r => r.CompressorName).ToList();
            
            Console.WriteLine($"{"Compressor",-20} {"Avg Ratio",-12} {"Avg C.Speed",-15} {"Avg D.Speed",-15} {"Wins",-8}");
            Console.WriteLine(new string('─', 80));

            foreach (var compressor in compressorNames)
            {
                var compressorResults = allResults.Values
                    .SelectMany(list => list)
                    .Where(r => r.CompressorName == compressor && r.VerificationPassed && r.ErrorMessage == null)
                    .ToList();

                if (!compressorResults.Any()) continue;

                var avgRatio = compressorResults.Average(r => r.CompressionRatio);
                var avgCSpeed = compressorResults.Average(r => r.CompressionSpeedMBps);
                var avgDSpeed = compressorResults.Average(r => r.DecompressionSpeedMBps);

                // Count wins (best ratio per file)
                int wins = 0;
                foreach (var fileResults in allResults.Values)
                {
                    var validFileResults = fileResults.Where(r => r.VerificationPassed && r.ErrorMessage == null).ToList();
                    if (validFileResults.Any())
                    {
                        var best = validFileResults.OrderByDescending(r => r.CompressionRatio).First();
                        if (best.CompressorName == compressor)
                            wins++;
                    }
                }

                Console.WriteLine($"{compressor,-20} {avgRatio:F2}x{"",-8} {avgCSpeed:F2} MB/s{"",-7} {avgDSpeed:F2} MB/s{"",-7} {wins}/{allResults.Count}");
            }
            Console.WriteLine();
        }

        private static void GenerateMarkdownReport(List<BenchmarkResult> results, string inputFile, long fileSize)
        {
            var reportPath = "BENCHMARK_REPORT.md";
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            using (var writer = new StreamWriter(reportPath, false))
            {
                writer.WriteLine("# Compression Benchmark Report");
                writer.WriteLine();
                writer.WriteLine($"**Generated:** {timestamp}");
                writer.WriteLine($"**Test File:** {Path.GetFileName(inputFile)}");
                writer.WriteLine($"**File Size:** {FormatBytes(fileSize)}");
                writer.WriteLine();

                writer.WriteLine("## Results");
                writer.WriteLine();
                writer.WriteLine("| Compressor | Compressed Size | Ratio | Space Saved | Compression Time | Decompression Time | Compression Speed | Decompression Speed | Status |");
                writer.WriteLine("|------------|----------------|-------|-------------|------------------|-------------------|-------------------|---------------------|--------|");

                foreach (var result in results)
                {
                    if (result.ErrorMessage != null)
                    {
                        writer.WriteLine($"| {result.CompressorName} | ERROR | - | - | - | - | - | - | ✗ |");
                        continue;
                    }

                    writer.WriteLine($"| {result.CompressorName} | " +
                        $"{FormatBytes(result.CompressedSize)} | " +
                        $"{result.CompressionRatio:F2}x | " +
                        $"{result.SpaceSavedPercent:F1}% | " +
                        $"{result.CompressionTimeMs:F0} ms | " +
                        $"{result.DecompressionTimeMs:F0} ms | " +
                        $"{result.CompressionSpeedMBps:F2} MB/s | " +
                        $"{result.DecompressionSpeedMBps:F2} MB/s | " +
                        $"{(result.VerificationPassed ? "✓" : "✗")} |");
                }

                writer.WriteLine();
                writer.WriteLine("## Analysis");
                writer.WriteLine();

                var validResults = results.Where(r => r.VerificationPassed && r.ErrorMessage == null).ToList();
                if (validResults.Any())
                {
                    var bestRatio = validResults.OrderByDescending(r => r.CompressionRatio).First();
                    var fastestCompression = validResults.OrderByDescending(r => r.CompressionSpeedMBps).First();

                    writer.WriteLine($"- **Best Compression Ratio:** {bestRatio.CompressorName} ({bestRatio.CompressionRatio:F2}x)");
                    writer.WriteLine($"- **Fastest Compression:** {fastestCompression.CompressorName} ({fastestCompression.CompressionSpeedMBps:F2} MB/s)");
                    writer.WriteLine($"- **Smallest Output:** {validResults.OrderBy(r => r.CompressedSize).First().CompressorName} ({FormatBytes(validResults.Min(r => r.CompressedSize))})");
                }
            }

            Console.WriteLine($"📄 Detailed report saved to: {reportPath}");
        }

        private static void GenerateBatchMarkdownReport(Dictionary<string, List<BenchmarkResult>> allResults)
        {
            var reportPath = "BATCH_BENCHMARK_REPORT.md";
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            using (var writer = new StreamWriter(reportPath, false))
            {
                writer.WriteLine("# Batch Compression Benchmark Report");
                writer.WriteLine();
                writer.WriteLine($"**Generated:** {timestamp}");
                writer.WriteLine($"**Files Tested:** {allResults.Count}");
                writer.WriteLine();

                foreach (var kvp in allResults)
                {
                    var fileName = Path.GetFileName(kvp.Key);
                    var results = kvp.Value;

                    writer.WriteLine($"## {fileName}");
                    writer.WriteLine();
                    writer.WriteLine("| Compressor | Size | Ratio | Saved | C.Time | D.Time | C.Speed | D.Speed |");
                    writer.WriteLine("|------------|------|-------|-------|--------|--------|---------|---------|");

                    foreach (var result in results.Where(r => r.VerificationPassed && r.ErrorMessage == null))
                    {
                        writer.WriteLine($"| {result.CompressorName} | " +
                            $"{FormatBytes(result.CompressedSize)} | " +
                            $"{result.CompressionRatio:F2}x | " +
                            $"{result.SpaceSavedPercent:F1}% | " +
                            $"{result.CompressionTimeMs:F0}ms | " +
                            $"{result.DecompressionTimeMs:F0}ms | " +
                            $"{result.CompressionSpeedMBps:F2} MB/s | " +
                            $"{result.DecompressionSpeedMBps:F2} MB/s |");
                    }
                    writer.WriteLine();
                }

                writer.WriteLine("## Overall Summary");
                writer.WriteLine();
                
                var compressorNames = allResults.Values.First().Select(r => r.CompressorName).ToList();
                writer.WriteLine("| Compressor | Avg Ratio | Avg Compression Speed | Avg Decompression Speed | Wins |");
                writer.WriteLine("|------------|-----------|----------------------|------------------------|------|");

                foreach (var compressor in compressorNames)
                {
                    var compressorResults = allResults.Values
                        .SelectMany(list => list)
                        .Where(r => r.CompressorName == compressor && r.VerificationPassed && r.ErrorMessage == null)
                        .ToList();

                    if (!compressorResults.Any()) continue;

                    var avgRatio = compressorResults.Average(r => r.CompressionRatio);
                    var avgCSpeed = compressorResults.Average(r => r.CompressionSpeedMBps);
                    var avgDSpeed = compressorResults.Average(r => r.DecompressionSpeedMBps);

                    int wins = 0;
                    foreach (var fileResults in allResults.Values)
                    {
                        var validFileResults = fileResults.Where(r => r.VerificationPassed && r.ErrorMessage == null).ToList();
                        if (validFileResults.Any())
                        {
                            var best = validFileResults.OrderByDescending(r => r.CompressionRatio).First();
                            if (best.CompressorName == compressor)
                                wins++;
                        }
                    }

                    writer.WriteLine($"| {compressor} | {avgRatio:F2}x | {avgCSpeed:F2} MB/s | {avgDSpeed:F2} MB/s | {wins}/{allResults.Count} |");
                }
            }

            Console.WriteLine($"📄 Batch report saved to: {reportPath}");
        }

        private static string FormatBytes(long bytes)
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
    }
}
