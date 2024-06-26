using PeliculasAPI.Validaciones;
using System.ComponentModel.DataAnnotations;

namespace PeliculasAPI.DTOs
{
    public class ActorCreacionDTO : ActorPatchDTO
    {
        [PesoArchivoValidacion(pesoMaximoEnMegaBytes: 4)]
        [TipoArchivoValidacion(grupoTipoArchivo: GrupoTipoArchivo.Imagen)]
        public IFormFile Foto { get; set; } //en AutorCreacionDTO ponemos IFormFile para que coja la foto y en ActorDTO ponemos el string de la URL
    }
}
