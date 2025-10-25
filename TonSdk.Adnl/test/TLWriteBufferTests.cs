using System.Numerics;
using System.Security.Cryptography;
using NUnit.Framework;
using TonSdk.Adnl.TL;

namespace TonSdk.Adnl.Tests;

public class TLWriteBufferTests
{
    [Test]
    public void Test_Int256FromRandomBytesCanBeEncoded()
    {
        // There is 1/256 chance that last byte will be 0 (biginteger is encoded as little-endian)
        // Check x100 samples
        for (int i = 0; i < 256 * 100; ++i)
        {
            byte[] randomBytes = GenerateRandomBytes(32);

            BigInteger queryId = new(randomBytes);

            EnsureCanBeWrittenAndReadBack(queryId);
        }
    }

    [Test]
    public void Test_Int256WithZeroLastByteCanBeWritten()
    {
        byte[] bytes = new byte[32];
        for (int i = 0; i < 31; ++i) bytes[i] = (byte)i;

        bytes[31] = 0;

        BigInteger queryId = new(bytes);

        EnsureCanBeWrittenAndReadBack(queryId);
    }

    static void EnsureCanBeWrittenAndReadBack(BigInteger value)
    {
        TLWriteBuffer writeBuffer = new();
        writeBuffer.WriteInt256(value);

        byte[] writtenBytes = writeBuffer.Build();
        BigInteger restoredValue = new(writtenBytes);

        Assert.That(restoredValue, Is.EqualTo(value));
    }

    // AdnlKeys generation logic
    static byte[] GenerateRandomBytes(int byteSize)
    {
        using RandomNumberGenerator randomNumberGenerator = RandomNumberGenerator.Create();
        byte[] randomBytes = new byte[byteSize];
        randomNumberGenerator.GetBytes(randomBytes);
        return randomBytes;
    }
}