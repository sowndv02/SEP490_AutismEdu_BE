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
                DateTime now = DateTime.Now;
                DateTime nextRunTime = GetNextSundayAt5AM(now);

                TimeSpan delay = nextRunTime - now;

                if (delay.TotalMilliseconds > 0)
                {
                    await Task.Delay(delay, stoppingToken);
                }
                //await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                await GenerateScheduleTimeSlotForTutor();
                await Task.Delay(TimeSpan.FromDays(7), stoppingToken);
            }
        }
        private DateTime GetNextSundayAt5AM(DateTime currentTime)
        {
            int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)currentTime.DayOfWeek + 7) % 7;
            DateTime nextSunday = currentTime.Date.AddDays(daysUntilSunday);
            return nextSunday.AddHours(5);
        }
        private async Task GenerateScheduleTimeSlotForTutor()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var scheduleTimeSlots = context.ScheduleTimeSlots.Include(x => x.StudentProfile).Where(x => x.StudentProfile != null && x.StudentProfile.Status == SD.StudentProfileStatus.Teaching).ToList();
                foreach (var item in scheduleTimeSlots)
                {
                    var nextDate = DateTime.Now;
                    switch (item.Weekday)
                    {
                        // CN
                        case (int)DayOfWeek.Sunday:
                            nextDate = GetNextDayOfWeek(DayOfWeek.Sunday);
                            break;
                        // T2
                        case (int)DayOfWeek.Monday:
                            nextDate = GetNextDayOfWeek(DayOfWeek.Monday);
                            break;
                        case (int)DayOfWeek.Tuesday:
                            nextDate = GetNextDayOfWeek(DayOfWeek.Tuesday);
                            break;
                        case (int)DayOfWeek.Wednesday:
                            nextDate = GetNextDayOfWeek(DayOfWeek.Wednesday);
                            break;
                        case (int)DayOfWeek.Thursday:
                            nextDate = GetNextDayOfWeek(DayOfWeek.Thursday);
                            break;
                        case (int)DayOfWeek.Friday:
                            nextDate = GetNextDayOfWeek(DayOfWeek.Friday);
                            break;
                        case (int)DayOfWeek.Saturday:
                            nextDate = GetNextDayOfWeek(DayOfWeek.Saturday);
                            break;
                    }
                    var schedule = new Schedule()
                    {
                        TutorId = item.StudentProfile?.TutorId,
                        AttendanceStatus = SD.AttendanceStatus.NOT_YET,
                        ScheduleDate = nextDate,
                        StudentProfileId = item.StudentProfileId,
                        CreatedDate = DateTime.Now,
                        PassingStatus = SD.PassingStatus.NOT_YET,
                        UpdatedDate = null,
                        Start = item.From,
                        End = item.To,
                        ScheduleTimeSlotId = item.Id
                    };
                    await context.Schedules.AddAsync(schedule);
                }

                int total = await context.SaveChangesAsync();
                _logger.LogWarning($"Background service generate schedule for tutor. Total {total} records");
            }
        }

        public DateTime GetNextDayOfWeek(DayOfWeek targetDay)
        {
            DateTime today = DateTime.Now;
            int daysUntilNextTargetDay = ((int)targetDay - (int)today.DayOfWeek + 7) % 7;
            if (daysUntilNextTargetDay == 0)
            {
                daysUntilNextTargetDay = 7;
            }
            return today.AddDays(daysUntilNextTargetDay);
        }

    }
}
