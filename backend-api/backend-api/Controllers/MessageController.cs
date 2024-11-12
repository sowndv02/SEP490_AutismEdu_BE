using Microsoft.AspNetCore.Mvc;

namespace backend_api.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    [ApiVersionNeutral]
    public class MessageController : ControllerBase
    {
    }
}
