using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeliculasAPI.DTOs;
using PeliculasAPI.Entidades;
using PeliculasAPI.Helpers;
using PeliculasAPI.Servicios;
using System.Linq.Dynamic.Core;

namespace PeliculasAPI.Controllers
{
    [ApiController]
    [Route("api/peliculas")]
    public class PeliculasController : CustomBaseController
    {
        private readonly ApplicationDBContext context;
        private readonly IMapper mapper;
        private readonly IAlmacenadorArchivos almacenadorArchivos;
        private readonly ILogger<PeliculasController> logger;
        private readonly string contenedor = "peliculas";

        public PeliculasController(ApplicationDBContext context, IMapper mapper, IAlmacenadorArchivos almacenadorArchivos, ILogger<PeliculasController> logger) : base (context, mapper)
        {
            this.context = context;
            this.mapper = mapper;
            this.almacenadorArchivos = almacenadorArchivos;
            this.logger = logger;
        }

        //Lógica personalizada, no se globaliza
        [HttpGet]
        public async Task<ActionResult<PeliculasIndexDTO>> Get() //metodo GET para separar las peliculas que estan en el cine y cuales son las que se van a estrenar
        {
            var top = 5;
            var hoy = DateTime.Today;

            var proximosExtrenos = await context.Peliculas.Where(x => x.FechaEstreno > hoy)
                .OrderBy(x => x.FechaEstreno)
                .Take(top).ToListAsync();

            var enCines = await context.Peliculas.Where(x => x.EnCines).Take(top).ToListAsync();

            var resultado = new PeliculasIndexDTO();
            resultado.FuturosEstrenos = mapper.Map<List<PeliculaDTO>>(proximosExtrenos);
            resultado.EnCines = mapper.Map<List<PeliculaDTO>>(enCines);
            return resultado;
        }

        //Lógica personalizada, no se globaliza
        [HttpGet("filtro")]
        public async Task<ActionResult<List<PeliculaDTO>>> Filtrar([FromQuery] FiltroPeliculasDTO filtroPeliculasDTO)
        {
            var peliculasQueryable = context.Peliculas.AsQueryable();

            if(!string.IsNullOrEmpty(filtroPeliculasDTO.Titulo))
            {
                peliculasQueryable = peliculasQueryable.Where(x => x.Titulo.Contains(filtroPeliculasDTO.Titulo));
            }
            if (filtroPeliculasDTO.EnCines)
            {
                peliculasQueryable = peliculasQueryable.Where(x => x.EnCines);
            }
            if (filtroPeliculasDTO.ProximosEstrenos)
            {
                var hoy = DateTime.Today;
                peliculasQueryable = peliculasQueryable.Where(x => x.FechaEstreno > hoy);
            }

            if(filtroPeliculasDTO.GeneroId != 0)
            {
                peliculasQueryable = peliculasQueryable.Where(x => x.PeliculasGeneros.Select(y => y.GeneroId).Contains(filtroPeliculasDTO.GeneroId));
            }

            //Instalamos la librería System.Linq.Dynamic.Core para que nos permita usar strings para realizar el ordenamiento (hay que poner el using antes)
            if (!string.IsNullOrEmpty(filtroPeliculasDTO.CampoOrdenar))
            {
                //Si OrdenAscendente es True será ascending y si es False será descending (Cuando pones la variable en la peticion)
                var tipoOrden = filtroPeliculasDTO.OrdenAscendente ? "ascending" : "descending";

                try //si por algun casual en la peticion ponen algo que no está registrado no salte el error y se vaya por el logger
                {
                    peliculasQueryable = peliculasQueryable.OrderBy($"{filtroPeliculasDTO.CampoOrdenar} {tipoOrden}");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex.Message, ex);    
                }
            }

            await HttpContext.InsertarParametrosPaginacion(peliculasQueryable, filtroPeliculasDTO.CantidadRegistrosPorPagina);

            var peliculas = await peliculasQueryable.Paginar(filtroPeliculasDTO.Paginacion).ToListAsync();

            return mapper.Map<List<PeliculaDTO>>(peliculas);

        }

        //Lógica personalizada, no se globaliza
        [HttpGet("{id}", Name = "obtenerPelicula")]
        public async Task<ActionResult<PeliculaDetallesDTO>> Get(int id)
        {
            var pelicula = await context.Peliculas
                .Include(x => x.PeliculasActores).ThenInclude(x => x.Actor)
                .Include(x => x.PeliculasGeneros).ThenInclude(x => x.Genero)
                .FirstOrDefaultAsync(x => x.Id == id);    
            if(pelicula == null)
            {
                return NotFound();
            }
            pelicula.PeliculasActores = pelicula.PeliculasActores.OrderBy(x => x.Orden).ToList();
            return mapper.Map<PeliculaDetallesDTO>(pelicula);
        }

        //Lógica personalizada, no se globaliza
        [HttpPost]
        public async Task<ActionResult> Post([FromForm]PeliculaCreacionDTO peliculaCreacionDTO)
        {
            var pelicula = mapper.Map<Pelicula>(peliculaCreacionDTO);
            
            if (peliculaCreacionDTO.Poster != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await peliculaCreacionDTO.Poster.CopyToAsync(memoryStream);
                    var contenido = memoryStream.ToArray();
                    var extension = Path.GetExtension(peliculaCreacionDTO.Poster.FileName);
                    pelicula.Poster = await almacenadorArchivos.GuardarArchivo(contenido, extension, contenedor, peliculaCreacionDTO.Poster.ContentType);
                }
            }

            AsignarOrdenActores(pelicula);

            context.Add(pelicula);
            await context.SaveChangesAsync();

            var peliculaDTO = mapper.Map<PeliculaDTO>(pelicula);
            return new CreatedAtRouteResult("obtenerPelicula", new {id = pelicula.Id}, peliculaDTO);
            
        }

        private void AsignarOrdenActores(Pelicula pelicula)
        {
            if(pelicula.PeliculasActores != null)
            {
                for (int i = 0; i < pelicula.PeliculasActores.Count; i++) 
                {
                    pelicula.PeliculasActores[i].Orden = i;
                }
            }
        }

        //Lógica personalizada, no se globaliza
        [HttpPut("{id}")]
        public async Task<ActionResult> Put(int id, [FromForm] PeliculaCreacionDTO peliculaCreacionDTO)
        {
            var peliculaDB = await context.Peliculas
                .Include(x => x.PeliculasActores)
                .Include(x => x.PeliculasGeneros)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (peliculaDB == null)
            {
                return NotFound();
            }

            peliculaDB = mapper.Map(peliculaCreacionDTO, peliculaDB);

            if (peliculaCreacionDTO.Poster != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await peliculaCreacionDTO.Poster.CopyToAsync(memoryStream);
                    var contenido = memoryStream.ToArray();
                    var extension = Path.GetExtension(peliculaCreacionDTO.Poster.FileName);
                    peliculaDB.Poster = await almacenadorArchivos.EditarArchivo(contenido, extension, contenedor, peliculaDB.Poster, peliculaCreacionDTO.Poster.ContentType);
                }
            }
            AsignarOrdenActores(peliculaDB);

            await context.SaveChangesAsync();
            return NoContent();
        }

        //Generalizado desde CustomBasecontroller
        [HttpPatch("{id}")]
        public async Task<ActionResult> Patch(int id, [FromBody] JsonPatchDocument<PeliculaPatchDTO> patchDocument)
        {
            return await Patch<Pelicula,PeliculaPatchDTO>(id, patchDocument);
        }

        //Generalizado desde CustomBasecontroller
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            return await Delete<Pelicula>(id);
        }
    }
}
