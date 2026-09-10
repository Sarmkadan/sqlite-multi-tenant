namespace SqliteMultiTenant.Security;

/// <summary>
/// Options for encryption configuration.
/// </summary>
public class EncryptionOptions
{
    /// <summary>
    /// Gets or sets the size of the encryption key in bits.
    /// </summary>
    public int KeySize { get; set; } = 256;

    /// <summary>
    /// Gets or sets the size of the initialization vector in bits.
    /// </summary>
    public int IvSize { get; set; } = 128;

    /// <summary>
    /// Gets or sets the size of the salt in bits.
    /// </summary>
    public int SaltSize { get; set; } = 128;

    /// <summary>
    /// Gets or sets the number of iterations for key derivation.
    /// </summary>
    public int Iterations { get; set; } = 10000;

    /// <summary>
    /// Gets or sets the salt string used for key derivation.
    /// </summary>
    public string DerivationSalt { get; set; } = "SqliteMultiTenant";

    public override string ToString() =>
        $"EncryptionOptions {{ KeySize = {KeySize}, IvSize = {IvSize}, SaltSize = {SaltSize}, Iterations = {Iterations}, DerivationSalt = {DerivationSalt} }}";
}