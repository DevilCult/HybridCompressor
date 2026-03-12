using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleApp1.Compression
{
    /// <summary>
    /// Asymmetric Numeral Systems (ANS) entropy coder
    /// Corrected implementation with proper state management
    /// </summary>
    public static class ANSEncoder
    {
        private const int TableSize = 256;
        private const uint StateMin = 256; // Minimum state value

        public static byte[] Encode(byte[] data)
        {
            if (data.Length == 0)
                return data;

            // For small data, ANS overhead isn't worth it - return original
            if (data.Length < 512)
            {
                var smallResult = new List<byte>();
                smallResult.Add(0xFF); // Flag for uncompressed
                smallResult.AddRange(BitConverter.GetBytes(data.Length));
                smallResult.AddRange(data);
                return smallResult.ToArray();
            }

            // Build frequency table
            var freq = new int[256];
            foreach (byte b in data)
                freq[b]++;

            // Count unique symbols for optimized storage
            int uniqueSymbols = 0;
            for (int i = 0; i < 256; i++)
                if (freq[i] > 0) uniqueSymbols++;

            // Normalize frequencies to sum to TableSize
            var normalizedFreq = NormalizeFrequencies(freq, TableSize);

            // Build cumulative frequencies
            var cumFreq = new int[256];
            int sum = 0;
            for (int i = 0; i < 256; i++)
            {
                cumFreq[i] = sum;
                sum += normalizedFreq[i];
            }

            // Encode data
            var result = new List<byte>();
            
            result.Add(0x01); // Flag for ANS compressed
            
            // Store frequency table - optimized for sparse symbols
            result.AddRange(SerializeFrequenciesOptimized(normalizedFreq, uniqueSymbols));
            
            // Store original length (4 bytes)
            result.AddRange(BitConverter.GetBytes(data.Length));

            // Encode symbols in reverse order
            uint state = StateMin;
            var outputBytes = new List<byte>();

            for (int i = data.Length - 1; i >= 0; i--)
            {
                byte symbol = data[i];
                int symbolFreq = normalizedFreq[symbol];
                
                if (symbolFreq == 0)
                    continue; // Skip symbols with zero frequency

                // Renormalize: output bytes while state is too large
                while (state >= StateMin * (uint)symbolFreq)
                {
                    outputBytes.Add((byte)(state & 0xFF));
                    state >>= 8;
                }

                // Update state
                uint stateDiv = state / (uint)symbolFreq;
                uint stateMod = state % (uint)symbolFreq;
                state = stateDiv * (uint)TableSize + (uint)cumFreq[symbol] + stateMod;
            }

            // Store final state (4 bytes)
            result.AddRange(BitConverter.GetBytes(state));

            // Store output bytes count (4 bytes)
            result.AddRange(BitConverter.GetBytes(outputBytes.Count));

            // Store output bytes (in reverse order as they were added)
            outputBytes.Reverse();
            result.AddRange(outputBytes);

            return result.ToArray();
        }

        public static byte[] Decode(byte[] data)
        {
            if (data.Length < 5)
                return Array.Empty<byte>();

            int pos = 0;
            byte flag = data[pos++];

            // Check if uncompressed
            if (flag == 0xFF)
            {
                int length = BitConverter.ToInt32(data, pos);
                pos += 4;
                byte[] uncompressed = new byte[length];
                Array.Copy(data, pos, uncompressed, 0, length);
                return uncompressed;
            }

            // ANS compressed
            if (data.Length < 264) // 256 (freq) + 4 (length) + 4 (state)
                return Array.Empty<byte>();

            // Read frequency table
            var normalizedFreq = DeserializeFrequenciesOptimized(data, ref pos);

            // Read original length
            int originalLength = BitConverter.ToInt32(data, pos);
            pos += 4;

            if (originalLength <= 0 || originalLength > 100_000_000)
                return Array.Empty<byte>();

            // Read final state
            uint state = BitConverter.ToUInt32(data, pos);
            pos += 4;

            // Read output bytes count
            if (pos + 4 > data.Length)
                return Array.Empty<byte>();
            
            int outputBytesCount = BitConverter.ToInt32(data, pos);
            pos += 4;

            if (outputBytesCount < 0 || pos + outputBytesCount > data.Length)
                return Array.Empty<byte>();

            // Read output bytes
            var outputBytes = new byte[outputBytesCount];
            Array.Copy(data, pos, outputBytes, 0, outputBytesCount);
            int bytePos = 0;

            // Build cumulative frequencies
            var cumFreq = new int[256];
            int sum = 0;
            for (int i = 0; i < 256; i++)
            {
                cumFreq[i] = sum;
                sum += normalizedFreq[i];
            }

            // Build reverse lookup table for faster decoding
            var slotToSymbol = new byte[TableSize];
            for (int s = 0; s < 256; s++)
            {
                for (int i = 0; i < normalizedFreq[s]; i++)
                {
                    slotToSymbol[cumFreq[s] + i] = (byte)s;
                }
            }

            // Decode symbols
            // Since we encoded in reverse order, decoding forward gives us the symbols in reverse
            // So we need to collect them and reverse at the end
            var result = new List<byte>();

            for (int i = 0; i < originalLength; i++)
            {
                // Find symbol from state
                int slot = (int)(state % TableSize);
                byte symbol = slotToSymbol[slot];
                
                result.Add(symbol);

                // Update state
                int symbolFreq = normalizedFreq[symbol];
                if (symbolFreq > 0)
                {
                    uint stateDiv = state / (uint)TableSize;
                    uint stateMod = state % (uint)TableSize;
                    state = (uint)symbolFreq * stateDiv + stateMod - (uint)cumFreq[symbol];

                    // Renormalize: read bytes while state is too small
                    while (state < StateMin && bytePos < outputBytes.Length)
                    {
                        state = (state << 8) | outputBytes[bytePos];
                        bytePos++;
                    }
                }
            }

            // Don't reverse - the symbols are already coming out correctly
            return result.ToArray();
        }

        private static int[] NormalizeFrequencies(int[] freq, int targetSum)
        {
            var result = new int[256];
            int totalFreq = freq.Sum();
            
            if (totalFreq == 0)
                return result;

            // First pass: assign proportional frequencies
            int assigned = 0;
            var symbolsWithFreq = new List<int>();
            
            for (int i = 0; i < 256; i++)
            {
                if (freq[i] > 0)
                {
                    symbolsWithFreq.Add(i);
                    result[i] = Math.Max(1, (freq[i] * targetSum) / totalFreq);
                    assigned += result[i];
                }
            }

            // Second pass: adjust to match target sum exactly
            int diff = targetSum - assigned;
            
            // Sort symbols by their fractional parts to distribute remainder fairly
            var fractionalParts = symbolsWithFreq
                .Select(i => new { Symbol = i, Frac = ((double)freq[i] * targetSum / totalFreq) - result[i] })
                .OrderByDescending(x => x.Frac)
                .ToList();

            int idx = 0;
            while (diff != 0 && idx < fractionalParts.Count)
            {
                int symbol = fractionalParts[idx].Symbol;
                
                if (diff > 0)
                {
                    result[symbol]++;
                    diff--;
                }
                else if (diff < 0 && result[symbol] > 1)
                {
                    result[symbol]--;
                    diff++;
                }
                
                idx++;
            }

            return result;
        }

        private static byte[] SerializeFrequencies(int[] freq)
        {
            var result = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                result[i] = (byte)Math.Min(freq[i], 255);
            }
            return result;
        }

        private static byte[] SerializeFrequenciesOptimized(int[] freq, int uniqueSymbols)
        {
            // For now, use standard serialization
            // Future optimization: only store non-zero frequencies with symbol indices
            return SerializeFrequencies(freq);
        }

        private static int[] DeserializeFrequencies(byte[] data, ref int pos)
        {
            var result = new int[256];
            for (int i = 0; i < 256 && pos < data.Length; i++)
            {
                result[i] = data[pos++];
            }
            return result;
        }

        private static int[] DeserializeFrequenciesOptimized(byte[] data, ref int pos)
        {
            // For now, use standard deserialization
            return DeserializeFrequencies(data, ref pos);
        }
    }
}
