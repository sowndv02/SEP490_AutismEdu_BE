using backend_api.Data;
using backend_api.RabbitMQSender;

namespace backend_api.Services
{
    /// <summary>
    /// Clean refresh token
    /// Reject tutor request
    /// </summary>
    public class DailyService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DailyService> _logger;

        public DailyService(IServiceProvider serviceProvider, ILogger<DailyService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken); // Run daily
                await RejectTutorRequestAfter2Days();
                await CleanupExpiredTokens();
            }
        }

        private async Task CleanupExpiredTokens()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var expiredTokens = context.RefreshTokens.Where(t => t.TokenType == SD.APPLICATION_REFRESH_TOKEN && (t.ExpiresAt < DateTime.Now || !t.IsValid));

                context.RefreshTokens.RemoveRange(expiredTokens);
                int total = await context.SaveChangesAsync();
                _logger.LogWarning($"Background service clear refresh token at {DateTime.Now} have {total} records deleted"); ;
            }
        }


        private async Task RejectTutorRequestAfter2Days()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var messageBus = scope.ServiceProvider.GetRequiredService<IRabbitMQMessageSender>();

                var tutorRequests = context.TutorRequests.Where(t => t.CreatedDate.AddDays(2) >= DateTime.Now && t.RequestStatus == SD.Status.PENDING);
                foreach(var item in tutorRequests)
                {
                    item.RejectionReason = SD.REQUEST_TIMEOUT_EXPIRED;
                    item.RejectType = SD.RejectType.Other;
                    item.RequestStatus = SD.Status.REJECT;
                    context.TutorRequests.Update(item);
                    await Task.Delay(2000);
                }
                int total = await context.SaveChangesAsync();
                _logger.LogWarning($"Background service reject request at {DateTime.Now} have {total} records updated"); ;
            }
        }

    }
}
