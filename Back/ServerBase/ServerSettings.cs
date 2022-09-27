namespace ServerBase
{
    public class ServerSettings
    {
        public int? ListeningPort { get; set; }

        public string? CertificateKey { get; set; }

        public string? Issuer { get; set; }

        public string? Audience { get; set; }

        // ReSharper disable once InconsistentNaming
        public string? JWTKey { get; set; }
    }

    public class SecuritySettings
    {
        public string? KeyringDirectory { get; set; } = "./keyring";

        public string? KeyringCertificateDirectory {get;set;}
        
        public string? PrimaryKeyringCertificate {get;set;}
        
        public string? ApplicationDiscriminator { get; set; }
    }
}