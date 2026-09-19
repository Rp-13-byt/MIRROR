using System.Security.Cryptography;
using System.Text;

namespace Mirror.Security;

public static class AesGcmEncryptor
{
    private static readonly byte[] MagicHeader = { 0x4D, 0x49, 0x52, 0x57 }; // MIRW
    private const int SaltSize = 32;
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32; // AES-256
    private const int Iterations = 100_000;

    public static byte[] Encrypt(byte[] plaintext, string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
        byte[] key = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            Iterations,
            HashAlgorithmName.SHA256,
            KeySize);

        byte[] tag = new byte[TagSize];
        byte[] ciphertext = new byte[plaintext.Length];

        using (var aesGcm = new AesGcm(key, TagSize))
        {
            aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);
        }

        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write(MagicHeader);
        writer.Write(salt);
        writer.Write(nonce);
        writer.Write(tag);
        writer.Write(ciphertext);

        return ms.ToArray();
    }

    public static byte[] Decrypt(byte[] encryptedData, string password)
    {
        if (encryptedData.Length < MagicHeader.Length + SaltSize + NonceSize + TagSize)
        {
            throw new CryptographicException("Corrupted or invalid encrypted payload.");
        }

        using var ms = new MemoryStream(encryptedData);
        using var reader = new BinaryReader(ms);

        byte[] header = reader.ReadBytes(MagicHeader.Length);
        if (!header.SequenceEqual(MagicHeader))
        {
            throw new CryptographicException("Invalid archive header. Not a valid Mirror Wallet file.");
        }

 byte[] salt = reader.ReadBytes(SaltSize);
 byte[] nonce = reader.ReadBytes(NonceSize);
 byte[] tag = reader.ReadBytes(TagSize);
 byte[] ciphertext = reader.ReadBytes((int)(ms.Length - ms.Position));

 byte[] key = Rfc2898DeriveBytes.Pbkdf2(
 Encoding.UTF8.GetBytes(password),
 salt,
 Iterations,
 HashAlgorithmName.SHA256,
 KeySize);

 byte[] plaintext = new byte[ciphertext.Length];
 using (var aesGcm = new AesGcm(key, TagSize))
 {
 aesGcm.Decrypt(nonce, ciphertext, tag, plaintext);
 }

 return plaintext;
 }
}