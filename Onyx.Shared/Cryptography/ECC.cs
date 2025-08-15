using System.Security.Cryptography;
using Org.BouncyCastle.Asn1.X9;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Security;
using static Onyx.Shared.Cryptography.Hashing;

namespace Onyx.Shared.Cryptography;

public static class ECC
{
    public static AsymmetricCipherKeyPair GenerateKeyPair(X9ECParameters? ecParams = null, ECDomainParameters? domainParameters = null)
    {
        ecParams = ecParams ?? ECNamedCurveTable.GetByName("secp256r1");
        domainParameters = domainParameters ?? new ECDomainParameters(ecParams.Curve, ecParams.G, ecParams.N, ecParams.H);
        
        var keyGen = new ECKeyPairGenerator();
        keyGen.Init(new ECKeyGenerationParameters(domainParameters, new SecureRandom()));
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

    public static byte[] ECDHSharedSecret(byte[] epkEncoded, AsymmetricCipherKeyPair ephemeralKeyPair, X9ECParameters? ecParams = null, 
        ECDomainParameters? domainParameters = null)
    {
        ecParams ??= ECNamedCurveTable.GetByName("secp256r1");
        domainParameters ??= new ECDomainParameters(ecParams.Curve, ecParams.G, ecParams.N, ecParams.H);
        
        var theirEphemeralQ = ecParams.Curve.DecodePoint(epkEncoded);
        var theirEphemeralKey = new ECPublicKeyParameters(theirEphemeralQ, domainParameters);
        IBasicAgreement agreement = new ECDHBasicAgreement();
        agreement.Init(ephemeralKeyPair.Private);
        BigInteger sharedSecret = agreement.CalculateAgreement(theirEphemeralKey);

        return Sha256(sharedSecret.ToByteArrayUnsigned());
    }
}