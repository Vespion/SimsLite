namespace AuthServer.Data
{
    public class AuthenticationSession
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }
        public string Verifier { get; set; } = null!;

        public string? AuthCode { get; set; }
    }
}