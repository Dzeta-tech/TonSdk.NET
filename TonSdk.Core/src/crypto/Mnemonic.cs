using System;
using System.Linq;

namespace TonSdk.Core.Crypto
{
    public class Mnemonic
    {
        static readonly string TON_SALT = "TON default seed";
        static readonly int TON_ROUNDS = 100000;
        static readonly int TON_KEY_LENGTH = 32;

        /// <summary>
        ///     Initializes a new instance of the Mnemonic class (TON).
        /// </summary>
        /// <param name="mnemonic">An optional array of mnemonic words.</param>
        /// <exception cref="Exception">Thrown when the provided mnemonic is invalid.</exception>
        public Mnemonic(string[]? mnemonic = null)
        {
            if (mnemonic != null && mnemonic.Length != 24)
                throw new Exception("Mnemonic: must contain 24 bip39 words.");
            if (mnemonic != null && !mnemonic.All(word => MnemonicWords.Bip0039En.Contains(word)))
                throw new Exception("Mnemonic: invalid mnemonic phrase words.");

            string[] words = mnemonic != null ? mnemonic : Utils.GenerateWords();
            byte[] seed = Utils.GenerateSeed(words, GenerateSalt(TON_SALT), TON_ROUNDS, TON_KEY_LENGTH);
            KeyPair keys = Utils.GenerateKeyPair(seed);

            _words = words;
            _seed = seed;
            _keys = keys;
        }

        string[] _words { get; }
        byte[] _seed { get; }
        KeyPair _keys { get; }

        public string[] Words => _words;

        public byte[] Seed => _seed;

        public KeyPair Keys => _keys;

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
        public static byte[] GenerateSeed(string[] mnemonic)
        {
            if (mnemonic != null && mnemonic.Length != 24)
                throw new Exception("Mnemonic: must contain 24 bip39 words.");
            if (mnemonic != null && !mnemonic.All(word => MnemonicWords.Bip0039En.Contains(word)))
                throw new Exception("Mnemonic: invalid mnemonic phrase words.");

            byte[] seed = Utils.GenerateSeed(mnemonic, Utils.Normalize(TON_SALT), TON_ROUNDS, TON_KEY_LENGTH);
            return seed;
        }

        protected string GenerateSalt(string? salt = null)
        {
            return Utils.Normalize(salt);
        }
    }
}