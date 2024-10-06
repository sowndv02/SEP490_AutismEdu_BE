using AutoMapper;
using backend_api.Models;
using backend_api.Repository.IRepository;
using backend_api.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend_api.Controllers.v1
{
    [Route("api/[controller]")]
    [ApiController]
    public class TutorRequestController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IWorkExperienceRepository _workExperienceRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IBlobStorageRepository _blobStorageRepository;
        private readonly ILogger<WorkExperienceController> _logger;
        private readonly IMapper _mapper;
        private readonly FormatString _formatString;
        protected APIResponse _response;
        protected int pageSize = 0;
        public TutorRequestController(IUserRepository userRepository, IWorkExperienceRepository workExperienceRepository,
            ILogger<WorkExperienceController> logger, IBlobStorageRepository blobStorageRepository,
            IMapper mapper, IConfiguration configuration, IRoleRepository roleRepository, FormatString formatString)
        {
            _formatString = formatString;
            _roleRepository = roleRepository;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _response = new APIResponse();
            _mapper = mapper;
            _blobStorageRepository = blobStorageRepository;
            _logger = logger;
            _userRepository = userRepository;
            _workExperienceRepository = workExperienceRepository;
        }


    }
}
