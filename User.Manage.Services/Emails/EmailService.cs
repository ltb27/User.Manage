using MimeKit;
using Microsoft.Extensions.Options;
using User.Manage.Services.Models.Emails;
using MailKit.Net.Smtp;

namespace User.Manage.Services.Emails
{
    public class EmailService : IEmailService
    {
        private readonly IOptions<EmailConfiguration> emailConfiguration;

        public EmailService(IOptions<EmailConfiguration> emailConfiguration)
        {
            this.emailConfiguration = emailConfiguration;
        }

        public async Task SendEmailAsync(Message message)
        {
            var email = CreateEmail(message);

            await SendAsync(email);
        }

        // create email
        private MimeMessage CreateEmail(Message message)
        {
            var emailMessage = new MimeMessage();

            // from
            emailMessage.From.Add(new MailboxAddress("email", emailConfiguration.Value.From));
            emailMessage.To.AddRange(message.To);
            emailMessage.Subject = message.Subject;
            emailMessage.Body = new TextPart(MimeKit.Text.TextFormat.Text)
            {
                Text = message.Content
            };

            return emailMessage;
        }

        // connect to smtp server and send email
        private async Task SendAsync(MimeMessage message)
        {
            // MailKit.Net.Smtp
            using var smtpClient = new SmtpClient();

            try
            {
                await smtpClient.ConnectAsync(
                    emailConfiguration.Value.SmtpServer,
                    emailConfiguration.Value.Port
                );

                smtpClient.AuthenticationMechanisms.Remove("XOAUTH2");

                await smtpClient.AuthenticateAsync(
                    emailConfiguration.Value.UserName,
                    emailConfiguration.Value.Password
                );

                await smtpClient.SendAsync(message);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                await smtpClient.DisconnectAsync(true);
            }
        }
    }
}
