using User.Manage.Services.Models.Emails;

namespace User.Manage.Services.Emails
{
    public interface IEmailService
    {
        Task SendEmailAsync(Message message);
    }
}
