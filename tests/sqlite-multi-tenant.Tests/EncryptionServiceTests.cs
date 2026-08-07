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
        _logger.LogInformation("Initializing EncryptionServiceTests");
        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = "test-encryption-key-with-32-chars-exactly"
            }!)
            .Build();

        _logger.LogInformation("Creating configuration for EncryptionServiceTests");
        _logger = Substitute.For<ILogger<EncryptionService>>();

        _logger.LogInformation("Creating EncryptionService instance");
        _encryptionService = new EncryptionService(_config, _logger);
        _logger.LogInformation("EncryptionService instance created");
    }

    [Fact]
    public void Encrypt_EncryptsNonEmptyString_ReturnsNonEmptyBase64String()
    {
        _logger.LogInformation("Testing Encrypt with non-empty string");
        // Arrange
        var plainText = "Hello, World!";

        // Act
        var cipherText = _encryptionService.Encrypt(plainText);

        // Assert
        cipherText.Should().NotBeNullOrEmpty();
        cipherText.Should().NotBe(plainText);
        cipherText.Should().Match("*"); // Should be base64
        _logger.LogInformation("Test Encrypt with non-empty string completed");
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
        _logger.LogInformation("Testing Decrypt with valid cipher text");
        // Arrange
        var plainText = "Sensitive data to encrypt";
        var cipherText = _encryptionService.Encrypt(plainText);

        // Act
        var decryptedText = _encryptionService.Decrypt(cipherText);

        // Assert
        decryptedText.Should().Be(plainText);
        _logger.LogInformation("Test Decrypt with valid cipher text completed");
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
        _logger.LogInformation("Testing Decrypt with invalid base64 string");
        // Arrange
        var invalidCipherText = "not-valid-base64!!!";

        // Act
        Action act = () => _encryptionService.Decrypt(invalidCipherText);

        // Assert
        act.Should().Throw<FormatException>();
        _logger.LogInformation("Test Decrypt with invalid base64 string completed");
    }

    [Fact]
    public void Decrypt_WithWrongKey_ThrowsException()
    {
        _logger.LogInformation("Testing Decrypt with wrong key");
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
        _logger.LogInformation("Test Decrypt with wrong key completed");
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
        _logger.LogInformation("Testing EncryptBytes");
        // Arrange
        var data = new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05 };

        // Act
        var encryptedBytes = _encryptionService.EncryptBytes(data);

        // Assert
        encryptedBytes.Should().NotBeEmpty();
        encryptedBytes.Should().NotBeEquivalentTo(data);
        _logger.LogInformation("Test EncryptBytes completed");
    }

    [Fact]
    public void DecryptBytes_DecryptsBytes_ReturnsOriginalBytes()
    {
        _logger.LogInformation("Testing DecryptBytes");
        // Arrange
        var originalData = new byte[] { 0x48, 0x65, 0x6C, 0x6C, 0x6F };
        var encryptedBytes = _encryptionService.EncryptBytes(originalData);

        // Act
        var decryptedBytes = _encryptionService.DecryptBytes(encryptedBytes);

        // Assert
        decryptedBytes.Should().BeEquivalentTo(originalData);
        _logger.LogInformation("Test DecryptBytes completed");
    }

    [Fact]
    public void DecryptBytes_WithInvalidData_ThrowsException()
    {
        _logger.LogInformation("Testing DecryptBytes with invalid data");
        // Arrange
        var invalidData = new byte[] { 0x01, 0x02 };

        // Act
        Action act = () => _encryptionService.DecryptBytes(invalidData);

        // Assert
        act.Should().Throw<InvalidOperationException>();
        _logger.LogInformation("Test DecryptBytes with invalid data completed");
    }

    [Fact]
    public void DecryptBytes_WithWrongKey_ThrowsException()
    {
        _logger.LogInformation("Testing DecryptBytes with wrong key");
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
        _logger.LogInformation("Test DecryptBytes with wrong key completed");
    }

    [Fact]
    public void HashPassword_ReturnsNonEmptyHash()
    {
        _logger.LogInformation("Testing HashPassword");
        // Arrange
        var password = "MySecurePassword123!";

        // Act
        var hash = _encryptionService.HashPassword(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().Match("*"); // Should be base64
        _logger.LogInformation("Test HashPassword completed");
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
        _logger.LogInformation("Testing VerifyHash with correct password");
        // Arrange
        var password = "CorrectPassword123!";
        var hash = _encryptionService.HashPassword(password);

        // Act
        var isValid = _encryptionService.VerifyHash(password, hash);

        // Assert
        isValid.Should().BeTrue();
        _logger.LogInformation("Test VerifyHash with correct password completed");
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
