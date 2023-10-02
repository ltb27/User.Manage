namespace User.Manage.Services.Models.Emails
{
    public class EmailConfiguration
    {
        public string UserName { get; set; } = null!;
        public string? Email { get; set; } = null!;
        public int Port { get; set; }
        public string Password { get; set; } = null!;
        public string SmtpServer { get; set; } = null!;
    }
}
