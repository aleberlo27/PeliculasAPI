﻿using System.ComponentModel.DataAnnotations;

namespace PeliculasAPI.DTOs
{
    public class SalaDeCineDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public double Latitud { get; set; }
        public double Longitud { get; set;}
    }
}
