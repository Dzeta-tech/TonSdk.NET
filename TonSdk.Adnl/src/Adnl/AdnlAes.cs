using System;
using System.Security.Cryptography;

namespace TonSdk.Adnl
{
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
        readonly Aes _aes;
        readonly AesCounter _counter;
        readonly object _lock = new object(); // Lock for thread-safe encryption
        byte[] _remainingCounter;
        int _remainingCounterIndex;

        public AesCtrMode(byte[] key, AesCounter? counter)
        {
            _counter = counter ?? new AesCounter(1);
            _remainingCounter = new byte[16];
            _remainingCounterIndex = 16;

            _aes = Aes.Create();
            _aes.Key = key;
            _aes.Mode = CipherMode.ECB;
            _aes.Padding = PaddingMode.None;
        }

        public byte[] Encrypt(byte[] plaintext)
        {
            // Thread-safe encryption: lock to prevent concurrent modification of counter state
            lock (_lock)
            {
                byte[] encrypted = new byte[plaintext.Length];

                for (int i = 0; i < encrypted.Length; i++)
                {
                    if (_remainingCounterIndex == 16)
                    {
                        _remainingCounter = EncryptCounter(_counter.Counter);
                        _remainingCounterIndex = 0;
                        _counter.Increment();
                    }

                    encrypted[i] = (byte)(plaintext[i] ^ _remainingCounter[_remainingCounterIndex++]);
                }

                return encrypted;
            }
        }

        byte[] EncryptCounter(byte[] counter)
        {
            using ICryptoTransform encryptor = _aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(counter, 0, counter.Length);
        }

        public byte[] Decrypt(byte[] ciphertext)
        {
            return Encrypt(ciphertext); // Delegates to Encrypt, which is now thread-safe
        }
    }
}