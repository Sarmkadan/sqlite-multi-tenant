using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SqliteMultiTenant.Security;
using System.Security.Cryptography;
using Xunit;

namespace SqliteMultiTenant.Tests.Security;

public class EncryptionServiceTests
{
    private readonly IConfiguration _config;
    private readonly ILogger<EncryptionService> _logger;
    private readonly IEncryptionService _encryptionService;

    public EncryptionServiceTests()
    {
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = "test-encryption-key-with-32-chars-exactly"
            }!)
            .Build();

        _logger = Substitute.For<ILogger<EncryptionService>>();

        _encryptionService = new EncryptionService(_config, _logger);
    }

    [Fact]
    public void Encrypt_EncryptsNonEmptyString_ReturnsNonEmptyBase64String()
    {
        // Arrange
        var plainText = "Hello, World!";

        // Act
        var cipherText = _encryptionService.Encrypt(plainText);

        // Assert
        cipherText.Should().NotBeNullOrEmpty();
        cipherText.Should().NotBe(plainText);
        cipherText.Should().Match("*"); // Should be base64
    }

    [Fact]
    public void Encrypt_EncryptsEmptyString_ReturnsEmptyString()
    {
        // Arrange
        var plainText = string.Empty;

        // Act
        var cipherText = _encryptionService.Encrypt(plainText);

        // Assert
        cipherText.Should().BeEmpty();
    }

    [Fact]
    public void Encrypt_EncryptsNullString_ReturnsEmptyString()
    {
        // Arrange
        string plainText = null!;

        // Act
        var cipherText = _encryptionService.Encrypt(plainText);

        // Assert
        cipherText.Should().BeEmpty();
    }

    [Fact]
    public void Decrypt_DecryptsValidCipherText_ReturnsOriginalPlainText()
    {
        // Arrange
        var plainText = "Sensitive data to encrypt";
        var cipherText = _encryptionService.Encrypt(plainText);

        // Act
        var decryptedText = _encryptionService.Decrypt(cipherText);

        // Assert
        decryptedText.Should().Be(plainText);
    }

    [Fact]
    public void Decrypt_DecryptsEmptyString_ReturnsEmptyString()
    {
        // Arrange
        var cipherText = string.Empty;

        // Act
        var plainText = _encryptionService.Decrypt(cipherText);

        // Assert
        plainText.Should().BeEmpty();
    }

    [Fact]
    public void Decrypt_DecryptsNullString_ReturnsEmptyString()
    {
        // Arrange
        string cipherText = null!;

        // Act
        var plainText = _encryptionService.Decrypt(cipherText);

        // Assert
        plainText.Should().BeEmpty();
    }

    [Fact]
    public void Decrypt_WithInvalidBase64String_ThrowsException()
    {
        // Arrange
        var invalidCipherText = "not-valid-base64!!!";

        // Act
        Action act = () => _encryptionService.Decrypt(invalidCipherText);

        // Assert
        act.Should().Throw<FormatException>();
    }

    [Fact]
    public void Decrypt_WithWrongKey_ThrowsException()
    {
        // Arrange
        var plainText = "Secret message";
        var cipherText = _encryptionService.Encrypt(plainText);

        // Create service with different key (exactly 32 chars)
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = "different-key-with-32-chars-exact"
            }!)
            .Build();
        var wrongService = new EncryptionService(config, _logger);

        // Act
        Action act = () => wrongService.Decrypt(cipherText);

        // Assert
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void EncryptDecrypt_Roundtrip_WithAsciiString()
    {
        // Arrange
        var plainText = "ASCII text 123!@#";

        // Act
        var cipherText = _encryptionService.Encrypt(plainText);
        var decryptedText = _encryptionService.Decrypt(cipherText);

        // Assert
        decryptedText.Should().Be(plainText);
    }

    [Fact]
    public void EncryptDecrypt_Roundtrip_WithUnicodeString()
    {
        // Arrange
        var plainText = "Unicode text: 你好世界 🌍 Привет мир";

        // Act
        var cipherText = _encryptionService.Encrypt(plainText);
        var decryptedText = _encryptionService.Decrypt(cipherText);

        // Assert
        decryptedText.Should().Be(plainText);
    }

    [Fact]
    public void EncryptDecrypt_Roundtrip_WithLongString()
    {
        // Arrange
        var plainText = new string('A', 10000);

        // Act
        var cipherText = _encryptionService.Encrypt(plainText);
        var decryptedText = _encryptionService.Decrypt(cipherText);

        // Assert
        decryptedText.Should().Be(plainText);
        decryptedText.Should().HaveLength(10000);
    }

    [Fact]
    public void EncryptDecrypt_Roundtrip_WithSpecialCharacters()
    {
        // Arrange
        var plainText = "!@#$%^&*()_+-=[]{}|;':\",./<>?~`";

        // Act
        var cipherText = _encryptionService.Encrypt(plainText);
        var decryptedText = _encryptionService.Decrypt(cipherText);

        // Assert
        decryptedText.Should().Be(plainText);
    }

    [Fact]
    public void EncryptBytes_EncryptsBytes_ReturnsNonEmptyBytes()
    {
        // Arrange
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

        // Act
        var encryptedBytes = _encryptionService.EncryptBytes(data);

        // Assert
        encryptedBytes.Should().NotBeEmpty();
        encryptedBytes.Should().NotBeEquivalentTo(data);
    }

    [Fact]
    public void DecryptBytes_DecryptsBytes_ReturnsOriginalBytes()
    {
        // Arrange
        var originalData = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };
        var encryptedBytes = _encryptionService.EncryptBytes(originalData);

        // Act
        var decryptedBytes = _encryptionService.DecryptBytes(encryptedBytes);

        // Assert
        decryptedBytes.Should().BeEquivalentTo(originalData);
    }

    [Fact]
    public void DecryptBytes_WithInvalidData_ThrowsException()
    {
        // Arrange
        var invalidData = new byte[] { 0x01, 0x02 };

        // Act
        Action act = () => _encryptionService.DecryptBytes(invalidData);

        // Assert
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void DecryptBytes_WithWrongKey_ThrowsException()
    {
        // Arrange
        var originalData = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        var encryptedBytes = _encryptionService.EncryptBytes(originalData);

        // Create service with different key (exactly 32 chars)
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = "different-key-with-32-chars-exact"
            }!)
            .Build();
        var wrongService = new EncryptionService(config, _logger);

        // Act
        Action act = () => wrongService.DecryptBytes(encryptedBytes);

        // Assert
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void HashPassword_ReturnsNonEmptyHash()
    {
        // Arrange
        var password = "MySecurePassword123!";

        // Act
        var hash = _encryptionService.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().Match("*"); // Should be base64
    }

    [Fact]
    public void HashPassword_WithEmptyPassword_ReturnsHash()
    {
        // Arrange
        var password = string.Empty;

        // Act
        var hash = _encryptionService.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void VerifyHash_ReturnsTrue_ForCorrectPassword()
    {
        // Arrange
        var password = "CorrectPassword123!";
        var hash = _encryptionService.HashPassword(password);

        // Act
        var isValid = _encryptionService.VerifyHash(password, hash);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void VerifyHash_ReturnsFalse_ForIncorrectPassword()
    {
        // Arrange
        var correctPassword = "CorrectPassword123!";
        var wrongPassword = "WrongPassword123!";
        var hash = _encryptionService.HashPassword(correctPassword);

        // Act
        var isValid = _encryptionService.VerifyHash(wrongPassword, hash);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void VerifyHash_ReturnsFalse_ForEmptyPassword()
    {
        // Arrange
        var password = "SomePassword123!";
        var hash = _encryptionService.HashPassword(password);

        // Act
        var isValid = _encryptionService.VerifyHash(string.Empty, hash);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void VerifyHash_ReturnsFalse_ForEmptyHash()
    {
        // Arrange
        var password = "SomePassword123!";

        // Act
        var isValid = _encryptionService.VerifyHash(password, string.Empty);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void VerifyHash_ReturnsFalse_ForNullPassword()
    {
        // Arrange
        var password = "SomePassword123!";
        var hash = _encryptionService.HashPassword(password);

        // Act
        var isValid = _encryptionService.VerifyHash(null!, hash);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void VerifyHash_ReturnsFalse_ForNullHash()
    {
        // Arrange
        var password = "SomePassword123!";

        // Act
        var isValid = _encryptionService.VerifyHash(password, null!);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void EncryptDecrypt_Roundtrip_WithWhitespaceString()
    {
        // Arrange
        var plainText = "  Trimmed  \n\t spaces  ";

        // Act
        var cipherText = _encryptionService.Encrypt(plainText);
        var decryptedText = _encryptionService.Decrypt(cipherText);

        // Assert
        decryptedText.Should().Be(plainText);
    }

    [Fact]
    public void EncryptDecrypt_Roundtrip_WithNewlinesAndTabs()
    {
        // Arrange
        var plainText = "Line 1\nLine 2\tTabbed\r\nWindowsLineEndings";

        // Act
        var cipherText = _encryptionService.Encrypt(plainText);
        var decryptedText = _encryptionService.Decrypt(cipherText);

        // Assert
        decryptedText.Should().Be(plainText);
    }
}
