namespace Api.Controllers
{
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;

    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get()
        {

            return Ok();
        }

        [HttpPost("{value}")]
        public async Task PostAsync([FromBody] string value)
        {
            
        }
    }
}