using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleApp1.Compression
{
    /// <summary>
    /// Dictionary training for improved compression of similar files
    /// Extracts common patterns across multiple samples
    /// </summary>
    public static class DictionaryTrainer
    {
        /// <summary>
        /// Train a dictionary from multiple sample files
        /// </summary>
        /// <param name="samples">List of sample data blocks</param>
        /// <param name="dictionarySize">Target dictionary size in bytes (default 64KB)</param>
        /// <returns>Trained dictionary</returns>
        public static byte[] TrainDictionary(List<byte[]> samples, int dictionarySize = 64 * 1024)
        {
            if (samples == null || samples.Count == 0)
                return Array.Empty<byte>();

            // Collect frequent patterns across all samples
            var patternFrequency = new Dictionary<string, int>();
            
            foreach (var sample in samples)
            {
                if (sample == null || sample.Length < 4)
                    continue;

                // Extract patterns of various lengths (4, 8, 16, 32, 64 bytes)
                for (int len = 4; len <= 64; len *= 2)
                {
                    // Sample every len/2 bytes to avoid too much overlap
                    for (int i = 0; i <= sample.Length - len; i += len / 2)
                    {
                        var pattern = Convert.ToBase64String(sample, i, Math.Min(len, sample.Length - i));
                        patternFrequency[pattern] = patternFrequency.GetValueOrDefault(pattern, 0) + 1;
                    }
                }
            }
            
            // Select most valuable patterns (frequency × length)
            var topPatterns = patternFrequency
                .OrderByDescending(kvp => kvp.Value * kvp.Key.Length)  // Score by frequency × length
                .Take(1000)  // Top 1000 patterns
                .Select(kvp => Convert.FromBase64String(kvp.Key))
                .ToList();
            
            // Build dictionary by concatenating top patterns
            var dictionary = new List<byte>();
            foreach (var pattern in topPatterns)
            {
                if (dictionary.Count + pattern.Length <= dictionarySize)
                {
                    dictionary.AddRange(pattern);
                }
                else
                {
                    break;  // Dictionary is full
                }
            }
            
            return dictionary.ToArray();
        }

        /// <summary>
        /// Train dictionary from file paths
        /// </summary>
        public static byte[] TrainDictionaryFromFiles(List<string> filePaths, int maxSamplesPerFile = 10, int dictionarySize = 64 * 1024)
        {
            var samples = new List<byte[]>();
            
            foreach (var filePath in filePaths)
            {
                try
                {
                    var fileData = System.IO.File.ReadAllBytes(filePath);
                    
                    // Sample chunks from the file
                    int chunkSize = Math.Max(4096, fileData.Length / maxSamplesPerFile);
                    for (int i = 0; i < fileData.Length && samples.Count < filePaths.Count * maxSamplesPerFile; i += chunkSize)
                    {
                        int size = Math.Min(chunkSize, fileData.Length - i);
                        var chunk = new byte[size];
                        Array.Copy(fileData, i, chunk, 0, size);
                        samples.Add(chunk);
                    }
                }
                catch
                {
                    // Skip files that can't be read
                    continue;
                }
            }
            
            return TrainDictionary(samples, dictionarySize);
        }
    }
}
