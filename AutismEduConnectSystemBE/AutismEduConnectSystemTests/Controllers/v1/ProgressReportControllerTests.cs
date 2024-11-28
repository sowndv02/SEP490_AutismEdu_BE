using Xunit;
using AutismEduConnectSystem.Controllers.v1;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutismEduConnectSystem.Repository.IRepository;
using AutoMapper;
using Moq;
using AutismEduConnectSystem.SignalR;
using Microsoft.AspNetCore.SignalR;
using AutismEduConnectSystem.Services.IServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace AutismEduConnectSystem.Controllers.v1.Tests
{
    public class ProgressReportControllerTests
    {

        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IProgressReportRepository> _mockProgressReportRepository;
        private readonly Mock<IAssessmentResultRepository> _mockAssessmentResultRepository;
        private readonly Mock<IResourceService> _mockResourceService;
        private readonly Mock<ILogger<ProgressReportController>> _mockLogger;
        private readonly ProgressReportController _controller;

        public ProgressReportControllerTests()
        {
            _mockMapper = new Mock<IMapper>();
            _mockProgressReportRepository = new Mock<IProgressReportRepository>();
            _mockAssessmentResultRepository = new Mock<IAssessmentResultRepository>();
            _mockResourceService = new Mock<IResourceService>();
            _mockLogger = new Mock<ILogger<ProgressReportController>>();

            _controller = new ProgressReportController(
                _mockMapper.Object,
                Mock.Of<IConfiguration>(),
                _mockProgressReportRepository.Object,
                _mockAssessmentResultRepository.Object,
                Mock.Of<IInitialAssessmentResultRepository>(),
                _mockResourceService.Object,
                _mockLogger.Object,
                Mock.Of<INotificationRepository>(),
                Mock.Of<IHubContext<NotificationHub>>(),
                Mock.Of<IStudentProfileRepository>(),
                Mock.Of<IChildInformationRepository>(),
                Mock.Of<IUserRepository>()
            );
        }

    }
}