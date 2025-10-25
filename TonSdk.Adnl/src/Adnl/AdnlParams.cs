using System.Linq;
using System.Security.Cryptography;

namespace TonSdk.Adnl;

internal class AdnlAesParams
{
    internal byte[] Bytes { get; } = AdnlKeys.GenerateRandomBytes(160);

    internal byte[] RxKey => Bytes.Take(32).ToArray();

    internal byte[] TxKey => Bytes.Skip(32).Take(32).ToArray();

    internal byte[] RxNonce => Bytes.Skip(64).Take(16).ToArray();

    internal byte[] TxNonce => Bytes.Skip(80).Take(16).ToArray();

    internal byte[] Padding => Bytes.Skip(96).Take(64).ToArray();

    internal byte[] Hash
    {
        get
        {
            using SHA256 sha256 = SHA256.Create();
            byte[] sha256Hash = sha256.ComputeHash(Bytes);
            return sha256Hash;
        }
    }
}