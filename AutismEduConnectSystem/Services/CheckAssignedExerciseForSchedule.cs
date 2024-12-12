using AutismEduConnectSystem.Data;
using AutismEduConnectSystem.Models;
using AutismEduConnectSystem.RabbitMQSender;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Services.IServices;
using AutismEduConnectSystem.SignalR;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace AutismEduConnectSystem.Services
{
    public class CheckAssignedExerciseForSchedule : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GenerateScheduleTimeSlot> _logger;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly string _queueName;
        private readonly IMapper _mapper;

        public CheckAssignedExerciseForSchedule(IServiceProvider serviceProvider, ILogger<GenerateScheduleTimeSlot> logger,
            IConfiguration configuration, IHubContext<NotificationHub> hubContext, IMapper mapper)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _queueName = configuration.GetValue<string>("RabbitMQSettings:QueueName");
            _hubContext = hubContext;
            _mapper = mapper;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                //DateTime now = DateTime.Now;
                //DateTime nextRunTime = GetNextRunTimeAt12PM(now);

                //TimeSpan delay = nextRunTime - now;

                //if (delay.TotalMilliseconds > 0)
                //{
                //    await Task.Delay(delay, stoppingToken);
                //}

                //await CheckForUnAssignedSchedule();
                //await Task.Delay(TimeSpan.FromDays(1), stoppingToken);

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                //await CheckForUnAssignedSchedule();
            }
        }

        private DateTime GetNextRunTimeAt12PM(DateTime currentTime)
        {
            DateTime nextRunDate = currentTime.Date.AddDays(1);
            return nextRunDate.AddHours(12); // 12 PM
        }

        private async Task CheckForUnAssignedSchedule()
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var scheduleRepository = scope.ServiceProvider.GetRequiredService<IScheduleRepository>();
                    var tutorRepository = scope.ServiceProvider.GetRequiredService<ITutorRepository>();
                    var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
                    var resourceService = scope.ServiceProvider.GetRequiredService<IResourceService>();
                    var messageBus = scope.ServiceProvider.GetRequiredService<IRabbitMQMessageSender>();

                    var scheduleNotAssigned = await scheduleRepository
                        .GetAllNotPagingAsync(x => (x.ScheduleDate.Date > DateTime.Today && x.ScheduleDate.Date <= DateTime.Today.AddDays(1))
                                                && (x.ExerciseId == null || x.ExerciseTypeId == null)
                                                && x.StudentProfile.Status == SD.StudentProfileStatus.Teaching
                                                && !x.IsHidden, "StudentProfile");
                    int totalGenerated = 0;

                    foreach (var item in scheduleNotAssigned.list)
                    {
                        // Notification
                        var tutor = await tutorRepository.GetAsync(x => x.TutorId.Equals(item.TutorId), true, "User");

                        if (tutor != null && tutor.User != null)
                        {
                            var connectionId = NotificationHub.GetConnectionIdByUserId(tutor.TutorId);
                            var notfication = new Notification()
                            {
                                ReceiverId = tutor.TutorId,
                                Message = resourceService.GetString(SD.EXERCISE_NOT_ASSIGNED_NOTIFICATION, item.ScheduleDate.ToString("dd/MM/yyyy"), item.Start.ToString(@"hh\:mm"), item.End.ToString(@"hh\:mm")),
                                UrlDetail = string.Concat(SD.URL_FE, SD.URL_FE_TUTOR_STUDENT_PROFILE_DETAIL, item.StudentProfileId),
                                IsRead = false,
                                CreatedDate = DateTime.Now
                            };
                            var notificationResult = await notificationRepository.CreateAsync(notfication);
                            if (!string.IsNullOrEmpty(connectionId))
                            {
                                await _hubContext.Clients.Client(connectionId).SendAsync($"Notifications-{tutor.TutorId}", _mapper.Map<NotificationDTO>(notificationResult));
                            }
                        }

                        var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "ScheduleNotAssignedTemplate.cshtml");
                        if (System.IO.File.Exists(templatePath) && tutor != null && tutor.User != null)
                        {
                            var subject = "Thông Báo Chưa Gán Bài Tập Cho Lịch Học";
                            var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);
                            var htmlMessage = templateContent
                                .Replace("@Model.TutorName", tutor.User.FullName)
                                .Replace("@Model.ScheduleDate", item.ScheduleDate.ToString("dd/MM/yyyy"))
                                .Replace("@Model.Start", item.Start.ToString(@"hh\:mm"))
                                .Replace("@Model.End", item.End.ToString(@"hh\:mm"))
                                .Replace("@Model.Url", string.Concat(SD.URL_FE, SD.URL_FE_TUTOR_STUDENT_PROFILE_DETAIL, item.StudentProfileId))
                                .Replace("@Model.Mail", SD.MAIL)
                                .Replace("@Model.Phone", SD.PHONE_NUMBER)
                                .Replace("@Model.WebsiteURL", SD.URL_FE);

                            messageBus.SendMessage(new EmailLogger()
                            {
                                UserId = tutor.TutorId,
                                Email = tutor.User.Email,
                                Subject = subject,
                                Message = htmlMessage
                            }, _queueName);
                        }
                        totalGenerated++;
                    }

                    _logger.LogWarning($"Background service reminded tutor about {totalGenerated} schedule(s) not assigned with exercise.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
