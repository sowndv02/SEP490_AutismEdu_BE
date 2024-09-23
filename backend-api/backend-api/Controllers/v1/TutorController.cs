using AutoMapper;
using backend_api.Models;
using backend_api.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;

namespace backend_api.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class TutorController : ControllerBase
    {
        private readonly IUserRepository _userRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IBlobStorageRepository _blobStorageRepository;
        private readonly ILogger<UserController> _logger;
        private readonly IMapper _mapper;
        protected APIResponse _response;
        protected int pageSize = 0;
        public TutorController()
        {

        }
    }
}
