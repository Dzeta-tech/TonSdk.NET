using System;
using System.Linq;

namespace TonSdk.Core.Crypto
{
    public class MnemonicOptions
    {
        public MnemonicOptions(string salt, int rounds, int keyLength)
        {
            Salt = salt;
            Rounds = rounds;
            KeyLength = keyLength;
        }

        public string Salt { get; set; }
        public int Rounds { get; set; }
        public int KeyLength { get; set; }
    }

    public class MnemonicBIP39
    {
        /// <summary>
        ///     Initializes a new instance of the MnemonicBIP39 class.
        /// </summary>
        /// <param name="mnemonic">An optional array of mnemonic words.</param>
        /// <param name="options">An optional MnemonicOptions object specifying custom options.</param>
        /// <exception cref="Exception">Thrown when the provided mnemonic is invalid.</exception>
        public MnemonicBIP39(string[]? mnemonic = null, MnemonicOptions? options = null)
        {
            if (mnemonic != null && mnemonic.Length != 24)
                throw new Exception("Mnemonic: must contain 24 bip39 words.");
            if (mnemonic != null && !mnemonic.All(word => MnemonicWords.Bip0039En.Contains(word)))
                throw new Exception("Mnemonic: invalid mnemonic phrase words.");

            // According to BIP39 by default
            string salt = "";
            int rounds = 2048;
            int keyLength = 64;

            if (options != null)
            {
                salt = options?.Salt != null ? options.Salt : "";
                rounds = options?.Rounds != null ? options.Rounds : 2048;
                keyLength = options?.KeyLength != null ? options.KeyLength : 64;
            }

            string[] words = mnemonic != null ? mnemonic : Utils.GenerateWords();
            byte[] seed = Utils.GenerateSeedBIP39(words, GenerateSalt(salt), rounds, keyLength).Take(32).ToArray();
            KeyPair keys = Utils.GenerateKeyPair(seed);

            _words = words;
            _seed = seed;
            _keys = keys;
        }

        string[]? _words { get; }
        byte[]? _seed { get; }
        KeyPair? _keys { get; }

        public string[]? Words => _words;

        public byte[]? Seed => _seed;

        public KeyPair? Keys => _keys;

        /// <summary>
        ///     Generates an array of random mnemonic words.
        /// </summary>
        /// <returns>An array of mnemonic words.</returns>
        public static string[] GenerateWords()
        {
            return Utils.GenerateWords();
        }

        /// <summary>
        ///     Generates a key pair from the provided seed.
        /// </summary>
        /// <param name="seed">The seed used for key pair generation.</param>
        /// <returns>A KeyPair object containing the public and private keys.</returns>
        public static KeyPair GenerateKeyPair(byte[] seed)
        {
            return Utils.GenerateKeyPair(seed);
        }

        /// <summary>
        ///     Generates a seed byte array from the provided mnemonic words.
        /// </summary>
        /// <param name="mnemonic">The mnemonic words.</param>
        /// <returns>The generated seed byte array.</returns>
        public static byte[] GenerateSeed(string[] mnemonic, string? salt = null, int rounds = 2048,
            int keyLength = 64)
        {
            if (mnemonic != null && mnemonic.Length != 24)
                throw new Exception("Mnemonic: must contain 24 bip39 words.");
            if (mnemonic != null && !mnemonic.All(word => MnemonicWords.Bip0039En.Contains(word)))
                throw new Exception("Mnemonic: invalid mnemonic phrase words.");

            string s = "mnemonic" + (salt != null ? Utils.Normalize(salt) : "");
            byte[] seed = Utils.GenerateSeedBIP39(mnemonic, s, rounds, keyLength).Take(32).ToArray();
            return seed;
        }

        protected string GenerateSalt(string? salt = null)
        {
            return "mnemonic" + (salt != null ? Utils.Normalize(salt) : "");
        }
    }
}