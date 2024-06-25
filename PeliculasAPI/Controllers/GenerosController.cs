using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace PeliculasAPI.Controllers
{
    [ApiController]
    [Route("api/generos")]
    public class GenerosController : ControllerBase
    {
        private readonly ApplicationDBContext context;

        public GenerosController(ApplicationDBContext context)
        {
            this.context = context;
        }

        [HttpGet]
        public async Task<ActionResult> Get()
        {
            return await context.Generos.ToListAsync();
        }

    }
}
