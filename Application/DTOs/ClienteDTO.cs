using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs
{
    // Define la información que devolveremos al consumidor
    // de la API después de consultar o registrar un cliente.
    public class ClienteDTO
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Ejercicio Propuesto 01: permite ver si el cliente sigue activo.
        public bool Activo { get; set; }
    }
}
