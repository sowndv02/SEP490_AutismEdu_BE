using backend_api.Data;
using backend_api.Models;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Services
{
    public class GenerateScheduleTimeSlot : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GenerateScheduleTimeSlot> _logger;

        public GenerateScheduleTimeSlot(IServiceProvider serviceProvider, ILogger<GenerateScheduleTimeSlot> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                DateTime now = DateTime.Now;
                DateTime nextRunTime = GetNextRunTimeAt1AM(now);

                TimeSpan delay = nextRunTime - now;

                if (delay.TotalMilliseconds > 0)
                {
                    await Task.Delay(delay, stoppingToken);
                }

                await GenerateScheduleTimeSlotForTutor();
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
        }

        private DateTime GetNextRunTimeAt1AM(DateTime currentTime)
        {
            DateTime nextRunDate = currentTime.Date.AddDays(1);
            return nextRunDate.AddHours(1); // 1 AM
        }

        private async Task GenerateScheduleTimeSlotForTutor()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Calculate the start date (tomorrow) and the date two weeks from now
                DateTime startDate = DateTime.Today.AddDays(1);
                DateTime twoWeeksFromNow = DateTime.Today.AddDays(14);

                var scheduleTimeSlots = context.ScheduleTimeSlots
                    .Include(x => x.StudentProfile)
                    .Where(x => x.StudentProfile != null && x.StudentProfile.Status == SD.StudentProfileStatus.Teaching)
                    .ToList();

                int totalGenerated = 0;

                foreach (var item in scheduleTimeSlots)
                {
                    var nextDate = GetNextDayOfWeek((DayOfWeek)item.Weekday);

                    // Skip slots if they have already occurred today and move to next week for this weekday
                    if (nextDate < startDate)
                    {
                        nextDate = nextDate.AddDays(7); // Move to next week's date for the same weekday
                    }

                    // Check if a schedule already exists for this TutorId, slot, and future date within two weeks
                    bool scheduleExists = await context.Schedules.AnyAsync(s =>
                        s.TutorId == item.StudentProfile.TutorId &&
                        s.ScheduleTimeSlotId == item.Id &&
                        s.ScheduleDate >= startDate &&
                        s.ScheduleDate <= twoWeeksFromNow
                    );

                    if (scheduleExists)
                    {
                        _logger.LogInformation($"Schedule already exists for TutorId {item.StudentProfile.TutorId} on {nextDate}. Skipping generation.");
                        continue;
                    }

                    // Create a new schedule if none exists for the upcoming two weeks
                    var schedule = new Schedule()
                    {
                        TutorId = item.StudentProfile.TutorId,
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
                    totalGenerated++;
                }

                await context.SaveChangesAsync();
                _logger.LogWarning($"Background service generated {totalGenerated} new schedule(s) for tutors.");
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
