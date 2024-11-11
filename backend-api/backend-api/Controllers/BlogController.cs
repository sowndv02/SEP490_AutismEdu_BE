using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Models.DTOs.UpdateDTOs;
using backend_api.Repository;
using backend_api.Repository.IRepository;
using backend_api.Services.IServices;
using backend_api.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq.Expressions;
using System.Net;
using System.Security.Claims;

namespace backend_api.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersionNeutral]
    public class BlogController : ControllerBase
    {
        private readonly IBlogRepository _blogRepository;
        private readonly IBlobStorageRepository _blobStorageRepository;
        private readonly ILogger<BlogController> _logger;
        private readonly IMapper _mapper;
        protected APIResponse _response;
        protected int pageSize = 0;
        private readonly IResourceService _resourceService;
        public BlogController(ILogger<BlogController> logger, IBlobStorageRepository blobStorageRepository,
            IMapper mapper, IConfiguration configuration, IBlogRepository blogRepository, IResourceService resourceService)
        {
            _resourceService = resourceService;
            _blogRepository = blogRepository;
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _response = new APIResponse();
            _mapper = mapper;
            _blobStorageRepository = blobStorageRepository;
            _logger = logger;
        }


        [HttpPost]
        [Authorize(Roles = $"{SD.STAFF_ROLE},{SD.MANAGER_ROLE}")]
        public async Task<ActionResult<APIResponse>> CreateAsync([FromForm] BlogCreateDTO createDTO)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (createDTO == null)
                {
                    _logger.LogWarning("Invalid BlogCreateDTO received. createDTO is null.");
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.BLOG) };
                    return BadRequest(_response);
                }
                Blog model = _mapper.Map<Blog>(createDTO);
                model.AuthorId = userId;
                if(model.IsPublished) model.PublishDate = DateTime.Now;
                model.CreatedDate = DateTime.Now;
                if(createDTO.ImageDisplay != null)
                {
                    using var mediaStream = createDTO.ImageDisplay.OpenReadStream();
                    string mediaUrl = await _blobStorageRepository.Upload(mediaStream, string.Concat(Guid.NewGuid().ToString(), Path.GetExtension(createDTO.ImageDisplay.FileName)));
                    model.UrlImageDisplay = mediaUrl;
                }
                await _blogRepository.CreateAsync(model);
                _response.IsSuccess = true;
                _response.Result = _mapper.Map<BlogDTO>(model);
                _response.StatusCode = HttpStatusCode.Created;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error occurred while creating an blog: {Message}", ex.Message);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

        [HttpGet]
        public async Task<ActionResult<APIResponse>> GetAllAsync([FromQuery] string? search, DateTime? startDate = null, DateTime? endDate = null, string? orderBy = SD.PUBLISH_DATE, string? sort = SD.ORDER_DESC, int pageNumber = 1)
        {
            try
            {

                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                Expression<Func<Blog, bool>> filter = u => true;
                Expression<Func<Blog, object>> orderByQuery = u => true;

                bool isDesc = !string.IsNullOrEmpty(sort) && sort == SD.ORDER_DESC;
                int total = 0;
                if (orderBy != null)
                {
                    switch (orderBy)
                    {
                        case SD.CREATED_DATE:
                            orderByQuery = x => x.CreatedDate;
                            break;
                        case SD.PUBLISH_DATE:
                            orderByQuery = x => x.PublishDate;
                            break;
                        default:
                            orderByQuery = x => x.PublishDate;
                            break;
                    }
                }
                
                if (!string.IsNullOrEmpty(search))
                {
                    filter = filter.AndAlso(x => x.Title.Contains(search));
                }
                if (startDate != null)
                {
                    filter = filter.AndAlso(x => x.PublishDate.Date >= startDate.Value.Date);
                }
                if (endDate != null)
                {
                    filter = filter.AndAlso(x => x.PublishDate.Date <= endDate.Value.Date);
                }
                var result = new List<Blog>();
                if (userRoles != null && (userRoles.Contains(SD.MANAGER_ROLE) || userRoles.Contains(SD.STAFF_ROLE)))
                {
                    var (count, list) = await _blogRepository.GetAllAsync(filter, "Author", pageSize, pageNumber, orderByQuery, isDesc);
                    result = list;
                    total = count;
                }
                else
                {
                    filter = filter.AndAlso(x => x.IsPublished);
                    var (count, list) = await _blogRepository.GetAllAsync(filter, "Author", pageSize, pageNumber, orderByQuery, isDesc);
                    result = list;
                    total = count;
                }
                Pagination pagination = new() { PageNumber = pageNumber, PageSize = pageSize, Total = total };
                _response.IsSuccess = true;
                _response.Result = _mapper.Map<List<BlogDTO>>(result);
                _response.Pagination = pagination;
                _response.StatusCode = HttpStatusCode.OK;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving blogs.");
                _response.IsSuccess = false;
                _logger.LogError("Error occurred while get an blog: {Message}", ex.Message);
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<APIResponse>> GetByIdAsync(int id)
        {
            try
            {
                var userRoles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();
                Blog blog = null;
                if (userRoles != null && (userRoles.Contains(SD.MANAGER_ROLE) || userRoles.Contains(SD.STAFF_ROLE)))
                {
                    blog = await _blogRepository.GetAsync(x => x.Id == id, true, "Author");

                }
                else
                {
                    blog = await _blogRepository.GetAsync(x => x.Id == id && x.IsPublished, true, "Author");
                }

                if (blog == null)
                {
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.NOT_FOUND_MESSAGE, SD.BLOG) };
                    return BadRequest(_response);
                }
                blog.ViewCount += 1;
                await _blogRepository.UpdateAsync(blog);
                _response.Result = _mapper.Map<BlogDTO>(blog);
                _response.StatusCode = HttpStatusCode.OK;
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }


        [HttpPut("{id}")]
        [Authorize(Roles = $"{SD.STAFF_ROLE},{SD.MANAGER_ROLE}")]
        public async Task<ActionResult<APIResponse>> DeleteAsync(int id, UpdateActiveDTO updateActiveDTO)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            try
            {
                if (id == 0 || id != updateActiveDTO.Id)
                {
                    _logger.LogWarning("Invalid blog ID: {Id}. Returning BadRequest.", id);
                    _response.StatusCode = HttpStatusCode.Unauthorized;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.ID) };
                    return BadRequest(_response);
                }
                var model = await _blogRepository.GetAsync(x => x.Id == id, true, "Author", null);
                if (model == null)
                {
                    _logger.LogWarning("Blog not found for ID: {id} and User ID: {userId}. Returning BadRequest.", id, userId);
                    _response.StatusCode = HttpStatusCode.BadRequest;
                    _response.IsSuccess = false;
                    _response.ErrorMessages = new List<string> { _resourceService.GetString(SD.BAD_REQUEST_MESSAGE, SD.CERTIFICATE) };
                    return BadRequest(_response);
                }
                model.IsPublished = updateActiveDTO.IsActive;
                if(updateActiveDTO.IsActive) model.PublishDate = DateTime.Now;
                model.UpdatedDate = DateTime.Now;
                await _blogRepository.UpdateAsync(model);
                _response.StatusCode = HttpStatusCode.NoContent;
                _response.Result = _mapper.Map<BlogDTO>(model);
                _response.IsSuccess = true;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating blog status ID: {id}", id);
                _response.IsSuccess = false;
                _response.StatusCode = HttpStatusCode.InternalServerError;
                _response.ErrorMessages = new List<string>() { _resourceService.GetString(SD.INTERNAL_SERVER_ERROR_MESSAGE) };
                return StatusCode((int)HttpStatusCode.InternalServerError, _response);
            }
        }

    }
}
