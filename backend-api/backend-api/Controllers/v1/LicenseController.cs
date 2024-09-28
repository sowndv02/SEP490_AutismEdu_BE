using AutoMapper;
using backend_api.Models;
using backend_api.Repository.IRepository;
using backend_api.Utils;
using Microsoft.AspNetCore.Mvc;

namespace backend_api.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class LicenseController : ControllerBase
    {

        private readonly IUserRepository _userRepository;
        private readonly ILicenseRepository _licenseRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IBlobStorageRepository _blobStorageRepository;
        private readonly ILogger<TutorController> _logger;
        private readonly IMapper _mapper;
        private readonly FormatString _formatString;
        protected APIResponse _response;
        protected int pageSize = 0;
        public LicenseController(IUserRepository userRepository, ILicenseRepository licenseRepository,
            ILogger<TutorController> logger, IBlobStorageRepository blobStorageRepository,
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
            _licenseRepository = licenseRepository;
        }


    }
}
