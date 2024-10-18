using AutoMapper;
using backend_api.Models;
using backend_api.Models.DTOs.CreateDTOs;
using backend_api.Models.DTOs;
using backend_api.Repository;
using backend_api.Repository.IRepository;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace backend_api.Controllers.v1
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class ScheduleController : ControllerBase
    {
        
    }
}
