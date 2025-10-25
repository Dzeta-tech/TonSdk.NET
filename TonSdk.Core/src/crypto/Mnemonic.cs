using System;
using System.Linq;

namespace TonSdk.Core.Crypto;

/// <summary>
///     Mnemonic phrase for TON wallet generation.
///     Supports both TON and BIP39 standards.
/// </summary>
public class Mnemonic
{
    const string TonDefaultSalt = "TON default seed";
    const int TonRounds = 100000;
    const int Bip39Rounds = 2048;
    const int KeyLength = 32;

    Mnemonic(string[] words, KeyPair keyPair, byte[] seed)
    {
        Words = words;
        KeyPair = keyPair;
        Seed = seed;
    }

    public string[] Words { get; }
    public KeyPair KeyPair { get; }
    public byte[] Seed { get; }

    /// <summary>
    ///     Creates a random mnemonic with TON standard (default).
    /// </summary>
    public static Mnemonic CreateRandom(MnemonicType type = MnemonicType.Ton)
    {
        string[] words = Utils.GenerateWords();
        return FromWords(words, type);
    }

    /// <summary>
    ///     Creates mnemonic from existing words.
    /// </summary>
    public static Mnemonic FromWords(string[] words, MnemonicType type = MnemonicType.Ton, string? password = null)
    {
        if (words == null || words.Length != 24)
            throw new ArgumentException("Mnemonic must contain exactly 24 words", nameof(words));

        if (!words.All(word => MnemonicWords.Bip0039En.Contains(word)))
            throw new ArgumentException("Invalid mnemonic words", nameof(words));

        byte[] seed;
        switch (type)
        {
            case MnemonicType.Ton:
                seed = Utils.GenerateSeed(words, GenerateSalt(TonDefaultSalt), TonRounds, KeyLength);
                break;

            case MnemonicType.Bip39:
                string salt = password != null ? $"mnemonic{password}" : "";
                seed = Utils.GenerateSeedBip39(words, GenerateSalt(salt), Bip39Rounds, 64).Take(32).ToArray();
                break;

            default:
                throw new ArgumentException("Invalid mnemonic type", nameof(type));
        }

        KeyPair keyPair = Utils.GenerateKeyPair(seed);
        return new Mnemonic(words, keyPair, seed);
    }

    /// <summary>
    ///     Validates mnemonic words.
    /// </summary>
    public static bool IsValid(string[] words)
    {
        if (words == null || words.Length != 24)
            return false;

        return words.All(word => MnemonicWords.Bip0039En.Contains(word));
    }

    static byte[] GenerateSalt(string salt)
    {
        return System.Text.Encoding.UTF8.GetBytes(salt);
    }
}

public enum MnemonicType
{
    Ton,
    Bip39
}

