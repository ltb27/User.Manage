using MimeKit;

namespace User.Manage.Services.Models.Emails
{
    public class Message
    {
        public List<MailboxAddress> To { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }

        public Message(List<MailboxAddress> To, string subject, string content)
        {
            this.To = new List<MailboxAddress>();
            To.AddRange(To.Select(t => new MailboxAddress(t.Name, t.Address)));
            Subject = subject;
            Content = content;
        }
    }
}
