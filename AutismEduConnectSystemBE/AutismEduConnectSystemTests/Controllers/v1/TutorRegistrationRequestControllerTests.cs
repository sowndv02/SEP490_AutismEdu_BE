using Xunit;
using AutismEduConnectSystem.Controllers.v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutismEduConnectSystem.RabbitMQSender;
using AutismEduConnectSystem.Repository.IRepository;
using AutismEduConnectSystem.Utils;
using AutoMapper;
using Moq;
using AutismEduConnectSystem.Services.IServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using AutismEduConnectSystem.Mapper;

namespace AutismEduConnectSystem.Controllers.v1.Tests
{
    public class TutorRegistrationRequestControllerTests
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<ITutorRepository> _mockTutorRepository;
        private readonly Mock<IRabbitMQMessageSender> _mockMessageBus;
        private readonly Mock<ITutorRegistrationRequestRepository> _mockTutorRegistrationRequestRepository;
        private readonly Mock<ICurriculumRepository> _mockCurriculumRepository;
        private readonly Mock<IWorkExperienceRepository> _mockWorkExperienceRepository;
        private readonly Mock<ICertificateMediaRepository> _mockCertificateMediaRepository;
        private readonly Mock<ICertificateRepository> _mockCertificateRepository;
        private readonly Mock<IRoleRepository> _mockRoleRepository;
        private readonly Mock<IBlobStorageRepository> _mockBlobStorageRepository;
        private readonly Mock<ILogger<TutorRegistrationRequestController>> _mockLogger;
        private readonly IMapper _mockMapper;
        private readonly Mock<IResourceService> _mockResourceService;
        private readonly Mock<IConfiguration> _mockConfiguration;

        private readonly TutorRegistrationRequestController _controller;

        public TutorRegistrationRequestControllerTests()
        {
            // Initialize mocks
            _mockUserRepository = new Mock<IUserRepository>();
            _mockTutorRepository = new Mock<ITutorRepository>();
            _mockMessageBus = new Mock<IRabbitMQMessageSender>();
            _mockTutorRegistrationRequestRepository = new Mock<ITutorRegistrationRequestRepository>();
            _mockCurriculumRepository = new Mock<ICurriculumRepository>();
            _mockWorkExperienceRepository = new Mock<IWorkExperienceRepository>();
            _mockCertificateMediaRepository = new Mock<ICertificateMediaRepository>();
            _mockCertificateRepository = new Mock<ICertificateRepository>();
            _mockRoleRepository = new Mock<IRoleRepository>();
            _mockBlobStorageRepository = new Mock<IBlobStorageRepository>();
            _mockLogger = new Mock<ILogger<TutorRegistrationRequestController>>();
            _mockResourceService = new Mock<IResourceService>();
            _mockConfiguration = new Mock<IConfiguration>();
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new MappingConfig());
            });
            _mockMapper = config.CreateMapper();
            // Setup configuration mock (if needed)
            _mockConfiguration.Setup(c => c["APIConfig:PageSize"]).Returns("10");
            _mockConfiguration.Setup(c => c["RabbitMQSettings:QueueName"]).Returns("testQueue");

            // Create controller instance using the mocked dependencies
            _controller = new TutorRegistrationRequestController(
                _mockUserRepository.Object,
                _mockTutorRepository.Object,
                _mockLogger.Object,
                _mockBlobStorageRepository.Object,
                _mockMapper,
                _mockConfiguration.Object,
                _mockRoleRepository.Object,
                new FormatString(),
                _mockWorkExperienceRepository.Object,
                _mockCertificateRepository.Object,
                _mockCertificateMediaRepository.Object,
                _mockTutorRegistrationRequestRepository.Object,
                _mockCurriculumRepository.Object,
                _mockMessageBus.Object,
                _mockResourceService.Object
            );
        }

    }
}