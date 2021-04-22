// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Security.Cryptography;
using System.Text;

namespace SqliteMultiTenant.Security;

/// <summary>
/// Provides encryption and decryption services for sensitive data.
/// Supports AES-256 encryption with automatic key derivation.
/// Uses standard PBKDF2 for key derivation from passwords.
/// </summary>
public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
    byte[] EncryptBytes(byte[] data);
    byte[] DecryptBytes(byte[] data);
    bool VerifyHash(string plainText, string hash);
    string HashPassword(string password);
}

public class EncryptionService : IEncryptionService
{
    private readonly string _encryptionKey;
    private readonly ILogger<EncryptionService> _logger;
    private const int KeySize = 256 / 8; // 256 bits
    private const int IvSize = 128 / 8;  // 128 bits
    private const int SaltSize = 128 / 8; // 128 bits
    private const int Iterations = 10000;

    public EncryptionService(IConfiguration config, ILogger<EncryptionService> logger)
    {
        _logger = logger;
        _encryptionKey = config.GetValue<string>("Encryption:Key") ??
            throw new InvalidOperationException("Encryption key not configured");

        if (_encryptionKey.Length < 32)
            throw new InvalidOperationException("Encryption key must be at least 32 characters");
    }

    /// <summary>
    /// Encrypts a string using AES-256.
    /// </summary>
    public string Encrypt(string plainText)
    {
        try
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            byte[] encryptedBytes = EncryptBytes(Encoding.UTF8.GetBytes(plainText));
            return Convert.ToBase64String(encryptedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Encryption error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Decrypts an encrypted string.
    /// </summary>
    public string Decrypt(string cipherText)
    {
        try
        {
            if (string.IsNullOrEmpty(cipherText))
                return string.Empty;

            byte[] cipherBytes = Convert.FromBase64String(cipherText);
            byte[] decryptedBytes = DecryptBytes(cipherBytes);
            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Decryption error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Encrypts raw bytes.
    /// </summary>
    public byte[] EncryptBytes(byte[] data)
    {
        try
        {
            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Generate random IV
                aes.GenerateIV();
                byte[] iv = aes.IV;

                // Derive key from encryption key
                byte[] key = DeriveKey(_encryptionKey);
                aes.Key = key;

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream())
                {
                    // Write IV at the beginning
                    ms.Write(iv, 0, iv.Length);

                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        cs.Write(data, 0, data.Length);
                        cs.FlushFinalBlock();
                    }

                    return ms.ToArray();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Byte encryption error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Decrypts raw bytes.
    /// </summary>
    public byte[] DecryptBytes(byte[] data)
    {
        try
        {
            if (data.Length < IvSize)
                throw new InvalidOperationException("Invalid encrypted data");

            using (var aes = Aes.Create())
            {
                aes.KeySize = 256;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                // Extract IV from the beginning of data
                byte[] iv = new byte[IvSize];
                Array.Copy(data, 0, iv, 0, IvSize);
                aes.IV = iv;

                // Derive key
                byte[] key = DeriveKey(_encryptionKey);
                aes.Key = key;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                using (var ms = new MemoryStream(data, IvSize, data.Length - IvSize))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var resultMs = new MemoryStream())
                {
                    cs.CopyTo(resultMs);
                    return resultMs.ToArray();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Byte decryption error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Verifies a password hash.
    /// </summary>
    public bool VerifyHash(string plainText, string hash)
    {
        try
        {
            if (string.IsNullOrEmpty(plainText) || string.IsNullOrEmpty(hash))
                return false;

            byte[] hashBytes = Convert.FromBase64String(hash);

            if (hashBytes.Length < SaltSize + KeySize)
                return false;

            // Extract salt from hash
            byte[] salt = new byte[SaltSize];
            Array.Copy(hashBytes, 0, salt, 0, SaltSize);

            // Derive key from password with extracted salt
            byte[] derivedKey = DeriveKeyFromPassword(plainText, salt);

            // Compare hashes
            for (int i = 0; i < derivedKey.Length; i++)
            {
                if (derivedKey[i] != hashBytes[i + SaltSize])
                    return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Hash verification error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Hashes a password for storage.
    /// </summary>
    public string HashPassword(string password)
    {
        try
        {
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] derivedKey = DeriveKeyFromPassword(password, salt);

            // Combine salt and hash
            byte[] hashBytes = new byte[salt.Length + derivedKey.Length];
            Array.Copy(salt, 0, hashBytes, 0, salt.Length);
            Array.Copy(derivedKey, 0, hashBytes, salt.Length, derivedKey.Length);

            return Convert.ToBase64String(hashBytes);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Password hashing error: {ex.Message}");
            throw;
        }
    }

    private byte[] DeriveKey(string key)
    {
        byte[] keyBytes = Encoding.UTF8.GetBytes(key);
        byte[] salt = Encoding.UTF8.GetBytes("SqliteMultiTenant");

        using (var pbkdf2 = new Rfc2898DeriveBytes(keyBytes, salt, Iterations, HashAlgorithmName.SHA256))
        {
            return pbkdf2.GetBytes(KeySize);
        }
    }

    private byte[] DeriveKeyFromPassword(string password, byte[] salt)
    {
        using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
        {
            return pbkdf2.GetBytes(KeySize);
        }
    }
}
