using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace ProjectDashboard.Web.Services;

public class EncryptionService
{
    private readonly byte[] _key;
    public EncryptionService(IConfiguration cfg)
    {
        var b64 = cfg["Security:EncryptionKeyBase64"] 
                  ?? throw new InvalidOperationException("Missing Security:EncryptionKeyBase64");
        _key = Convert.FromBase64String(b64);
        if (_key.Length != 32) throw new InvalidOperationException("Encryption key must be 32 bytes.");
    }

    public (string cipherB64, string ivB64) Encrypt(string plaintext)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();
        using var enc = aes.CreateEncryptor();
        var plain = System.Text.Encoding.UTF8.GetBytes(plaintext);
        var cipher = enc.TransformFinalBlock(plain, 0, plain.Length);
        return (Convert.ToBase64String(cipher), Convert.ToBase64String(aes.IV));
    }

    public string Decrypt(string cipherB64, string ivB64)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.IV = Convert.FromBase64String(ivB64);
        using var dec = aes.CreateDecryptor();
        var cipher = Convert.FromBase64String(cipherB64);
        var plain = dec.TransformFinalBlock(cipher, 0, cipher.Length);
        return System.Text.Encoding.UTF8.GetString(plain);
    }
}
