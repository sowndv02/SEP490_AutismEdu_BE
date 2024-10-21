using backend_api.Data;
using backend_api.Models;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Services
{
    public class GenerateScheduleTimeSlot : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RefreshTokenCleanupService> _logger;

        public GenerateScheduleTimeSlot(IServiceProvider serviceProvider, ILogger<RefreshTokenCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // Run daily
                await GenerateScheduleTimeSlotForTutor();
            }
        }

        private async Task GenerateScheduleTimeSlotForTutor()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var scheduleTimeSlots = context.ScheduleTimeSlots.Include(x => x.StudentProfile).Where(x => x.StudentProfile != null && x.StudentProfile.Status == SD.StudentProfileStatus.Teaching).ToList();
                foreach (var item in scheduleTimeSlots)
                {
                    var schedule = new Schedule()
                    {
                        TutorId = item.StudentProfile?.TutorId,
                        ChildId = (int)(item.StudentProfile?.ChildId),
                        AttendanceStatus = SD.AttendanceStatus.NOT_YET,
                        CreatedDate = DateTime.Now,
                        PassingStatus = SD.PassingStatus.NOT_YET,
                        UpdatedDate = null,
                        ScheduleTimeSlotId = item.Id
                    };
                    await context.Schedules.AddAsync(schedule);
                }

                int total = await context.SaveChangesAsync();
                _logger.LogWarning($"Background service generate schedule for tutor. Total {total} records");
            }
        }
    }
}
