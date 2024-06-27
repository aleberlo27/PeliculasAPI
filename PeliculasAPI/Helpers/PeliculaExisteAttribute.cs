
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace PeliculasAPI.Helpers
{
    public class PeliculaExisteAttribute: Attribute, IAsyncResourceFilter
    {
        private readonly ApplicationDBContext dbContext;
        public PeliculaExisteAttribute(ApplicationDBContext context)
        {
            this.dbContext= context;
        }

        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var peliculaIdObject = context.HttpContext.Request.RouteValues["peliculaId"];
            if(peliculaIdObject == null )
            {
                return;
            }

            var peliculaId = int.Parse(peliculaIdObject.ToString());

            var existePelicula = await dbContext.Peliculas.AnyAsync(x => x.Id == peliculaId);

            if (!existePelicula)
            {
                context.Result = new NotFoundResult(); //Cortamos el ciclo de vida del HTTP 
            }
            else
            {
                await next(); //Seguimos el ciclo de vida de la peticion HTTP
            }
        }
    }
}
