namespace User.Manage.API.Models.Configuration
{
    public class JWTConfiguration
    {
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public string SecretKey { get; set; } = null!;
    }
}
