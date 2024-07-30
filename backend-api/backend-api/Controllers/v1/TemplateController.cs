using AutoMapper;
using backend_api.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace backend_api.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class TemplateController: ControllerBase
    {
        //private readonly I....Repository _...Repository; DI Repository
        protected APIResponse _response;
        private readonly IMapper _mapper;
        public TemplateController(IMapper mapper) // Add DI using constructor
        {
            _mapper = mapper;
            _response = new();
        }


        //[HttpGet]
        //public async Task<ActionResult<APIResponse>> GetAllAsync()
        //{
        //    try
        //    {
        //        List<Workspace> list = await _workspaceRepository.GetAllAsync();
        //        _response.Result = _mapper.Map<List<WorkspaceDTO>>(list);
        //        _response.StatusCode = HttpStatusCode.OK;
        //        return Ok(_response);
        //    }
        //    catch (Exception ex)
        //    {
        //        _response.IsSuccess = false;
        //        _response.StatusCode = HttpStatusCode.InternalServerError;
        //        _response.ErrorMessages = new List<string>() { ex.ToString() };
        //        return StatusCode((int)HttpStatusCode.InternalServerError, _response);
        //    }
        //}

        //[HttpGet("{userId}", Name = "GetWorkspaceByUserId")]
        //public async Task<ActionResult<APIResponse>> GetAsyncByUserId(string userId)
        //{
        //    try
        //    {
        //        List<Workspace> list = await _workspaceRepository.GetAllAsync(x => x.OwnerId.Equals(userId) && !x.IsDeleted);
        //        _response.Result = _mapper.Map<List<WorkspaceDTO>>(list);
        //        _response.StatusCode = HttpStatusCode.OK;
        //        return Ok(_response);
        //    }
        //    catch (Exception ex)
        //    {
        //        _response.IsSuccess = false;
        //        _response.StatusCode = HttpStatusCode.InternalServerError;
        //        _response.ErrorMessages = new List<string>() { ex.ToString() };
        //        return StatusCode((int)HttpStatusCode.InternalServerError, _response);
        //    }
        //}


        //[HttpGet("{id:int}", Name = "GetWorkspaceById")]
        //public async Task<ActionResult<APIResponse>> GetByIdAsync(int id)
        //{
        //    try
        //    {
        //        if (id < 0)
        //        {
        //            _response.StatusCode = HttpStatusCode.BadRequest;
        //            _response.IsSuccess = false;
        //            _response.ErrorMessages = new List<string> { $"{id} is invalid!" };
        //            return BadRequest(_response);
        //        }
        //        Workspace model = await _workspaceRepository.GetAsync(x => x.Id == id);
        //        _response.Result = _mapper.Map<WorkspaceDTO>(model);
        //        return Ok(_response);
        //    }
        //    catch (Exception ex)
        //    {
        //        _response.IsSuccess = false;
        //        _response.StatusCode = HttpStatusCode.InternalServerError;
        //        _response.ErrorMessages = new List<string>() { ex.ToString() };
        //        return StatusCode((int)HttpStatusCode.InternalServerError, _response);
        //    }
        //}

        //[HttpPost]
        //public async Task<ActionResult<APIResponse>> CreateAsync([FromBody] WorkspaceDTO createDTO)
        //{
        //    try
        //    {

        //        if (createDTO == null) return BadRequest(createDTO);
        //        Workspace model = _mapper.Map<Workspace>(createDTO);
        //        await _workspaceRepository.CreateAsync(model);
        //        _response.Result = _mapper.Map<WorkspaceDTO>(model);
        //        _response.StatusCode = HttpStatusCode.Created;
        //        return CreatedAtRoute("GetWorkspaceById", new { model.Id }, _response);
        //    }
        //    catch (Exception ex)
        //    {
        //        _response.IsSuccess = false;
        //        _response.StatusCode = HttpStatusCode.InternalServerError;
        //        _response.ErrorMessages = new List<string>() { ex.ToString() };
        //        return StatusCode((int)HttpStatusCode.InternalServerError, _response);
        //    }
        //}

        //[HttpPut("{id:int}")]
        //public async Task<ActionResult<APIResponse>> UpdateAsync(int id, [FromBody] WorkspaceDTO updateDTO)
        //{
        //    try
        //    {
        //        if (updateDTO == null || id != updateDTO.Id)
        //        {
        //            _response.StatusCode = HttpStatusCode.BadRequest;
        //            _response.IsSuccess = false;
        //            _response.ErrorMessages = new List<string>() { "Id invalid!" };
        //            return BadRequest(_response);
        //        }
        //        Workspace model = _mapper.Map<Workspace>(updateDTO);
        //        await _workspaceRepository.UpdateAsync(model);
        //        _response.StatusCode = HttpStatusCode.NoContent;
        //        _response.IsSuccess = true;
        //        return Ok(_response);
        //    }
        //    catch (Exception ex)
        //    {
        //        _response.IsSuccess = false;
        //        _response.StatusCode = HttpStatusCode.InternalServerError;
        //        _response.ErrorMessages = new List<string>() { ex.ToString() };
        //        return StatusCode((int)HttpStatusCode.InternalServerError, _response);
        //    }
        //}


        //[HttpDelete("{id}")]
        //public async Task<ActionResult<APIResponse>> DeleteAsync(int id)
        //{
        //    try
        //    {
        //        if (id == 0) return BadRequest();
        //        var obj = await _workspaceRepository.GetAsync(x => x.Id == id);

        //        if (obj == null) return NotFound();

        //        await _workspaceRepository.RemoveAsync(obj);
        //        _response.StatusCode = HttpStatusCode.NoContent;
        //        _response.IsSuccess = true;
        //        return Ok(_response);

        //    }
        //    catch (Exception ex)
        //    {
        //        _response.IsSuccess = false;
        //        _response.ErrorMessages = new List<string>() { ex.ToString() };
        //        return StatusCode((int)HttpStatusCode.InternalServerError, _response);
        //    }
        //}

        //[HttpDelete("{workspaceId}/{userId}", Name = "DeleteWorkspaceUserByWorkspaceAndUser")]
        //public async Task<ActionResult<APIResponse>> DeleteProjectUserAsync(int workspaceId, string userId)
        //{
        //    try
        //    {
        //        if (workspaceId == 0 || string.IsNullOrEmpty(userId)) return BadRequest();
        //        var obj = await _workspaceUserRepository.GetAsync(x => x.WorkspaceId == workspaceId && x.UserId.Equals(userId));

        //        if (obj == null) return NotFound();

        //        await _workspaceUserRepository.RemoveAsync(obj);
        //        _response.StatusCode = HttpStatusCode.NoContent;
        //        _response.IsSuccess = true;
        //        return Ok(_response);

        //    }
        //    catch (Exception ex)
        //    {
        //        _response.IsSuccess = false;
        //        _response.ErrorMessages = new List<string>() { ex.ToString() };
        //        return StatusCode((int)HttpStatusCode.InternalServerError, _response);
        //    }
        //}

    }
}
