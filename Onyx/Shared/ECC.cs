using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;

namespace Onyx.Shared;

public static class ECC
{
    public static AsymmetricCipherKeyPair GenerateKeyPair()
    {
        var ecParams = ECNamedCurveTable.GetByName("secp256r1");
        var domainParams = new ECDomainParameters(ecParams.Curve, ecParams.G, ecParams.N, ecParams.H);
        
        var keyGen = new ECKeyPairGenerator();
        keyGen.Init(new ECKeyGenerationParameters(domainParams, new SecureRandom()));
        return keyGen.GenerateKeyPair();
    }

    public static (byte[] epkEncoded, byte[] sig) SignEphemeralKey(AsymmetricCipherKeyPair staticKeyPair,
        AsymmetricCipherKeyPair ephemeralKeyPair)
    {
        byte[] epkEncoded = ((ECPublicKeyParameters)ephemeralKeyPair.Public).Q.GetEncoded(compressed: false);
        ISigner signer = SignerUtilities.GetSigner("SHA256withECDSA");
        signer.Init(true, staticKeyPair.Private);
        signer.BlockUpdate(epkEncoded, 0, epkEncoded.Length);
        return (epkEncoded, signer.GenerateSignature());
    }
    
    public static bool VerifyEphemeralKeySignature(byte[] epkEncoded, byte[] sig, AsymmetricCipherKeyPair staticKeyPair)
    {
        ISigner signer = SignerUtilities.GetSigner("SHA256withECDSA");
        signer.Init(false, staticKeyPair.Public);
        signer.BlockUpdate(epkEncoded, 0, epkEncoded.Length);
        return signer.VerifySignature(sig);
    }
}