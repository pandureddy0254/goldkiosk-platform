using System.Text;

namespace GoldKiosk.Kiosk.UI.Services;

/// <summary>
/// A small, dependency-free QR code encoder implementing ISO/IEC 18004 byte mode with
/// error-correction level M, versions 1–10 (ample for receipt URLs). Produces the module
/// matrix; rendering is the caller's concern (the <c>QrCode</c> component draws SVG).
/// </summary>
public static class QrCodeGenerator
{
    private const int MinVersion = 1;
    private const int MaxVersion = 10;
    private const int PenaltyN1 = 3;
    private const int PenaltyN2 = 3;
    private const int PenaltyN3 = 40;
    private const int PenaltyN4 = 10;

    // Error-correction level M: number of blocks and EC codewords per block, versions 1–10.
    private static readonly int[] _ecBlocksM = [1, 1, 1, 2, 2, 4, 4, 4, 5, 5];
    private static readonly int[] _ecCodewordsPerBlockM = [10, 16, 26, 18, 24, 16, 18, 22, 22, 26];

    /// <summary>
    /// Attempts to encode text as a QR symbol. Fails only when the UTF-8 payload exceeds
    /// the version-10/M capacity (213 bytes).
    /// </summary>
    /// <param name="text">The text to encode (receipt URLs are ASCII).</param>
    /// <param name="modules">The square module matrix, <c>true</c> = dark, row-major.</param>
    /// <returns><see langword="true"/> when encoding succeeded.</returns>
    public static bool TryEncode(string text, out bool[][] modules)
    {
        ArgumentNullException.ThrowIfNull(text);
        modules = [];

        byte[] payload = Encoding.UTF8.GetBytes(text);
        int version = ChooseVersion(payload.Length);
        if (version < 0)
        {
            return false;
        }

        byte[] codewords = BuildCodewords(payload, version);
        byte[] interleaved = AddEccAndInterleave(codewords, version);
        modules = BuildMatrix(interleaved, version);
        return true;
    }

    private static int ChooseVersion(int byteCount)
    {
        for (int version = MinVersion; version <= MaxVersion; version++)
        {
            int dataCapacityBits = TotalDataCodewords(version) * 8;
            int neededBits = 4 + CharCountBits(version) + (byteCount * 8);
            if (neededBits <= dataCapacityBits)
            {
                return version;
            }
        }

        return -1;
    }

    private static int CharCountBits(int version) => version <= 9 ? 8 : 16;

    private static int TotalRawCodewords(int version)
    {
        int bits = ((16 * version) + 128) * version + 64;
        if (version >= 2)
        {
            int numAlign = (version / 7) + 2;
            bits -= ((25 * numAlign) - 10) * numAlign - 55;
            if (version >= 7)
            {
                bits -= 36;
            }
        }

        return bits / 8;
    }

    private static int TotalDataCodewords(int version) =>
        TotalRawCodewords(version) - (_ecBlocksM[version - 1] * _ecCodewordsPerBlockM[version - 1]);

    private static byte[] BuildCodewords(byte[] payload, int version)
    {
        List<bool> bits = [];
        AppendBits(bits, 0b0100, 4);
        AppendBits(bits, payload.Length, CharCountBits(version));
        foreach (byte value in payload)
        {
            AppendBits(bits, value, 8);
        }

        int capacityBits = TotalDataCodewords(version) * 8;
        int terminator = Math.Min(4, capacityBits - bits.Count);
        AppendBits(bits, 0, terminator);
        AppendBits(bits, 0, (8 - (bits.Count % 8)) % 8);

        for (byte pad = 0xEC; bits.Count < capacityBits; pad ^= 0b11111101)
        {
            AppendBits(bits, pad, 8);
        }

        byte[] codewords = new byte[bits.Count / 8];
        for (int i = 0; i < bits.Count; i++)
        {
            if (bits[i])
            {
                codewords[i / 8] |= (byte)(0b10000000 >> (i % 8));
            }
        }

        return codewords;
    }

    private static void AppendBits(List<bool> bits, int value, int length)
    {
        for (int i = length - 1; i >= 0; i--)
        {
            bits.Add(((value >> i) & 1) != 0);
        }
    }

    private static byte[] AddEccAndInterleave(byte[] data, int version)
    {
        int numBlocks = _ecBlocksM[version - 1];
        int blockEccLen = _ecCodewordsPerBlockM[version - 1];
        int rawCodewords = TotalRawCodewords(version);
        int numShortBlocks = numBlocks - (rawCodewords % numBlocks);
        int shortBlockLen = rawCodewords / numBlocks;

        byte[] rsDivisor = ReedSolomonComputeDivisor(blockEccLen);
        byte[][] blocks = new byte[numBlocks][];
        int k = 0;
        for (int i = 0; i < numBlocks; i++)
        {
            int datLen = shortBlockLen - blockEccLen + (i < numShortBlocks ? 0 : 1);
            byte[] dat = data[k..(k + datLen)];
            k += datLen;
            byte[] block = new byte[shortBlockLen + 1];
            dat.CopyTo(block, 0);
            byte[] ecc = ReedSolomonComputeRemainder(dat, rsDivisor);
            ecc.CopyTo(block, block.Length - blockEccLen);
            blocks[i] = block;
        }

        byte[] result = new byte[rawCodewords];
        int index = 0;
        for (int i = 0; i < blocks[0].Length; i++)
        {
            for (int j = 0; j < blocks.Length; j++)
            {
                // Skip the padding byte in short blocks.
                if (i != shortBlockLen - blockEccLen || j >= numShortBlocks)
                {
                    result[index] = blocks[j][i];
                    index++;
                }
            }
        }

        return result;
    }

    private static byte[] ReedSolomonComputeDivisor(int degree)
    {
        byte[] result = new byte[degree];
        result[degree - 1] = 1;
        int root = 1;
        for (int i = 0; i < degree; i++)
        {
            for (int j = 0; j < result.Length; j++)
            {
                result[j] = (byte)GfMultiply(result[j], root);
                if (j + 1 < result.Length)
                {
                    result[j] ^= result[j + 1];
                }
            }

            root = GfMultiply(root, 0x02);
        }

        return result;
    }

    private static byte[] ReedSolomonComputeRemainder(byte[] data, byte[] divisor)
    {
        byte[] result = new byte[divisor.Length];
        foreach (byte b in data)
        {
            int factor = b ^ result[0];
            Array.Copy(result, 1, result, 0, result.Length - 1);
            result[^1] = 0;
            for (int i = 0; i < result.Length; i++)
            {
                result[i] ^= (byte)GfMultiply(divisor[i], factor);
            }
        }

        return result;
    }

    private static int GfMultiply(int x, int y)
    {
        int z = 0;
        for (int i = 7; i >= 0; i--)
        {
            z = (z << 1) ^ ((z >> 7) * 0x11D);
            z ^= ((y >> i) & 1) * x;
        }

        return z;
    }

    private static bool[][] BuildMatrix(byte[] codewords, int version)
    {
        int size = (4 * version) + 17;
        bool[][] modules = new bool[size][];
        bool[][] isFunction = new bool[size][];
        for (int i = 0; i < size; i++)
        {
            modules[i] = new bool[size];
            isFunction[i] = new bool[size];
        }

        DrawFunctionPatterns(modules, isFunction, version);
        DrawCodewords(modules, isFunction, codewords);

        int bestMask = 0;
        int bestPenalty = int.MaxValue;
        for (int mask = 0; mask < 8; mask++)
        {
            ApplyMask(modules, isFunction, mask);
            DrawFormatBits(modules, isFunction, mask);
            int penalty = PenaltyScore(modules);
            if (penalty < bestPenalty)
            {
                bestPenalty = penalty;
                bestMask = mask;
            }

            ApplyMask(modules, isFunction, mask); // XOR undo
        }

        ApplyMask(modules, isFunction, bestMask);
        DrawFormatBits(modules, isFunction, bestMask);
        return modules;
    }

    private static void DrawFunctionPatterns(bool[][] modules, bool[][] isFunction, int version)
    {
        int size = modules.Length;

        for (int i = 0; i < size; i++)
        {
            SetFunction(modules, isFunction, 6, i, i % 2 == 0);
            SetFunction(modules, isFunction, i, 6, i % 2 == 0);
        }

        DrawFinderPattern(modules, isFunction, 3, 3);
        DrawFinderPattern(modules, isFunction, size - 4, 3);
        DrawFinderPattern(modules, isFunction, 3, size - 4);

        int[] alignPositions = AlignmentPatternPositions(version);
        int numAlign = alignPositions.Length;
        for (int i = 0; i < numAlign; i++)
        {
            for (int j = 0; j < numAlign; j++)
            {
                bool overlapsFinder = (i == 0 && j == 0)
                    || (i == 0 && j == numAlign - 1)
                    || (i == numAlign - 1 && j == 0);
                if (!overlapsFinder)
                {
                    DrawAlignmentPattern(modules, isFunction, alignPositions[i], alignPositions[j]);
                }
            }
        }

        DrawFormatBits(modules, isFunction, 0); // Reserve the format areas.
        DrawVersionInfo(modules, isFunction, version);
    }

    private static int[] AlignmentPatternPositions(int version)
    {
        if (version == 1)
        {
            return [];
        }

        int size = (4 * version) + 17;
        int numAlign = (version / 7) + 2;
        int step = ((version * 4) + (numAlign * 2) + 1) / ((numAlign * 2) - 2) * 2;
        int[] result = new int[numAlign];
        result[0] = 6;
        for (int i = result.Length - 1, pos = size - 7; i >= 1; i--, pos -= step)
        {
            result[i] = pos;
        }

        return result;
    }

    private static void DrawFinderPattern(bool[][] modules, bool[][] isFunction, int x, int y)
    {
        int size = modules.Length;
        for (int dy = -4; dy <= 4; dy++)
        {
            for (int dx = -4; dx <= 4; dx++)
            {
                int dist = Math.Max(Math.Abs(dx), Math.Abs(dy));
                int xx = x + dx;
                int yy = y + dy;
                if (xx >= 0 && xx < size && yy >= 0 && yy < size)
                {
                    SetFunction(modules, isFunction, xx, yy, dist != 2 && dist != 4);
                }
            }
        }
    }

    private static void DrawAlignmentPattern(bool[][] modules, bool[][] isFunction, int x, int y)
    {
        for (int dy = -2; dy <= 2; dy++)
        {
            for (int dx = -2; dx <= 2; dx++)
            {
                SetFunction(modules, isFunction, x + dx, y + dy, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1);
            }
        }
    }

    private static void DrawFormatBits(bool[][] modules, bool[][] isFunction, int mask)
    {
        int size = modules.Length;

        // EC level M has format indicator 0b00.
        int data = mask;
        int rem = data;
        for (int i = 0; i < 10; i++)
        {
            rem = (rem << 1) ^ ((rem >> 9) * 0x537);
        }

        int bits = ((data << 10) | rem) ^ 0x5412;

        for (int i = 0; i <= 5; i++)
        {
            SetFunction(modules, isFunction, 8, i, GetBit(bits, i));
        }

        SetFunction(modules, isFunction, 8, 7, GetBit(bits, 6));
        SetFunction(modules, isFunction, 8, 8, GetBit(bits, 7));
        SetFunction(modules, isFunction, 7, 8, GetBit(bits, 8));
        for (int i = 9; i < 15; i++)
        {
            SetFunction(modules, isFunction, 14 - i, 8, GetBit(bits, i));
        }

        for (int i = 0; i < 8; i++)
        {
            SetFunction(modules, isFunction, size - 1 - i, 8, GetBit(bits, i));
        }

        for (int i = 8; i < 15; i++)
        {
            SetFunction(modules, isFunction, 8, size - 15 + i, GetBit(bits, i));
        }

        SetFunction(modules, isFunction, 8, size - 8, true); // Dark module.
    }

    private static void DrawVersionInfo(bool[][] modules, bool[][] isFunction, int version)
    {
        if (version < 7)
        {
            return;
        }

        int size = modules.Length;
        int rem = version;
        for (int i = 0; i < 12; i++)
        {
            rem = (rem << 1) ^ ((rem >> 11) * 0x1F25);
        }

        int bits = (version << 12) | rem;
        for (int i = 0; i < 18; i++)
        {
            bool bit = GetBit(bits, i);
            int a = size - 11 + (i % 3);
            int b = i / 3;
            SetFunction(modules, isFunction, a, b, bit);
            SetFunction(modules, isFunction, b, a, bit);
        }
    }

    private static void DrawCodewords(bool[][] modules, bool[][] isFunction, byte[] codewords)
    {
        int size = modules.Length;
        int bitIndex = 0;
        int totalBits = codewords.Length * 8;

        for (int right = size - 1; right >= 1; right -= 2)
        {
            if (right == 6)
            {
                right = 5;
            }

            for (int vert = 0; vert < size; vert++)
            {
                for (int j = 0; j < 2; j++)
                {
                    int x = right - j;
                    bool upward = ((right + 1) & 2) == 0;
                    int y = upward ? size - 1 - vert : vert;
                    if (!isFunction[y][x] && bitIndex < totalBits)
                    {
                        modules[y][x] = GetBit(codewords[bitIndex >> 3], 7 - (bitIndex & 7));
                        bitIndex++;
                    }
                }
            }
        }
    }

    private static void ApplyMask(bool[][] modules, bool[][] isFunction, int mask)
    {
        int size = modules.Length;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool invert = mask switch
                {
                    0 => (x + y) % 2 == 0,
                    1 => y % 2 == 0,
                    2 => x % 3 == 0,
                    3 => (x + y) % 3 == 0,
                    4 => ((x / 3) + (y / 2)) % 2 == 0,
                    5 => (x * y % 2) + (x * y % 3) == 0,
                    6 => ((x * y % 2) + (x * y % 3)) % 2 == 0,
                    _ => (((x + y) % 2) + (x * y % 3)) % 2 == 0,
                };
                modules[y][x] ^= invert & !isFunction[y][x];
            }
        }
    }

    private static int PenaltyScore(bool[][] modules)
    {
        int size = modules.Length;
        int result = 0;

        // N1: runs of five or more same-colored modules, rows and columns.
        for (int y = 0; y < size; y++)
        {
            int runLength = 1;
            for (int x = 1; x <= size; x++)
            {
                if (x < size && modules[y][x] == modules[y][x - 1])
                {
                    runLength++;
                    continue;
                }

                if (runLength >= 5)
                {
                    result += PenaltyN1 + runLength - 5;
                }

                runLength = 1;
            }
        }

        for (int x = 0; x < size; x++)
        {
            int runLength = 1;
            for (int y = 1; y <= size; y++)
            {
                if (y < size && modules[y][x] == modules[y - 1][x])
                {
                    runLength++;
                    continue;
                }

                if (runLength >= 5)
                {
                    result += PenaltyN1 + runLength - 5;
                }

                runLength = 1;
            }
        }

        // N2: 2×2 blocks of the same color.
        for (int y = 0; y < size - 1; y++)
        {
            for (int x = 0; x < size - 1; x++)
            {
                bool color = modules[y][x];
                if (color == modules[y][x + 1] && color == modules[y + 1][x] && color == modules[y + 1][x + 1])
                {
                    result += PenaltyN2;
                }
            }
        }

        // N3: finder-like 1:1:3:1:1 patterns with four light modules on either side.
        result += CountFinderLikePatterns(modules, horizontal: true) * PenaltyN3;
        result += CountFinderLikePatterns(modules, horizontal: false) * PenaltyN3;

        // N4: dark-module balance.
        int dark = 0;
        foreach (bool[] row in modules)
        {
            foreach (bool cell in row)
            {
                if (cell)
                {
                    dark++;
                }
            }
        }

        int total = size * size;
        int k = Math.Abs((dark * 100) - (total * 50)) / (total * 5);
        result += k * PenaltyN4;
        return result;
    }

    private static int CountFinderLikePatterns(bool[][] modules, bool horizontal)
    {
        // The two 11-module windows: dark-light-dark-dark-dark-light-dark plus four
        // light modules before or after.
        ReadOnlySpan<bool> patternA = [true, false, true, true, true, false, true, false, false, false, false];
        ReadOnlySpan<bool> patternB = [false, false, false, false, true, false, true, true, true, false, true];

        int size = modules.Length;
        int count = 0;
        for (int a = 0; a < size; a++)
        {
            for (int b = 0; b + 11 <= size; b++)
            {
                bool matchesA = true;
                bool matchesB = true;
                for (int i = 0; i < 11; i++)
                {
                    bool cell = horizontal ? modules[a][b + i] : modules[b + i][a];
                    matchesA &= cell == patternA[i];
                    matchesB &= cell == patternB[i];
                }

                if (matchesA || matchesB)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static void SetFunction(bool[][] modules, bool[][] isFunction, int x, int y, bool isDark)
    {
        modules[y][x] = isDark;
        isFunction[y][x] = true;
    }

    private static bool GetBit(int value, int index) => ((value >> index) & 1) != 0;
}
