using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Models.DTOs;
using backend_api.Repository.IRepository;
using backend_api.Utils;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace backend_api.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class ExerciseController : ControllerBase
    {
        private readonly IExerciseRepository _exerciseRepository;
        private readonly IExerciseTypeRepository _exerciseTypeRepository;
        private readonly IUserRepository _userRepository;
        private readonly ITutorRepository _tutorRepository;
        private readonly ITutorRegistrationRequestRepository _tutorRegistrationRequestRepository;
        private readonly ICurriculumRepository _curriculumRepository;
        private readonly IWorkExperienceRepository _workExperienceRepository;
        private readonly ICertificateMediaRepository _certificateMediaRepository;
        private readonly ICertificateRepository _certificateRepository;
        private readonly IRoleRepository _roleRepository;
        private readonly IBlobStorageRepository _blobStorageRepository;
        private readonly ILogger<TutorController> _logger;
        private readonly IMapper _mapper;
        private readonly FormatString _formatString;
        protected APIResponse _response;
        protected int pageSize = 0;

        public ExerciseController(IUserRepository userRepository, ITutorRepository tutorRepository,
            ILogger<TutorController> logger, IBlobStorageRepository blobStorageRepository,
            IMapper mapper, IConfiguration configuration, IRoleRepository roleRepository,
            FormatString formatString, IWorkExperienceRepository workExperienceRepository,
            ICertificateRepository certificateRepository, ICertificateMediaRepository certificateMediaRepository,
            ITutorRegistrationRequestRepository tutorRegistrationRequestRepository, IExerciseRepository exerciseRepository, IExerciseTypeRepository exerciseTypeRepository)
        {
            _formatString = formatString;
            _roleRepository = roleRepository;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _response = new APIResponse();
            _mapper = mapper;
            _blobStorageRepository = blobStorageRepository;
            _logger = logger;
            _userRepository = userRepository;
            _tutorRepository = tutorRepository;
            _workExperienceRepository = workExperienceRepository;
            _certificateRepository = certificateRepository;
            _certificateMediaRepository = certificateMediaRepository;
            _tutorRegistrationRequestRepository = tutorRegistrationRequestRepository;
            _exerciseRepository = exerciseRepository;
            _exerciseTypeRepository = exerciseTypeRepository;
        }

        [HttpPost]
        public async Task<ActionResult<APIResponse>> CreateExerciseAsync(ExerciseCreateDTO exerciseCreateDTO)
        {
            try
            {
                // Check if ExerciseTypeId exists
                var exerciseType = await _exerciseTypeRepository.GetAsync(x => x.Id == exerciseCreateDTO.ExerciseTypeId);
                if (exerciseType == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { "Invalid ExerciseTypeId." };
                    return BadRequest(_response);
                }

                // Create a new Exercise object
                var exercise = _mapper.Map<Exercise>(exerciseCreateDTO);
                exercise.ExerciseType = exerciseType;
                // Save the new exercise
                var createdExercise = await _exerciseRepository.CreateAsync(exercise);

                // Prepare response DTO
                var exerciseDTO = new ExerciseDTO
                {
                    Id = createdExercise.ExerciseId,
                    ExerciseName = createdExercise.ExerciseName,
                    ExerciseContent = createdExercise.ExerciseContent,
                    TutorId = createdExercise.TutorId,
                    ExerciseTypeId = createdExercise.ExerciseTypeId  // Add this if needed in DTO
                };

                // Set the response
                _response.Result = exerciseDTO;
                _response.StatusCode = HttpStatusCode.Created;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
