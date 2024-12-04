using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using Microsoft.AspNetCore.Identity.UI.Services;
using AutismEduConnectSystem.Repository.IRepository;

namespace AutismEduConnectSystem.Services
{
    /// <summary>
    /// Clean refresh token
    /// Reject tutor request
    /// </summary>
    public class DailyService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DailyService> _logger;
        private readonly string _queueName;

        public DailyService(IServiceProvider serviceProvider, ILogger<DailyService> logger, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _queueName = configuration.GetValue<string>("RabbitMQSettings:QueueName");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var next2AM = GetNextExecutionTime(2); // Run at 2 AM
                var delay = next2AM - DateTime.Now;

                await Task.Delay(delay, stoppingToken);
                await PerformScheduledTasks(stoppingToken);
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken); // Repeat every 24 hours
            }
        }

        private DateTime GetNextExecutionTime(int hour)
        {
            var now = DateTime.Now;
            var nextExecution = now.Date.AddHours(hour);
            return now > nextExecution ? nextExecution.AddDays(1) : nextExecution;
        }

        private async Task PerformScheduledTasks(CancellationToken stoppingToken)
        {
            await Task.WhenAll(
                CleanupExpiredTokens(),
                RejectTutorRequestAfter2Days(),
                CheckAndSendPaymentReminders()
            );
        }

        private async Task CleanupExpiredTokens()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var expiredTokens = context.RefreshTokens
                    .Where(t => t.TokenType == SD.APPLICATION_REFRESH_TOKEN && (t.ExpiresAt < DateTime.Now || !t.IsValid));
                context.RefreshTokens.RemoveRange(expiredTokens);
                var total = await context.SaveChangesAsync();
                _logger.LogInformation($"{total} expired tokens cleaned up at {DateTime.Now}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up expired tokens");
            }
        }


        private async Task RejectTutorRequestAfter2Days()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var tutorRequestRepository = scope.ServiceProvider.GetRequiredService<ITutorRequestRepository>();
                var messageBus = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                var (total, list) = await tutorRequestRepository.GetAllNotPagingAsync(
                    t => t.CreatedDate.AddDays(2) >= DateTime.Now && t.RequestStatus == SD.Status.PENDING, "Parent", null, x => x.CreatedDate, true
                );

                foreach (var item in list)
                {
                    item.RejectionReason = SD.REQUEST_TIMEOUT_EXPIRED;
                    item.RejectType = SD.RejectType.Other;
                    item.RequestStatus = SD.Status.REJECT;
                    await tutorRequestRepository.UpdateAsync(item);

                    var subject = "Thông Báo: Yêu Cầu Đăng Ký Gia Sư Đã Bị Từ Chối Do Hết Hạn";
                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "AutoRejectTutorRequestTemplate.cshtml");
                    var templateContent = await File.ReadAllTextAsync(templatePath);
                    var htmlMessage = templateContent
                        .Replace("@Model.FullName", item.Parent?.FullName)
                        .Replace("@Model.RejectionReason", "Yêu cầu đã hết hạn xác nhận")
                        .Replace("@Model.RejectType", SD.OtherMsg);

                    try
                    {
                        //messageBus.SendMessage(new EmailLogger
                        //{
                        //    Email = item.Parent?.Email,
                        //    Subject = subject,
                        //    Message = htmlMessage
                        //}, _queueName);
                        await messageBus.SendEmailAsync(item.Parent?.Email, subject, htmlMessage);

                        _logger.LogInformation($"{total} tutor requests were automatically rejected due to expiration as of {DateTime.Now:dd/MM/yyyy HH:mm}. Requests rejected include IDs: {string.Join(", ", list.Select(x => x.Id))}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to send tutor requests to user {item.Parent?.Email}");
                    }

                    await Task.Delay(2000);
                }
                _logger.LogInformation($"{total} tutor requests rejected due to expiration at {DateTime.Now}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting expired tutor requests");
            }
        }


        private async Task CheckAndSendPaymentReminders()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var paymentHistoryRepository = scope.ServiceProvider.GetRequiredService<IPaymentHistoryRepository>();
                var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                var messageBus = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                var (total, list) = await paymentHistoryRepository.GetAllNotPagingAsync(
                    x => x.ExpirationDate <= DateTime.Now.AddDays(7) && x.ExpirationDate > DateTime.Now
                );

                foreach (var paymentHistory in list)
                {
                    var user = await userRepository.GetAsync(x => x.Id == paymentHistory.SubmitterId);

                    if (user != null)
                    {
                        var subject = "Thông Báo Gia Hạn Thanh Toán - Tài Khoản Của Bạn Sắp Hết Hạn";
                        var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "PaymentReminderEmail.cshtml");
                        var templateContent = await File.ReadAllTextAsync(templatePath);
                        var htmlMessage = templateContent
                            .Replace("@Model.FullName", user.FullName)
                            .Replace("@Model.ExpirationDate", paymentHistory.ExpirationDate.ToString("dd/MM/yyyy"))
                            .Replace("@Model.PaymentUrl", string.Concat(SD.URL_FE, SD.URL_FE_PAYMENT_QR));

                        try
                        {
                            //messageBus.SendMessage(new EmailLogger
                            //{
                            //    Email = user.Email,
                            //    Subject = subject,
                            //    Message = htmlMessage
                            //}, _queueName);
                            await messageBus.SendEmailAsync(user.Email, subject, htmlMessage);
                            _logger.LogInformation($"Payment reminder sent to user {user.Email} for payment expiring on {paymentHistory.ExpirationDate}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"Failed to send payment reminder to user {user.Email}");
                        }
                    }
                    await Task.Delay(2000);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting expired tutor requests");
            }
        }

    }
}
