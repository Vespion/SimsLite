namespace AuthServer
{
    public class JwtOptions
    {
        public string? Key { get; set; }

        public int? Expiry { get; set; }
    }
}