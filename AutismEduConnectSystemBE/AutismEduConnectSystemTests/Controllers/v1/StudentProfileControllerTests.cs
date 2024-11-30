using Xunit;
using AutismEduConnectSystem.Controllers.v1;
using AutismEduConnectSystem.RabbitMQSender;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.SignalR;
using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Microsoft.Extensions.Configuration;
using AutismEduConnectSystem.Services.IServices;
using Microsoft.Extensions.Logging;
using AutismEduConnectSystem.Mapper;

namespace AutismEduConnectSystem.Controllers.v1.Tests
{
    public class StudentProfileControllerTests
    {
        private readonly Mock<IStudentProfileRepository> _mockStudentProfileRepository;
        private readonly Mock<IScheduleTimeSlotRepository> _mockScheduleTimeSlotRepository;
        private readonly Mock<IInitialAssessmentResultRepository> _mockInitialAssessmentResultRepository;
        private readonly Mock<IAssessmentQuestionRepository> _mockAssessmentQuestionRepository;
        private readonly Mock<IChildInformationRepository> _mockChildInfoRepository;
        private readonly Mock<ITutorRequestRepository> _mockTutorRequestRepository;
        private readonly Mock<ITutorRepository> _mockTutorRepository;
        private readonly Mock<IRabbitMQMessageSender> _mockMessageBus;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IRoleRepository> _mockRoleRepository;
        private readonly Mock<IBlobStorageRepository> _mockBlobStorageRepository;
        private readonly Mock<IScheduleRepository> _mockScheduleRepository;
        private readonly Mock<IResourceService> _mockResourceService;
        private readonly Mock<ILogger<StudentProfileController>> _mockLogger;
        private readonly Mock<INotificationRepository> _mockNotificationRepository;
        private readonly Mock<IHubContext<NotificationHub>> _mockHubContext;
        private readonly IMapper _mockMapper;
        private readonly Mock<IConfiguration> _mockConfiguration;

        private readonly StudentProfileController _controller;

        public StudentProfileControllerTests()
        {
            _mockStudentProfileRepository = new Mock<IStudentProfileRepository>();
            _mockScheduleTimeSlotRepository = new Mock<IScheduleTimeSlotRepository>();
            _mockInitialAssessmentResultRepository = new Mock<IInitialAssessmentResultRepository>();
            _mockAssessmentQuestionRepository = new Mock<IAssessmentQuestionRepository>();
            _mockChildInfoRepository = new Mock<IChildInformationRepository>();
            _mockTutorRequestRepository = new Mock<ITutorRequestRepository>();
            _mockTutorRepository = new Mock<ITutorRepository>();
            _mockMessageBus = new Mock<IRabbitMQMessageSender>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockRoleRepository = new Mock<IRoleRepository>();
            _mockBlobStorageRepository = new Mock<IBlobStorageRepository>();
            _mockScheduleRepository = new Mock<IScheduleRepository>();
            _mockResourceService = new Mock<IResourceService>();
            _mockLogger = new Mock<ILogger<StudentProfileController>>();
            _mockNotificationRepository = new Mock<INotificationRepository>();
            _mockHubContext = new Mock<IHubContext<NotificationHub>>();
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingConfig());
            });
            _mockMapper = config.CreateMapper();
            _mockConfiguration = new Mock<IConfiguration>();

            _mockConfiguration.SetupGet(c => c["APIConfig:PageSize"]).Returns("10");
            _mockConfiguration.SetupGet(c => c["RabbitMQSettings:QueueName"]).Returns("some-queue");

            _controller = new StudentProfileController(
                _mockStudentProfileRepository.Object,
                _mockAssessmentQuestionRepository.Object,
                _mockScheduleTimeSlotRepository.Object,
                _mockInitialAssessmentResultRepository.Object,
                _mockChildInfoRepository.Object,
                _mockTutorRequestRepository.Object,
                _mockMapper,
                _mockConfiguration.Object,
                _mockTutorRepository.Object,
                _mockMessageBus.Object,
                _mockUserRepository.Object,
                _mockRoleRepository.Object,
                _mockBlobStorageRepository.Object,
                _mockScheduleRepository.Object,
                _mockResourceService.Object,
                _mockLogger.Object,
                _mockNotificationRepository.Object,
                _mockHubContext.Object
            );
        }











    }
}