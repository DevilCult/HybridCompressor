using System;
using System.Collections.Generic;

namespace ConsoleApp1.Compression.Transformers
{
    /// <summary>
    /// Run-Length Encoding for repetitive data
    /// </summary>
    public static class RLEEncoder
    {
        public static byte[] Encode(byte[] data)
        {
            if (data.Length == 0)
                return data;

            var result = new List<byte>();
            int i = 0;

            while (i < data.Length)
            {
                byte current = data[i];
                int runLength = 1;

                // Count consecutive identical bytes
                while (i + runLength < data.Length &&
                       data[i + runLength] == current &&
                       runLength < 255)
                {
                    runLength++;
                }

                if (runLength >= 4)
                {
                    // Use RLE: marker (255) + count + byte
                    // Changed order: count comes before byte to avoid confusion with escaped 255
                    result.Add(255);
                    result.Add((byte)runLength);
                    result.Add(current);
                }
                else
                {
                    // Store literally
                    for (int j = 0; j < runLength; j++)
                    {
                        // Escape marker byte (255)
                        if (current == 255)
                        {
                            result.Add(255);
                            result.Add(0);
                            result.Add(255);
                        }
                        else
                        {
                            result.Add(current);
                        }
                    }
                }

                i += runLength;
            }

            return result.ToArray();
        }

        public static byte[] Decode(byte[] data)
        {
            if (data.Length == 0)
                return data;

            var result = new List<byte>();
            int i = 0;

            while (i < data.Length)
            {
                if (data[i] == 255)
                {
                    if (i + 2 < data.Length)
                    {
                        if (data[i + 1] == 0)
                        {
                            // Escaped 255: 255 + 0 + 255
                            result.Add(data[i + 2]);
                            i += 3;
                        }
                        else
                        {
                            // RLE sequence: 255 + count + value
                            int count = data[i + 1];
                            byte value = data[i + 2];
                            
                            for (int j = 0; j < count; j++)
                                result.Add(value);
                            
                            i += 3;
                        }
                    }
                    else
                    {
                        // Incomplete sequence at end - shouldn't happen in valid data
                        // Just add remaining bytes as-is
                        result.Add(data[i]);
                        i++;
                    }
                }
                else
                {
                    result.Add(data[i]);
                    i++;
                }
            }

            return result.ToArray();
        }
    }
}
