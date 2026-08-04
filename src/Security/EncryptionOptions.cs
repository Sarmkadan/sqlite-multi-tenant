namespace SqliteMultiTenant.Security;

public class EncryptionOptions
{
    public int KeySize { get; set; } = 256;
    public int IvSize { get; set; } = 128;
    public int SaltSize { get; set; } = 128;
    public int Iterations { get; set; } = 10000;
    public string DerivationSalt { get; set; } = "SqliteMultiTenant";
}
