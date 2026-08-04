using Microsoft.AspNetCore.Mvc;

namespace ABP.WebApi.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class BaseApiController : ControllerBase
    {
        
    }
}