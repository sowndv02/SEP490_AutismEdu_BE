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
                //DateTime now = DateTime.Now;
                //DateTime nextRunTime = GetNextRunTimeAt1AM(now);

                //TimeSpan delay = nextRunTime - now;

                //if (delay.TotalMilliseconds > 0)
                //{
                //    await Task.Delay(delay, stoppingToken);
                //}

                //await GenerateScheduleTimeSlotForTutor();
                //await Task.Delay(TimeSpan.FromDays(1), stoppingToken);

                 await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); 
                 await GenerateScheduleTimeSlotForTutor();

            }
        }

        private DateTime GetNextRunTimeAt1AM(DateTime currentTime)
        {
            DateTime nextRunDate = currentTime.Date.AddDays(1);
            return nextRunDate.AddHours(1); // 1 AM
        }

        private async Task GenerateScheduleTimeSlotForTutor()
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    // Calculate the start date (tomorrow) and the date three weeks from now
                    DateTime startDate = DateTime.Today.AddDays(1);
                    DateTime threeWeeksFromNow = DateTime.Today.AddDays(21);

                    var scheduleTimeSlots = context.ScheduleTimeSlots
                        .Include(x => x.StudentProfile)
                        .Where(x => x.StudentProfile != null && x.StudentProfile.Status == SD.StudentProfileStatus.Teaching)
                        .ToList();

                    int totalGenerated = 0;

                    foreach (var item in scheduleTimeSlots)
                    {
                        // Loop to generate schedules up to the three-week mark
                        DateTime nextDate = GetNextDayOfWeek((DayOfWeek)item.Weekday);

                        // Adjust the initial nextDate if it's before startDate
                        if (nextDate.Date < startDate.Date)
                        {
                            nextDate = nextDate.AddDays(7);
                        }

                        while (nextDate <= threeWeeksFromNow)
                        {
                            // Check if a schedule already exists for this TutorId, slot, and specific date
                            bool scheduleExists = await context.Schedules.AnyAsync(s =>
                                s.TutorId == item.StudentProfile.TutorId &&
                                s.ScheduleTimeSlotId == item.Id &&
                                s.ScheduleDate.Date == nextDate.Date
                            );

                            if (scheduleExists)
                            {
                                _logger.LogInformation($"Schedule already exists for TutorId {item.StudentProfile.TutorId} on {nextDate}. Skipping generation.");
                            }
                            else
                            {
                                // Create a new schedule for the specified date
                                var schedule = new Schedule()
                                {
                                    TutorId = item.StudentProfile.TutorId,
                                    AttendanceStatus = SD.AttendanceStatus.NOT_YET,
                                    ScheduleDate = nextDate,
                                    StudentProfileId = item.StudentProfileId,
                                    CreatedDate = DateTime.Now,
                                    PassingStatus = SD.PassingStatus.NOT_YET,
                                    UpdatedDate = DateTime.Now,
                                    Start = item.From,
                                    End = item.To,
                                    ScheduleTimeSlotId = item.Id
                                };

                                await context.Schedules.AddAsync(schedule);
                                totalGenerated++;
                            }

                            // Move to the next week's date for the same weekday
                            nextDate = nextDate.AddDays(7);
                        }
                    }

                    await context.SaveChangesAsync();
                    _logger.LogWarning($"Background service generated {totalGenerated} new schedule(s) for tutors over the next 3 weeks.");
                }
            }catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
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
