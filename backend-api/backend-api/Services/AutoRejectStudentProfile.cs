using backend_api.Data;
using backend_api.Models;
using backend_api.RabbitMQSender;

using Microsoft.EntityFrameworkCore;

namespace backend_api.Services
{
    public class AutoRejectStudentProfile : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RefreshTokenCleanupService> _logger;
        private readonly IRabbitMQMessageSender _messageBus;
        private string queueName = string.Empty;
        public AutoRejectStudentProfile(IServiceProvider serviceProvider, ILogger<RefreshTokenCleanupService> logger,
            IRabbitMQMessageSender messageBus, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            queueName = configuration.GetValue<string>("RabbitMQSettings:QueueName");
            _messageBus = messageBus;
        }

        protected async override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await RejectProfilesAfter24Hours();
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task RejectProfilesAfter24Hours()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Get all student profiles that were created more than 24 hours ago and have not been approved/rejected
                var profilesToReject = context.StudentProfiles
                    .Where(x => x.Status == SD.StudentProfileStatus.Pending &&
                                x.CreatedDate.AddHours(24) <= DateTime.Now)
                    .Include(x => x.InitialAssessmentResults)
                    .Include(x => x.ScheduleTimeSlots)
                    .Include(x => x.Child)
                    .ThenInclude(x => x.Parent)
                    .ToList();

                foreach (var profile in profilesToReject)
                {
                    profile.Status = SD.StudentProfileStatus.Reject;
                    profile.UpdatedDate = DateTime.Now;

                    context.StudentProfiles.Update(profile);

                    //TODO: send email
                    var tutor = context.Tutors.Where(t => t.TutorId.Equals(profile.TutorId)).Include(t => t.User).FirstOrDefault();
                    var subject = "Thông Báo Xét Duyệt Hồ Sơ Học Sinh";
                    var templatePath = Path.Combine(Directory.GetCurrentDirectory(), "Templates", "AutoRejectStudentProfileTemplate.cshtml");
                    var templateContent = await System.IO.File.ReadAllTextAsync(templatePath);
                    var htmlMessage = templateContent
                        .Replace("@Model.ParentName", profile.Child.Parent.FullName)
                        .Replace("@Model.TutorName", tutor.User.FullName)
                        .Replace("@Model.StudentName", profile.Child.Name)
                        .Replace("@Model.Email", profile.Child.Parent.Email)
                        .Replace("@Model.ProfileCreationDate", profile.CreatedDate.ToString("dd/MM/yyyy"));
                    _messageBus.SendMessage(new EmailLogger()
                    {
                        Email = profile.Child.Parent.Email,
                        Subject = subject,
                        Message = htmlMessage
                    }, queueName);

                    _logger.LogWarning($"Profile {profile.Id} has been rejected automatically after 24 hours.");
                }

                int total = await context.SaveChangesAsync();
                _logger.LogInformation($"Total {total} profiles automatically rejected.");
            }
        }

    }
}
