using System.Security.Cryptography;

namespace Zygy.Api.Services.Implementations;

public class AesEncryptionService(byte[] key) : IEncryptionService
{
    private const int IvSize = 16;

    public byte[] Encrypt(byte[] data)
    {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();
        var iv = aes.IV;
        using var encryptor = aes.CreateEncryptor(aes.Key, iv);
        using var ms = new MemoryStream();
        ms.Write(iv, 0, iv.Length);
        using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
        {
            cs.Write(data, 0, data.Length);
            cs.FlushFinalBlock();
        }

        return ms.ToArray();
    }

    public byte[] Decrypt(byte[] data)
    {
        if (data is not { Length: > IvSize })
        {
            throw new ArgumentException("密文数据无效或长度不足。");
        }

        using var aes = Aes.Create();
        aes.Key = key;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        var iv = new byte[IvSize];
        Buffer.BlockCopy(data, 0, iv, 0, IvSize);
        aes.IV = iv;

        var cipherLength = data.Length - IvSize;
        var cipherText = new byte[cipherLength];
        Buffer.BlockCopy(data, IvSize, cipherText, 0, cipherLength);

        using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using var ms = new MemoryStream();
        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
        {
            cs.Write(cipherText, 0, cipherText.Length);
            cs.FlushFinalBlock();
        }

        return ms.ToArray();
    }
}
