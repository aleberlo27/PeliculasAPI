﻿using AutoMapper;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PeliculasAPI.DTOs;
using PeliculasAPI.Entidades;
using PeliculasAPI.Helpers;

namespace PeliculasAPI.Controllers
{
    /*
     * Para controladores que se centran en CRUDS, es decir, en objetos simples (leer, crear, actualizar y borrar) 
     * esta clase es muy util para reutilizar código.
     */
    public class CustomBaseController : ControllerBase
    {
        private readonly ApplicationDBContext context;
        private readonly IMapper mapper;

        public CustomBaseController(ApplicationDBContext context, IMapper mapper)
        {
            this.context = context;
            this.mapper = mapper;
        }

        protected async Task<List<TDTO>> Get<TEntidad, TDTO>() where TEntidad : class
        {
            var entidades = await context.Set<TEntidad>().AsNoTracking().ToListAsync();
            var dto = mapper.Map<List<TDTO>>(entidades);
            return dto;
        }

        //Reutilización del código de PAGINACIÓN
        protected async Task<List<TDTO>> Get<TEntidad, TDTO>(PaginacionDTO paginacionDTO) where TEntidad : class
        {
            var queryable = context.Set<TEntidad>().AsQueryable();
            return await Get<TEntidad,TDTO>(paginacionDTO,queryable); //En este método reutilizamos código del de abajo
        }

        protected async Task<List<TDTO>> Get<TEntidad, TDTO>(PaginacionDTO paginacionDTO, IQueryable<TEntidad> queryable) where TEntidad : class
        {
            await HttpContext.InsertarParametrosPaginacion(queryable, paginacionDTO.CantidadRegistrosPorPagina);

            var entidades = await queryable.Paginar(paginacionDTO).ToListAsync();
            return mapper.Map<List<TDTO>>(entidades);
        }


        protected async Task<ActionResult<TDTO>> Get<TEntidad, TDTO>(int id) where TEntidad : class, IId
        {
            var entidad = await context.Set<TEntidad>().AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);   
            if (entidad == null)
            {
                return NotFound();
            }
            return mapper.Map<TDTO>(entidad);
        }

        protected async Task<ActionResult> Post<TCreacion, TEntidad, TLectura>(TCreacion creacionDTO, string nombreRuta) where TEntidad : class, IId
        {
            var entidad = mapper.Map<TEntidad>(creacionDTO);
            context.Add(entidad);
            await context.SaveChangesAsync();
            var dtoLectura = mapper.Map<TLectura>(entidad);

            return new CreatedAtRouteResult(nombreRuta, new { id = entidad.Id }, dtoLectura);
        }

        protected async Task<ActionResult> Put<TCreacion, TEntidad>(int id, TCreacion creacionDTO) where TEntidad : class, IId
        {
            var entidad = mapper.Map<TEntidad>(creacionDTO);
            entidad.Id = id;
            context.Entry(entidad).State = EntityState.Modified;
            await context.SaveChangesAsync();
            return NoContent();
        }

        protected async Task<ActionResult> Patch<TEntidad, TDTO>(int id, JsonPatchDocument<TDTO> patchDocument) 
            where TDTO : class
            where TEntidad : class, IId
        {
            if (patchDocument == null)
            {
                return BadRequest();
            }
            var entidadDB = await context.Set<TEntidad>().FirstOrDefaultAsync(x => x.Id == id);

            if (entidadDB == null)
            {
                return NotFound();
            }

            var entidadDTO = mapper.Map<TDTO>(entidadDB);

            patchDocument.ApplyTo(entidadDTO, ModelState);

            var esValido = TryValidateModel(entidadDTO);
            if (!esValido)
            {
                return BadRequest(ModelState);
            }

            mapper.Map(entidadDTO, entidadDB);

            await context.SaveChangesAsync();

            return NoContent();
        }

        protected async Task<ActionResult> Delete<TEntidad>(int id) where TEntidad : class, IId, new() 
        {
            var existe = await context.Set<TEntidad>().AnyAsync(x => x.Id == id);

            if (!existe)
            {
                return NotFound();
            }

            context.Remove(new TEntidad() { Id = id });

            await context.SaveChangesAsync();
            return NoContent();
        }
    }
}
