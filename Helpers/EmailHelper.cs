using System.Net;
using System.Net.Mail;

namespace WebUITopic5_Team4.Helpers
{
    public static class EmailHelper
    {
        public static async Task SendEmailAsync(IConfiguration configuration, string email, string subject, string htmlMessage)
        {
            var mailSettings = configuration.GetSection("MailSettings");
            
            var host = mailSettings["Host"];
            var port = int.Parse(mailSettings["Port"] ?? "587");
            var fromEmail = mailSettings["Email"];
            var password = mailSettings["Password"];
            var displayName = mailSettings["DisplayName"];

            using (var client = new SmtpClient(host, port))
            {
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(fromEmail, password);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail!, displayName),
                    Subject = subject,
                    Body = htmlMessage,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(email);

                await client.SendMailAsync(mailMessage);
            }
        }
    }
}
