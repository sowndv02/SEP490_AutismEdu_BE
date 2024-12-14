using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using System.Net;
using System.Net.Mail;

namespace AutismEduConnectSystem.Services
{
    public class EmailAccount
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class EmailBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public EmailBackgroundService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        private readonly List<EmailAccount> emailAccounts = new List<EmailAccount>
        {
            new EmailAccount { Host = "smtp.gmail.com", Port = 587, Username = "primary@example.com", Password = "primarypassword" },
            new EmailAccount { Host = "smtp.gmail.com", Port = 587, Username = "backup1@example.com", Password = "backuppassword1" },
            new EmailAccount { Host = "smtp.gmail.com", Port = 587, Username = "backup2@example.com", Password = "backuppassword2" }
        };

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    // Fetch unsent or retryable emails
                    var emailLogs = dbContext.EmailLoggers
                        .Where(e => (!e.SendFirstTime || e.MaxRetries > 0) && e.ErrorCode == null)
                        .OrderBy(e => e.CreatedDate)
                        .ToList();

                    foreach (var email in emailLogs)
                    {
                        try
                        {
                            // Attempt to send the email
                            await SendEmailAsync(email);

                            // Update email log as sent
                            email.SendFirstTime = true;
                            email.MaxRetries = 0;
                            email.ErrorCode = null;
                        }
                        catch (Exception ex)
                        {
                            // Log failure and decrement retries
                            email.ErrorCode = ex.Message;
                            email.MaxRetries--;
                        }

                        dbContext.EmailLoggers.Update(email);
                    }

                    await dbContext.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    // Log any unexpected errors (implement logging as needed)
                    Console.WriteLine($"Unexpected error: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Wait before checking again
            }
        }

        private async Task SendEmailAsync(EmailLogger email)
        {
            foreach (var account in emailAccounts)
            {
                int maxRetries = 3;
                int attempt = 0;

                while (attempt < maxRetries)
                {
                    try
                    {
                        using var smtpClient = new SmtpClient(account.Host, account.Port)
                        {
                            Credentials = new NetworkCredential(account.Username, account.Password),
                            EnableSsl = true
                        };

                        var mailMessage = new MailMessage
                        {
                            From = new MailAddress(account.Username),
                            Subject = email.Subject,
                            Body = email.Message,
                            IsBodyHtml = true
                        };
                        mailMessage.To.Add(email.Email);

                        await smtpClient.SendMailAsync(mailMessage);
                        Console.WriteLine($"Email sent to {email.Email} using account {account.Username}");
                        return; // Exit method when email is sent successfully
                    }
                    catch (SmtpException smtpEx)
                    {
                        attempt++;
                        Console.WriteLine($"Attempt {attempt} failed for account {account.Username}: {smtpEx.Message}");

                        if (IsTemporaryError(smtpEx) && attempt < maxRetries)
                        {
                            Console.WriteLine("Temporary error, retrying...");
                            await Task.Delay(2000); // Wait before retrying
                        }
                        else
                        {
                            Console.WriteLine("Permanent error detected or retries exhausted. Switching to the next account.");
                            break; // Exit retry loop and switch accounts
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                        break; // Exit retry loop and switch accounts
                    }
                }
            }

            throw new Exception($"Failed to send email to {email.Email} using all available accounts.");
        }

        private bool IsTemporaryError(SmtpException smtpEx)
        {
            // Check for temporary error codes
            return smtpEx.StatusCode == SmtpStatusCode.MailboxBusy ||
                   smtpEx.StatusCode == SmtpStatusCode.ServiceNotAvailable ||
                   smtpEx.StatusCode == SmtpStatusCode.InsufficientStorage ||
                   smtpEx.StatusCode == SmtpStatusCode.TransactionFailed;
        }
    }
}
