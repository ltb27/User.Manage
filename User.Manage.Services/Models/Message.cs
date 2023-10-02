using MimeKit;

namespace User.Manage.Services.Models.Emails
{
    public class Message
    {
        // people who will receive mail
        public List<MailboxAddress> To { get; set; }
        public string Subject { get; set; }
        public string Content { get; set; }

        public Message(IEnumerable<string> to, string subject, string content)
        {
            To = new List<MailboxAddress>();
            To.AddRange(to.Select(t => new MailboxAddress("email", t)));
            Subject = subject;
            Content = content;
        }
    }
}
