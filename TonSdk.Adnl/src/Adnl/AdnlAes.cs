using System;
using System.Security.Cryptography;

namespace TonSdk.Adnl;

public class AesCounter
{
    public AesCounter(byte[] initialValue)
    {
        if (initialValue.Length != 16)
            throw new ArgumentException("Invalid counter bytes size (must be 16 bytes)");
        Counter = initialValue;
    }

    public AesCounter(int initialValue)
    {
        Counter = new byte[16];
        for (int i = 15; i >= 0; i--)
        {
            Counter[i] = (byte)(initialValue % 256);
            initialValue /= 256;
        }
    }

    public byte[] Counter { get; }

    public void Increment()
    {
        for (int i = 15; i >= 0; i--)
            if (Counter[i] == 255)
            {
                Counter[i] = 0;
            }
            else
            {
                Counter[i]++;
                break;
            }
    }
}

public class AesCtrMode
{
    readonly Aes aes;
    readonly AesCounter counter;
    readonly object @lock = new(); // Lock for thread-safe encryption
    byte[] remainingCounter;
    int remainingCounterIndex;

    public AesCtrMode(byte[] key, AesCounter? counter)
    {
        this.counter = counter ?? new AesCounter(1);
        remainingCounter = new byte[16];
        remainingCounterIndex = 16;

        aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.ECB;
        aes.Padding = PaddingMode.None;
    }

    public byte[] Encrypt(byte[] plaintext)
    {
        // Thread-safe encryption: lock to prevent concurrent modification of counter state
        lock (@lock)
        {
            byte[] encrypted = new byte[plaintext.Length];

            for (int i = 0; i < encrypted.Length; i++)
            {
                if (remainingCounterIndex == 16)
                {
                    remainingCounter = EncryptCounter(counter.Counter);
                    remainingCounterIndex = 0;
                    counter.Increment();
                }

                encrypted[i] = (byte)(plaintext[i] ^ remainingCounter[remainingCounterIndex++]);
            }

            return encrypted;
        }
    }

    byte[] EncryptCounter(byte[] counter)
    {
        using ICryptoTransform encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(counter, 0, counter.Length);
    }

    public byte[] Decrypt(byte[] ciphertext)
    {
        return Encrypt(ciphertext); // Delegates to Encrypt, which is now thread-safe
    }
}