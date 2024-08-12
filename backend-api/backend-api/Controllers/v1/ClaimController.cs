using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Net;

namespace backend_api.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class ClaimController : ControllerBase
    {
        private readonly IClaimRepository _claimRepository;
        protected APIResponse _response;
        private readonly IMapper _mapper;

        public ClaimController(IClaimRepository claimRepository, IMapper mapper)
        {
            _claimRepository = claimRepository;
            _mapper = mapper;
            _response = new();
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllClaimsAsync()
        {
            try
            {
                List<ApplicationClaim> list = await _claimRepository.GetAllAsync();
                _response.Result = _mapper.Map<List<ClaimDTO>>(list);
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet("{id:int}", Name = "GetClaimById")]
        public async Task<ActionResult<APIResponse>> GetClaimByIdAsync(int id)
        {
            try
            {
                if (id < 0)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { $"{id} is invalid!" };
                    return BadRequest(_response);
                }

                ApplicationClaim model = await _claimRepository.GetAsync(id);

                if (model == null)
                {
                    _response.StatusCode = HttpStatusCode.NotFound;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { $"Claim with ID {id} not found." };
                    return NotFound(_response);
                }

                _response.Result = _mapper.Map<ClaimDTO>(model);
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string> { ex.ToString() };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPost]
        public async Task<ActionResult<APIResponse>> CreateAsync([FromBody] ClaimDTO claimDTO)
        {
            try
            {
                if (claimDTO == null) return BadRequest(claimDTO);
                ApplicationClaim model = _mapper.Map<ApplicationClaim>(claimDTO);
                await _claimRepository.CreateAsync(model);
                _response.Result = _mapper.Map<ClaimDTO>(model);
                _response.StatusCode = HttpStatusCode.Created;
                return CreatedAtRoute("GetClaimById", new { model.Id }, _response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<APIResponse>> UpdateAsync(int id, [FromBody] ClaimDTO claimDTO)
        {
            try
            {
                if (claimDTO == null || id != claimDTO.Id)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string>() { "Id invalid!" };
                    return BadRequest(_response);
                }
                ApplicationClaim model = _mapper.Map<ApplicationClaim>(claimDTO);
                await _claimRepository.UpdateAsync(model);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult<APIResponse>> DeleteAsync(int id)
        {
            try
            {
                if (id == 0) return BadRequest();
                var obj = await _claimRepository.GetAsync(id);

                if (obj == null) return NotFound();

                await _claimRepository.RemoveAsync(obj);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.IsSuccess = true;
                return Ok(_response);

            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.ToString() };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }
    }
}
