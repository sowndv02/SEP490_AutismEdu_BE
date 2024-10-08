﻿using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Models.DTOs.UpdateDTOs;
using backend_api.Repository.IRepository;
using backend_api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;
using static backend_api.SD;

namespace backend_api.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class CurriculumController : ControllerBase
    {
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

        public CurriculumController(IUserRepository userRepository, ITutorRepository tutorRepository,
            ILogger<TutorController> logger, IBlobStorageRepository blobStorageRepository,
            IMapper mapper, IConfiguration configuration, IRoleRepository roleRepository,
            FormatString formatString, IWorkExperienceRepository workExperienceRepository,
            ICertificateRepository certificateRepository, ICertificateMediaRepository certificateMediaRepository,
            ITutorRegistrationRequestRepository tutorRegistrationRequestRepository, ICurriculumRepository curriculumRepository)
        {
            _curriculumRepository = curriculumRepository;
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
        }


        [HttpGet("{id}")]
        public async Task<IActionResult> GetActiveCurriculum(int id)
        {
            var curriculum = await _curriculumRepository.GetAsync(x => x.Id == id && x.IsActive);
            if (curriculum == null)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                return BadRequest(_response);
            }

            _response.StatusCode = HttpStatusCode.Created;
            _response.Result = _mapper.Map<CurriculumDTO>(curriculum);
            _response.IsSuccess = true;
            return Ok(_response);
        }

        [HttpPost("submit")]
        public async Task<IActionResult> SubmitCurriculumUpdate(CurriculumCreateDTO curriculumDto)
        {
            if (!ModelState.IsValid)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                return BadRequest(_response);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var newCurriculum = _mapper.Map<Curriculum>(curriculumDto);

            newCurriculum.SubmiterId = userId;
            newCurriculum.IsActive = false;
            newCurriculum.VersionNumber = await _curriculumRepository.GetNextVersionNumberAsync(curriculumDto.OriginalCurriculumId);

            await _curriculumRepository.CreateAsync(newCurriculum);
            _response.StatusCode = HttpStatusCode.Created;
            _response.Result = _mapper.Map<CurriculumDTO>(newCurriculum);
            _response.IsSuccess = true;
            return Ok(_response);
        }

        [HttpPost("review/{id}")]
        //[Authorize(Roles = "Staff")]
        public async Task<IActionResult> ReviewCurriculumUpdate(int id, [FromBody] ChangeStatusDTO reviewDto)
        {
            var curriculum = await _curriculumRepository.GetAsync(x => x.Id == reviewDto.Id);
            if (curriculum == null || curriculum.RequestStatus != Status.PENDING)
            {
                _response.StatusCode = HttpStatusCode.BadRequest;
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                return BadRequest(_response);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            curriculum.ApprovedId = userId;
            curriculum.UpdatedDate = DateTime.Now;

            if (reviewDto.StatusChange == (int)Status.APPROVE)
            {
                curriculum.RequestStatus = Status.APPROVE;
                curriculum.IsActive = true;
                await _curriculumRepository.DeactivatePreviousVersionsAsync(curriculum.OriginalCurriculumId);
            }
            else if(reviewDto.StatusChange == (int)Status.REJECT)
            {
                curriculum.RequestStatus = Status.REJECT;
                curriculum.RejectionReason = reviewDto.RejectionReason;
            }

            await _curriculumRepository.UpdateAsync(curriculum);
            _response.StatusCode = HttpStatusCode.Created;
            _response.Result = _mapper.Map<CurriculumDTO>(curriculum);
            _response.IsSuccess = true;
            return Ok(_response);
        }


        [HttpPost]
        //[Authorize]
        public async Task<ActionResult<APIResponse>> CreateAsync(CurriculumCreateDTO curriculumCreateDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (curriculumCreateDTO == null || string.IsNullOrEmpty(userId))
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                    return BadRequest(_response);
                }
                Curriculum model = _mapper.Map<Curriculum>(curriculumCreateDTO);
                model.SubmiterId = userId;
                model.CreatedDate = DateTime.Now;
                await _curriculumRepository.CreateAsync(model);
                _response.StatusCode = HttpStatusCode.Created;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPut("changeStatus/{id}")]
        //[Authorize(Policy = "UpdateTutorPolicy")]
        public async Task<IActionResult> ApproveOrRejectCurriculumRequest(ChangeStatusDTO changeStatusDTO)
        {
            try
            {
                var userId = _userRepository.GetAsync(x => x.Email == SD.ADMIN_EMAIL_DEFAULT).GetAwaiter().GetResult().Id;
                //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                //if (string.IsNullOrEmpty(userId))
                //{
                //    _response.StatusCode = HttpStatusCode.BadRequest;
                //    _response.IsSuccess = false;
                //    _response.ErrorMessages = new List<string> { SD.BAD_REQUEST_MESSAGE };
                //    return BadRequest(_response);
                //}

                Curriculum model = await _curriculumRepository.GetAsync(x => x.Id == changeStatusDTO.Id, false, null, null);
                if (changeStatusDTO.StatusChange == (int)Status.APPROVE)
                {
                    model.RequestStatus = Status.APPROVE;
                    model.UpdatedDate = DateTime.Now;
                    model.ApprovedId = userId;
                    await _curriculumRepository.UpdateAsync(model);
                    _response.Result = _mapper.Map<CurriculumDTO>(model);
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    return Ok(_response);
                }
                else if (changeStatusDTO.StatusChange == (int)Status.REJECT)
                {
                    // Handle for reject
                    model.RejectionReason = changeStatusDTO.RejectionReason;
                    model.UpdatedDate = DateTime.Now;
                    model.ApprovedId = userId;
                    await _curriculumRepository.UpdateAsync(model);
                    _response.Result = _mapper.Map<CurriculumDTO>(model);
                    _response.StatusCode = HttpStatusCode.OK;
                    _response.IsSuccess = true;
                    return Ok(_response);
                }
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.Message };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
