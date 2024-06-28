using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeliculasAPI.DTOs;
using PeliculasAPI.Entidades;
using PeliculasAPI.Helpers;
using PeliculasAPI.Migrations;
using System.Security.Claims;

namespace PeliculasAPI.Controllers
{
    [Route("api/peliculas/{peliculaId:int}/reviews")]
    [ServiceFilter(typeof(PeliculaExisteAttribute))]
    public class ReviewController : CustomBaseController
    {
        private readonly ApplicationDBContext context;
        private readonly IMapper mapper;

        public ReviewController(ApplicationDBContext context, IMapper mapper) :base (context,mapper)
        {
            this.context = context;
            this.mapper = mapper;
        }

        [HttpGet]
        public async Task<ActionResult<List<ReviewDTO>>> Get(int peliculaId, [FromQuery] PaginacionDTO paginacionDTO)
        {
            /*
             * Comprobamos si existe la película para poder retornar el review de la misma
             * LO COMENTO (para ver como se haría solo con 1 método) PORQUE CREAMOS UN SERVICIO PARA NO REPETIR CODIGO, ESTÁ ARRIBA DEL TODO LLAMADO: PeliculaExisteAttribute
             * 
                var existePelicula = await context.Peliculas.AnyAsync(x => x.Id == peliculaId);

                if (!existePelicula)
                {
                    return NotFound();
                }

            */
            var queryable = context.Reviews.Include(x => x.Usuario).AsQueryable();  
            queryable = queryable.Where(x => x.PeliculaId == peliculaId);
            return await Get<Review, ReviewDTO>(paginacionDTO, queryable);
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult> Post (int peliculaId, [FromBody] ReviewCreacionDTO reviewCreacionDTO)
        {
            
            var usuarioId = HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value;

            var reviewExiste = await context.Reviews.AnyAsync(x => x.Id == peliculaId && x.UsuarioId == usuarioId);
            if (reviewExiste)
            {
                return BadRequest("El usuario ya ha escrito un review de esta película");

            }

            var review = mapper.Map<Review> (reviewCreacionDTO);
            review.PeliculaId = peliculaId;
            review.UsuarioId = usuarioId;

            context.Add(review);
            await context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPut("{reviewId:int}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult> Put(int peliculaId, int reviewId, [FromBody] ReviewCreacionDTO reviewCreacionDTO)
        {
            var reviewDB = await context.Reviews.FirstOrDefaultAsync(x => x.Id == reviewId);
            if ( reviewDB == null)
            {
                return NotFound();   
            }

            var usuarioId = HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value;

            //Si el usuario actual no es el usuario que escribió el review no le vamos a dejar actualizar el review
            if(reviewDB.UsuarioId != usuarioId)
            {
                return BadRequest("No tienes permisos de editar este review.");
            }

            reviewDB = mapper.Map(reviewCreacionDTO, reviewDB);

            await context.SaveChangesAsync();   
            return NoContent();

        }

        [HttpDelete("{reviewId:int}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<ActionResult> Delete(int reviewId)
        {
            //Comprobamos si existe el review que ha seleccionado
            var reviewDB = await context.Reviews.FirstOrDefaultAsync(x => x.Id == reviewId);
            if (reviewDB == null)
            {
                return NotFound();
            }

            //Comprobamos si el usuario que quiere borrar la review es el mismo que ha escrito la review
            var usuarioId = HttpContext.User.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier).Value;
            if (reviewDB.UsuarioId != usuarioId)
            {
                return Forbid();
            }

            context.Remove(reviewDB);
            await context.SaveChangesAsync();
            return NoContent();
        }

    }
}
