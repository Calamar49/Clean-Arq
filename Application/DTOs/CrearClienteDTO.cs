using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Application.DTOs
{
    // Define los datos que recibimos para crear o actualizar
    // un cliente, sin exponer directamente la entidad Domain.
    public class CrearClienteDTO
    {
        // El nombre es obligatorio y admite hasta 100 caracteres.
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        // Validamos que se proporcione un correo con formato válido.
        [Required(ErrorMessage = "El correo es obligatorio.")]
        [EmailAddress(ErrorMessage = "El correo no es válido.")]
        public string Email { get; set; } = string.Empty;
    }
}
