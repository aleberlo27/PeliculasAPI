﻿using System.ComponentModel.DataAnnotations;

namespace PeliculasAPI.DTOs
{
    //DTO DE CREACION 
    public class GeneroCreacionDTO
    {
        [Required]
        [StringLength(40)]
        public string Nombre { get; set; }
    }
}
