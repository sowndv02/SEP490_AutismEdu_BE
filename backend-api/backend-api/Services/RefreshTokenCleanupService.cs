using backend_api.Data;

namespace backend_api.Services
{
    public class RefreshTokenCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RefreshTokenCleanupService> _logger;

        public RefreshTokenCleanupService(IServiceProvider serviceProvider, ILogger<RefreshTokenCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken); // Run daily
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
    }
}
