using backend_api.Data;

namespace backend_api.Services
{
    public class RefreshTokenCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;

        public RefreshTokenCleanupService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
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
                var expiredTokens = context.RefreshTokens.Where(t => t.ExpiresAt < DateTime.Now || !t.IsValid);

                context.RefreshTokens.RemoveRange(expiredTokens);
                await context.SaveChangesAsync();
            }
        }
    }
}
