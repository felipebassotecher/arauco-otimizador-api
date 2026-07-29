using Arauco.Otimizador.WebApi.Flow.Exceptions;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Arauco.Otimizador.WebApi.Flow.Util;

public class CryptoService
{
    private readonly string privatePem;
    private readonly string passphrase;

    public CryptoService(IConfiguration config, IWebHostEnvironment env)
    {
        passphrase = config["Certificate:PassPhrase"];

        var assembly = Assembly.GetExecutingAssembly();
        using (var stream = assembly.GetManifestResourceStream($"Arauco.Otimizador.WebApi.Flow.Certificates.{env.EnvironmentName}.pem"))
        using (var reader = new StreamReader(stream))
        {
            privatePem = reader.ReadToEnd();
        }
    }

    public (string PlainText, byte[] aesKey, byte[] iv) DecryptRequest(Models.FlowEncryptedModel model)
    {
        try
        {
            var encryptedAesKey = Convert.FromBase64String(model.EncryptedAesKey);
            var encryptedFlowData = Convert.FromBase64String(model.EncryptedFlowData);
            var iv = Convert.FromBase64String(model.InitialVector);

            var decryptedAesKey = DecryptAesKey(encryptedAesKey);

            GcmBlockCipher cipher = new GcmBlockCipher(new AesEngine());
            AeadParameters parameters = new AeadParameters(new KeyParameter(decryptedAesKey), 128, iv, null);

            cipher.Init(false, parameters);
            byte[] plainBytes = new byte[cipher.GetOutputSize(encryptedFlowData.Length)];
            int retLen = cipher.ProcessBytes(encryptedFlowData, 0, encryptedFlowData.Length, plainBytes, 0);
            cipher.DoFinal(plainBytes, retLen);

            var plainText = Encoding.UTF8.GetString(plainBytes).TrimEnd("\r\n\0".ToCharArray());

            return (plainText, decryptedAesKey, iv);
        }
        catch (Exception)
        {
            throw new FlowEndpointException(421, "Failed to decrypt the request. Please verify your private key.");
        }
    }

    public string EncryptResponse(string plainText, byte[] key, byte[] iv)
    {
        // Flip initial vector
        var flippedIv = new byte[iv.Length];
        for (var i = 0; i < iv.Length; i++)
        {
            flippedIv[i] = (byte)~iv[i];
        }

        string res = string.Empty;
        try
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);

            GcmBlockCipher cipher = new GcmBlockCipher(new AesFastEngine());
            AeadParameters parameters = new AeadParameters(new KeyParameter(key), 128, flippedIv, null);

            cipher.Init(true, parameters);

            byte[] encryptedBytes = new byte[cipher.GetOutputSize(plainBytes.Length)];
            int retLen = cipher.ProcessBytes(plainBytes, 0, plainBytes.Length, encryptedBytes, 0);
            cipher.DoFinal(encryptedBytes, retLen);
            res = Convert.ToBase64String(encryptedBytes, Base64FormattingOptions.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
        }

        return res;
    }

    private byte[] DecryptAesKey(byte[] encryptedAesKey)
    {
        byte[] decryptedAesKey;
        using (var stringReader = new StringReader(privatePem))
        {
            var pemReader = new PemReader(stringReader, new PasswordFinder(passphrase));

            object keyObject = pemReader.ReadObject();


            if (keyObject is AsymmetricCipherKeyPair keyPair)
            {
                RsaPrivateCrtKeyParameters privateKey = (RsaPrivateCrtKeyParameters)keyPair.Private;

                RSA rsa = RSA.Create();
                RSAParameters rsaParams = new RSAParameters
                {
                    Modulus = privateKey.Modulus.ToByteArrayUnsigned(),
                    Exponent = privateKey.PublicExponent.ToByteArrayUnsigned(),
                    D = privateKey.Exponent.ToByteArrayUnsigned(),
                    P = privateKey.P.ToByteArrayUnsigned(),
                    Q = privateKey.Q.ToByteArrayUnsigned(),
                    DP = privateKey.DP.ToByteArrayUnsigned(),
                    DQ = privateKey.DQ.ToByteArrayUnsigned(),
                    InverseQ = privateKey.QInv.ToByteArrayUnsigned()
                };

                rsa.ImportParameters(rsaParams);

                return rsa.Decrypt(encryptedAesKey, RSAEncryptionPadding.OaepSHA256);
            }
            else if (keyObject is AsymmetricKeyParameter keyParam)
            {
                throw new Exception("Expected an RSA private key pair but got a public key parameter.");
            }
            else
            {
                throw new Exception("Invalid PEM format or decryption failed.");
            }
        }
    }

    class PasswordFinder : IPasswordFinder
    {
        private readonly string password;

        public PasswordFinder(string password)
        {
            this.password = password;
        }

        public char[] GetPassword()
        {
            return password.ToCharArray();
        }
    }

}


