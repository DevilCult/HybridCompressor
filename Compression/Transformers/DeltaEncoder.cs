using System;

namespace ConsoleApp1.Compression.Transformers
{
    /// <summary>
    /// Delta encoding for numeric data
    /// </summary>
    public static class DeltaEncoder
    {
        public static byte[] Encode(byte[] data)
        {
            if (data.Length < 4)
                return data;

            byte[] result = new byte[data.Length];
            
            // Copy first value as-is
            Array.Copy(data, 0, result, 0, 4);

            // Store deltas for 32-bit integers
            int i;
            for (i = 4; i + 4 <= data.Length; i += 4)
            {
                int current = BitConverter.ToInt32(data, i);
                int previous = BitConverter.ToInt32(data, i - 4);
                int delta = current - previous;
                
                byte[] deltaBytes = BitConverter.GetBytes(delta);
                Array.Copy(deltaBytes, 0, result, i, 4);
            }
            
            // Handle remaining bytes (if data length is not divisible by 4)
            if (i < data.Length)
            {
                Array.Copy(data, i, result, i, data.Length - i);
            }

            return result;
        }

        public static byte[] Decode(byte[] data)
        {
            if (data.Length < 4)
                return data;

            byte[] result = new byte[data.Length];
            
            // Copy first value as-is
            Array.Copy(data, 0, result, 0, 4);

            // Reconstruct from deltas
            int i;
            for (i = 4; i + 4 <= data.Length; i += 4)
            {
                int previous = BitConverter.ToInt32(result, i - 4);
                int delta = BitConverter.ToInt32(data, i);
                int current = previous + delta;
                
                byte[] currentBytes = BitConverter.GetBytes(current);
                Array.Copy(currentBytes, 0, result, i, 4);
            }
            
            // Handle remaining bytes (if data length is not divisible by 4)
            if (i < data.Length)
            {
                Array.Copy(data, i, result, i, data.Length - i);
            }

            return result;
        }
    }
}
