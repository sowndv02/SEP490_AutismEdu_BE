using AutoMapper;
using backend_api.Models;
using backend_api.Repository;
using backend_api.Repository.IRepository;
using backend_api.Services.IServices;
using backend_api.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace backend_api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiVersionNeutral]
    public class PacketPaymentController : ControllerBase
    {
        private readonly IPacketPaymentRepository _packetPaymentRepository;
        private readonly IMapper _mapper;
        protected APIResponse _response;
        protected int pageSize = 0;
        private readonly IResourceService _resourceService;
        public PacketPaymentController(IPacketPaymentRepository packetPaymentRepository,
            IConfiguration configuration, IMapper mapper, IResourceService resourceService)
        {
            pageSize = int.Parse(configuration["APIConfig:PageSize"]);
            _response = new APIResponse();
            _mapper = mapper;
            _packetPaymentRepository = packetPaymentRepository;
            _resourceService = resourceService;
        }



    }
}
