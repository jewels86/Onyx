using Org.BouncyCastle.Crypto.Digests;

namespace Onyx.Shared.Cryptography;

public static class Hashing
{
    public static byte[] Sha256(byte[] data)
    {
        var sha256 = new Sha256Digest();
        sha256.BlockUpdate(data, 0, data.Length);
        byte[] hash = new byte[sha256.GetDigestSize()];
        sha256.DoFinal(hash, 0);
        return hash;
    }
}