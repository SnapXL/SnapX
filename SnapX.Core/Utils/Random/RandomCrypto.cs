// SPDX-License-Identifier: GPL-3.0-or-later


using System.Security.Cryptography;

namespace SnapX.Core.Utils.Random;

// https://docs.microsoft.com/en-us/archive/msdn-magazine/2007/september/net-matters-tales-from-the-cryptorandom
public static class RandomCrypto
{
    private static readonly object randomLock = new object();
    private static byte[] uint32Buffer = new byte[4];

    /// <summary>Returns a non-negative random integer.</summary>
    /// <returns>A 32-bit signed integer that is greater than or equal to 0 and less than <c>System.Int32.MaxValue.</c></returns>
    public static int Next()
    {
        lock (randomLock)
        {
            RandomNumberGenerator.Fill(uint32Buffer);
            return BitConverter.ToInt32(uint32Buffer, 0) & 0x7FFFFFFF;
        }
    }

    /// <summary>Returns a non-negative random integer that is less than or equal to <paramref name="maxValue"/>.</summary>
    /// <param name="maxValue">The inclusive upper bound of the random number returned.</param>
    /// <returns>A 32-bit signed integer that is greater than or equal to 0 and less than or equal to <paramref name="maxValue"/>.</returns>
    public static int Next(int maxValue)
    {
        if (maxValue < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxValue));
        }

        return Next(0, maxValue);
    }

    /// <summary>Returns a random integer that is within a specified range.</summary>
    /// <param name="minValue">The inclusive lower bound of the random number returned.</param>
    /// <param name="maxValue">The inclusive upper bound of the random number returned.</param>
    /// <returns>A 32-bit signed integer that is greater than or equal to <paramref name="minValue"/> and less than or equal to <paramref name="maxValue"/>.</returns>
    public static int Next(int minValue, int maxValue)
    {
        maxValue++;

        if (minValue > maxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(minValue));
        }

        if (minValue == maxValue)
        {
            return minValue;
        }

        long diff = maxValue - minValue;

        lock (randomLock)
        {
            while (true)
            {
                RandomNumberGenerator.Fill(uint32Buffer);
                var rand = BitConverter.ToUInt32(uint32Buffer, 0);

                var max = 1 + (long)uint.MaxValue;
                var remainder = max % diff;

                if (rand < max - remainder)
                {
                    return (int)(minValue + (rand % diff));
                }
            }
        }
    }

    /// <summary>Returns a random floating-point number that is greater than or equal to 0.0, and less than 1.0.</summary>
    /// <returns>A double-precision floating point number that is greater than or equal to 0.0, and less than 1.0.</returns>
    public static double NextDouble()
    {
        lock (randomLock)
        {
            RandomNumberGenerator.Fill(uint32Buffer);
            var rand = BitConverter.ToUInt32(uint32Buffer, 0);
            return rand / (1.0 + uint.MaxValue);
        }
    }

    /// <summary>Fills the elements of a specified array of bytes with random numbers.</summary>
    /// <param name="buffer">An array of bytes to contain random numbers.</param>
    public static void NextBytes(byte[] buffer)
    {
        if (buffer == null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        lock (randomLock)
        {
            RandomNumberGenerator.Fill(buffer);
        }
    }

    public static T Pick<T>(params T[] array)
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array));
        }

        if (array.Length == 0)
        {
            throw new ArgumentException(nameof(array));
        }

        return array[Next(array.Length - 1)];
    }

    public static T Pick<T>(List<T> list)
    {
        if (list == null)
        {
            throw new ArgumentNullException(nameof(list));
        }

        if (list.Count == 0)
        {
            throw new ArgumentException(nameof(list));
        }

        return list[Next(list.Count - 1)];
    }

    public static void Run(params Action[] actions)
    {
        Pick(actions)();
    }
}

