using AutoMapper;
using Azure;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeliculasAPI.DTOs;
using PeliculasAPI.Entidades;
using PeliculasAPI.Helpers;
using PeliculasAPI.Servicios;

namespace PeliculasAPI.Controllers
{
    [ApiController]
    [Route("api/actores")]
    public class ActoresController : CustomBaseController
    {
        private readonly ApplicationDBContext context;
        private readonly IMapper mapper;
        private readonly IAlmacenadorArchivos almacenadorArchivos;
        private readonly string contenedor = "actores";

        public ActoresController(ApplicationDBContext context, IMapper mapper, IAlmacenadorArchivos almacenadorArchivos) : base (context, mapper)
        {
            this.context = context;
            this.mapper = mapper;
            this.almacenadorArchivos = almacenadorArchivos;
        }

        [HttpGet]
        public async Task<ActionResult<List<ActorDTO>>> Get([FromQuery] PaginacionDTO paginacionDTO)
        {
            return await Get<Actor,ActorDTO> (paginacionDTO);

        }

        [HttpGet("{id}", Name = "obtenerActor")]
        public async Task<ActionResult<ActorDTO>> Get(int id)
        {
            return await Get<Actor,ActorDTO>(id);
        }

        //Como el método Post es más complejo y tiene una lógica personal no podemos generalizarlo, lo dejamos como está
        [HttpPost]
        public async Task<ActionResult> Post([FromForm] ActorCreacionDTO actorCreacionDTO)
        {
            var entidad = mapper.Map<Actor>(actorCreacionDTO);
            if(actorCreacionDTO.Foto != null)
            {
                using (var memoryStream = new  MemoryStream())
                {
                    await actorCreacionDTO.Foto.CopyToAsync(memoryStream);
                    var contenido = memoryStream.ToArray();
                    var extension = Path.GetExtension(actorCreacionDTO.Foto.FileName);
                    entidad.Foto = await almacenadorArchivos.GuardarArchivo(contenido, extension, contenedor, actorCreacionDTO.Foto.ContentType);
                }
            }
            context.Add(entidad);
            context.SaveChangesAsync();

            var dto = mapper.Map<ActorDTO>(entidad);

            return new CreatedAtRouteResult("obtenerActor", new {id = entidad.Id}, dto);
        }

        //Éste método igual que el anterior lo dejamos como está porque tiene una lógica más personal y no podemos generalizarlo
        [HttpPut("{id}")]
        public async Task<ActionResult> Put(int id, [FromForm] ActorCreacionDTO actorCreacionDTO)
        {
            var actorDB = await context.Actores.FirstOrDefaultAsync(x => x.Id == id);
            if (actorDB == null)
            {
                return NotFound();
            }

            actorDB = mapper.Map(actorCreacionDTO, actorDB);

            if (actorCreacionDTO.Foto != null)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await actorCreacionDTO.Foto.CopyToAsync(memoryStream);
                    var contenido = memoryStream.ToArray();
                    var extension = Path.GetExtension(actorCreacionDTO.Foto.FileName);
                    actorDB.Foto = await almacenadorArchivos.EditarArchivo(contenido, extension, contenedor,actorDB.Foto, actorCreacionDTO.Foto.ContentType);
                }
            }

            await context.SaveChangesAsync();
            return NoContent();
        }

        //Al ser un método más genérico lo llevamos al CustomBaseController y lo globalizamos
        [HttpPatch("{id}")]
        public async Task<ActionResult> Patch(int id, [FromBody] JsonPatchDocument<ActorPatchDTO> patchDocument)
        {
            return await Patch<Actor,ActorPatchDTO>(id, patchDocument);
        }
        
        //Igual que todos los genéricos
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            return await Delete<Actor>(id);
        }
    }
}

