using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;

namespace AutismEduConnectSystem.Services
{

    public class SendMailService : IEmailSender
    {
        private readonly MailSettings mailSettings;
        private DbContextOptions<ApplicationDbContext> _dbOptions;
        public SendMailService(IOptions<MailSettings> _mailSettings, DbContextOptions<ApplicationDbContext> dbOptions)
        {
            _dbOptions = dbOptions;
            mailSettings = _mailSettings.Value;
        }

        public async Task EmailAndLog(EmailLogger emailLogger)
        {
            try
            {
                await using var _db = new ApplicationDbContext(_dbOptions);
                await _db.EmailLoggers.AddAsync(emailLogger);
                await _db.SaveChangesAsync();
                await SendEmailAsync(emailLogger.Email, emailLogger.Subject, emailLogger.Message);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var message = new MimeMessage();
            message.Sender = new MailboxAddress(mailSettings.DisplayName, mailSettings.Mail);
            message.From.Add(new MailboxAddress(mailSettings.DisplayName, mailSettings.Mail));
            message.To.Add(MailboxAddress.Parse(email));
            message.Subject = subject;

            var builder = new BodyBuilder();
            builder.HtmlBody = htmlMessage;
            message.Body = builder.ToMessageBody();

            // dùng SmtpClient của MailKit
            using var smtp = new MailKit.Net.Smtp.SmtpClient();

            try
            {
                //smtp.Connect(mailSettings.Host, mailSettings.Port, SecureSocketOptions.StartTls);
                //smtp.Authenticate(mailSettings.Mail, mailSettings.Password);
                //await smtp.SendAsync(message);
                await using var _db = new ApplicationDbContext(_dbOptions);
                await _db.EmailLoggers.AddAsync(new EmailLogger()
                {
                    MaxRetries = 3,
                    SendFirstTime = false,
                    Email = email,
                    Subject = subject,
                    Message = htmlMessage
                });
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Gửi mail thất bại, nội dung email sẽ lưu vào thư mục mailssave
                System.IO.Directory.CreateDirectory("mailssave");
                var emailsavefile = string.Format(@"mailssave/{0}.eml", Guid.NewGuid());
                await message.WriteToAsync(emailsavefile);
            }
            smtp.Disconnect(true);
        }
    }
}
