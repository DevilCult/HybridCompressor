using System;
using System.IO;
using System.Text;

namespace ConsoleApp1
{
    public class TestDataGenerator
    {
        public static void GenerateTestFiles(string outputDir = "benchmark_test_files")
        {
            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            Console.WriteLine("╔════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                    GENERATING TEST DATA FILES                          ║");
            Console.WriteLine("╚════════════════════════════════════════════════════════════════════════╝\n");

            // 1. Text file (highly compressible)
            GenerateTextFile(Path.Combine(outputDir, "test_text.txt"), 1024 * 1024); // 1MB
            
            // 2. Repetitive data (excellent for RLE)
            GenerateRepetitiveFile(Path.Combine(outputDir, "test_repetitive.bin"), 1024 * 1024); // 1MB
            
            // 3. Numeric data (good for delta encoding)
            GenerateNumericFile(Path.Combine(outputDir, "test_numeric.bin"), 1024 * 1024); // 1MB
            
            // 4. Random data (incompressible)
            GenerateRandomFile(Path.Combine(outputDir, "test_random.bin"), 1024 * 1024); // 1MB
            
            // 5. Mixed data (realistic scenario)
            GenerateMixedFile(Path.Combine(outputDir, "test_mixed.bin"), 2 * 1024 * 1024); // 2MB
            
            // 6. JSON-like structured data
            GenerateJsonFile(Path.Combine(outputDir, "test_data.json"), 512 * 1024); // 512KB
            
            // 7. Log file (repetitive patterns)
            GenerateLogFile(Path.Combine(outputDir, "test_log.log"), 1024 * 1024); // 1MB
            
            // 8. CSV data
            GenerateCsvFile(Path.Combine(outputDir, "test_data.csv"), 1024 * 1024); // 1MB

            // 9. Large text file
            GenerateTextFile(Path.Combine(outputDir, "test_large_text.txt"), 10 * 1024 * 1024); // 10MB

            // 10. Binary executable-like data
            GenerateBinaryFile(Path.Combine(outputDir, "test_binary.bin"), 2 * 1024 * 1024); // 2MB

            Console.WriteLine("\n✓ All test files generated successfully!");
            Console.WriteLine($"📁 Output directory: {Path.GetFullPath(outputDir)}");
        }

        private static void GenerateTextFile(string path, int targetSize)
        {
            Console.WriteLine($"Generating: {Path.GetFileName(path)} ({FormatBytes(targetSize)})");
            
            var sb = new StringBuilder();
            var random = new Random(42);
            string[] words = { "the", "quick", "brown", "fox", "jumps", "over", "lazy", "dog", 
                              "Lorem", "ipsum", "dolor", "sit", "amet", "consectetur", "adipiscing", "elit",
                              "compression", "algorithm", "data", "structure", "performance", "benchmark" };
            
            while (sb.Length < targetSize)
            {
                for (int i = 0; i < 20; i++)
                {
                    sb.Append(words[random.Next(words.Length)]);
                    sb.Append(' ');
                }
                sb.AppendLine();
            }
            
            File.WriteAllText(path, sb.ToString().Substring(0, Math.Min(targetSize, sb.Length)));
        }

        private static void GenerateRepetitiveFile(string path, int targetSize)
        {
            Console.WriteLine($"Generating: {Path.GetFileName(path)} ({FormatBytes(targetSize)})");
            
            var data = new byte[targetSize];
            var pattern = new byte[] { 0xAA, 0xBB, 0xCC, 0xDD };
            
            for (int i = 0; i < targetSize; i++)
            {
                data[i] = pattern[i % pattern.Length];
            }
            
            File.WriteAllBytes(path, data);
        }

        private static void GenerateNumericFile(string path, int targetSize)
        {
            Console.WriteLine($"Generating: {Path.GetFileName(path)} ({FormatBytes(targetSize)})");
            
            var data = new byte[targetSize];
            int value = 1000;
            
            for (int i = 0; i < targetSize / 4; i++)
            {
                var bytes = BitConverter.GetBytes(value);
                Array.Copy(bytes, 0, data, i * 4, 4);
                value += new Random(i).Next(1, 10); // Small increments
            }
            
            File.WriteAllBytes(path, data);
        }

        private static void GenerateRandomFile(string path, int targetSize)
        {
            Console.WriteLine($"Generating: {Path.GetFileName(path)} ({FormatBytes(targetSize)})");
            
            var data = new byte[targetSize];
            new Random(42).NextBytes(data);
            
            File.WriteAllBytes(path, data);
        }

        private static void GenerateMixedFile(string path, int targetSize)
        {
            Console.WriteLine($"Generating: {Path.GetFileName(path)} ({FormatBytes(targetSize)})");
            
            var data = new byte[targetSize];
            var random = new Random(42);
            int offset = 0;
            
            // Mix of different data types
            while (offset < targetSize)
            {
                int chunkSize = Math.Min(random.Next(1024, 8192), targetSize - offset);
                int dataType = random.Next(4);
                
                switch (dataType)
                {
                    case 0: // Text
                        var text = Encoding.UTF8.GetBytes("This is some text data. ");
                        for (int i = 0; i < chunkSize && offset < targetSize; i++)
                        {
                            data[offset++] = text[i % text.Length];
                        }
                        break;
                    
                    case 1: // Repetitive
                        byte pattern = (byte)random.Next(256);
                        for (int i = 0; i < chunkSize && offset < targetSize; i++)
                        {
                            data[offset++] = pattern;
                        }
                        break;
                    
                    case 2: // Numeric
                        int value = random.Next(1000, 2000);
                        for (int i = 0; i < chunkSize / 4 && offset < targetSize - 3; i++)
                        {
                            var bytes = BitConverter.GetBytes(value);
                            Array.Copy(bytes, 0, data, offset, 4);
                            offset += 4;
                            value += random.Next(1, 5);
                        }
                        break;
                    
                    case 3: // Random
                        for (int i = 0; i < chunkSize && offset < targetSize; i++)
                        {
                            data[offset++] = (byte)random.Next(256);
                        }
                        break;
                }
            }
            
            File.WriteAllBytes(path, data);
        }

        private static void GenerateJsonFile(string path, int targetSize)
        {
            Console.WriteLine($"Generating: {Path.GetFileName(path)} ({FormatBytes(targetSize)})");
            
            var sb = new StringBuilder();
            var random = new Random(42);
            
            sb.AppendLine("[");
            
            while (sb.Length < targetSize - 100)
            {
                sb.AppendLine("  {");
                sb.AppendLine($"    \"id\": {random.Next(1000, 9999)},");
                sb.AppendLine($"    \"name\": \"User{random.Next(1, 1000)}\",");
                sb.AppendLine($"    \"email\": \"user{random.Next(1, 1000)}@example.com\",");
                sb.AppendLine($"    \"age\": {random.Next(18, 80)},");
                sb.AppendLine($"    \"score\": {random.Next(0, 100)},");
                sb.AppendLine($"    \"active\": {(random.Next(2) == 0 ? "true" : "false")}");
                sb.AppendLine("  },");
            }
            
            sb.AppendLine("]");
            
            File.WriteAllText(path, sb.ToString());
        }

        private static void GenerateLogFile(string path, int targetSize)
        {
            Console.WriteLine($"Generating: {Path.GetFileName(path)} ({FormatBytes(targetSize)})");
            
            var sb = new StringBuilder();
            var random = new Random(42);
            var logLevels = new[] { "INFO", "WARN", "ERROR", "DEBUG" };
            var messages = new[] 
            { 
                "Application started successfully",
                "Processing request from client",
                "Database connection established",
                "Cache hit for key",
                "Request completed in",
                "Memory usage:",
                "Thread pool status:"
            };
            
            var timestamp = DateTime.Now;
            
            while (sb.Length < targetSize)
            {
                timestamp = timestamp.AddSeconds(random.Next(1, 60));
                var level = logLevels[random.Next(logLevels.Length)];
                var message = messages[random.Next(messages.Length)];
                
                sb.AppendLine($"[{timestamp:yyyy-MM-dd HH:mm:ss}] [{level}] {message} {random.Next(1, 1000)}");
            }
            
            File.WriteAllText(path, sb.ToString().Substring(0, Math.Min(targetSize, sb.Length)));
        }

        private static void GenerateCsvFile(string path, int targetSize)
        {
            Console.WriteLine($"Generating: {Path.GetFileName(path)} ({FormatBytes(targetSize)})");
            
            var sb = new StringBuilder();
            var random = new Random(42);
            
            sb.AppendLine("ID,Name,Email,Age,Score,Department,Salary,JoinDate");
            
            var departments = new[] { "Engineering", "Sales", "Marketing", "HR", "Finance" };
            
            while (sb.Length < targetSize)
            {
                sb.AppendLine($"{random.Next(1000, 9999)}," +
                    $"Employee{random.Next(1, 1000)}," +
                    $"emp{random.Next(1, 1000)}@company.com," +
                    $"{random.Next(22, 65)}," +
                    $"{random.Next(60, 100)}," +
                    $"{departments[random.Next(departments.Length)]}," +
                    $"{random.Next(40000, 150000)}," +
                    $"2020-{random.Next(1, 13):D2}-{random.Next(1, 29):D2}");
            }
            
            File.WriteAllText(path, sb.ToString().Substring(0, Math.Min(targetSize, sb.Length)));
        }

        private static void GenerateBinaryFile(string path, int targetSize)
        {
            Console.WriteLine($"Generating: {Path.GetFileName(path)} ({FormatBytes(targetSize)})");
            
            var data = new byte[targetSize];
            var random = new Random(42);
            
            // Simulate binary executable with some structure
            // Header section (repetitive)
            for (int i = 0; i < 1024; i++)
            {
                data[i] = (byte)(i % 256);
            }
            
            // Code section (semi-random with patterns)
            for (int i = 1024; i < targetSize / 2; i++)
            {
                if (i % 100 == 0)
                {
                    // Simulate function prologue
                    data[i] = 0x55; // push ebp
                    if (i + 1 < targetSize / 2) data[i + 1] = 0x89; // mov ebp, esp
                    if (i + 2 < targetSize / 2) data[i + 2] = 0xE5;
                }
                else
                {
                    data[i] = (byte)random.Next(256);
                }
            }
            
            // Data section (more structured)
            for (int i = targetSize / 2; i < targetSize; i++)
            {
                data[i] = (byte)((i % 16) * 16);
            }
            
            File.WriteAllBytes(path, data);
        }

        private static string FormatBytes(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;

            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }

            return $"{len:F2} {sizes[order]}";
        }

        public static string[] GetGeneratedTestFiles(string outputDir = "benchmark_test_files")
        {
            if (!Directory.Exists(outputDir))
                return new string[0];

            return Directory.GetFiles(outputDir);
        }
    }
}
