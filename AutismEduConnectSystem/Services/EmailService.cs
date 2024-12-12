using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.Services.IServices;
using MailKit.Security;

using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using System;
using System.Net;
using System.Net.Mail;

namespace AutismEduConnectSystem.Services
{
    public class MailSettings
    {
        public string Mail { get; set; }
        public string DisplayName { get; set; }
        public string Password { get; set; }
        public string Host { get; set; }
        public int Port { get; set; }

    }


    public class EmailService : IEmailService
    {
        private readonly MailSettings _mailSettings;
        private DbContextOptions<ApplicationDbContext> _dbOptions;
        private readonly ILogger<EmailService> _logger;
        public EmailService(IOptions<MailSettings> mailSettings, ILogger<EmailService> logger, DbContextOptions<ApplicationDbContext> dbOptions)
        {
            _dbOptions = dbOptions;
            _mailSettings = mailSettings.Value;
            _logger = logger;
            _logger.LogInformation("Create SendMailService with RabbitMQ");
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
            message.Sender = new MailboxAddress(_mailSettings.DisplayName, _mailSettings.Mail);
            message.From.Add(new MailboxAddress(_mailSettings.DisplayName, _mailSettings.Mail));
            message.To.Add(MailboxAddress.Parse(email));
            message.Subject = subject;

            var builder = new BodyBuilder();
            builder.HtmlBody = htmlMessage;
            message.Body = builder.ToMessageBody();

            // dùng SmtpClient của MailKit
            using var smtp = new MailKit.Net.Smtp.SmtpClient();

            try
            {
                smtp.Connect(_mailSettings.Host, _mailSettings.Port, SecureSocketOptions.StartTls);
                smtp.Authenticate(_mailSettings.Mail, _mailSettings.Password);
                await smtp.SendAsync(message);
                Console.WriteLine("Send mail successful");
            }
            catch (Exception ex)
            {
                // Gửi mail thất bại, nội dung email sẽ lưu vào thư mục mailssave
                System.IO.Directory.CreateDirectory("MailSave");
                var emailsavefile = string.Format(@"MailSave/{0}.eml", Guid.NewGuid());
                await message.WriteToAsync(emailsavefile);
                Console.WriteLine(ex.Message);
                _logger.LogInformation("Lỗi gửi mail, lưu tại - " + emailsavefile);
                _logger.LogError(ex.Message);
            }
            smtp.Disconnect(true);
            smtp.Dispose();
            _logger.LogInformation("send mail to: " + email);
        }

        //public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        //{
        //    try
        //    {
        //        // Setup SMTP client
        //        var smtpClient = new SmtpClient(_mailSettings.Host, _mailSettings.Port)
        //        {
        //            Credentials = new NetworkCredential(_mailSettings.Mail, _mailSettings.Password),
        //            EnableSsl = true, // Ensure SSL/TLS is enabled if needed
        //        };

        //        // Create the email message
        //        var mailMessage = new MailMessage
        //        {
        //            From = new MailAddress(_mailSettings.Mail, _mailSettings.DisplayName),
        //            Subject = subject,
        //            Body = htmlMessage,
        //            IsBodyHtml = true, // HTML content
        //        };

        //        mailMessage.To.Add(email);

        //        // Send the email asynchronously
        //        await smtpClient.SendMailAsync(mailMessage);
        //        Console.WriteLine("Send mail successful");
        //    }
        //    catch (Exception ex)
        //    {
        //        // Handle error and save email to file if needed
        //        System.IO.Directory.CreateDirectory("MailSave");
        //        var emailsavefile = $"MailSave/{Guid.NewGuid()}.eml";
        //        //await SaveEmailAsync(mailMessage, emailsavefile);
        //        Console.WriteLine(ex.Message);
        //        _logger.LogInformation("Error sending email, saved to: " + emailsavefile);
        //        _logger.LogError(ex.Message);
        //    }
        //}

        private async Task SaveEmailAsync(MailMessage mailMessage, string filePath)
        {
            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                using (var writer = new StreamWriter(fileStream))
                {
                    await writer.WriteAsync(mailMessage.ToString());
                }
                _logger.LogInformation($"Email saved to {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save email");
            }
        }


    }
}
