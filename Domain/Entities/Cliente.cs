using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities
{
    // Representa a un cliente dentro del negocio.
    // No depende de controladores, servicios HTTP ni repositorios.
    public class Cliente
    {
        // Identificador único del cliente.
        // Solo puede establecerse al crear la entidad.
        public int Id { get; private set; }

        // Datos del cliente protegidos contra cambios directos.
        public string Nombre { get; private set; }
        public string Email { get; private set; }

        // Ejercicio Propuesto 01: indica si el cliente sigue activo
        // en el sistema, sin necesidad de eliminar su registro.
        public bool Activo { get; private set; }

        // Constructor: crea un cliente y valida sus datos.
        public Cliente(int id, string nombre, string email)
        {
            if (id < 0)
                throw new ArgumentException("El identificador no puede ser negativo.");

            Id = id;

            // Inicialización de las propiedades.
            Nombre = string.Empty;
            Email = string.Empty;

            // Todo cliente nuevo se crea activo.
            Activo = true;

            // Reutilizamos las reglas de validación.
            ActualizarDatos(nombre, email);
        }

        // Modifica los datos respetando las reglas del negocio.
        public void ActualizarDatos(string nombre, string email)
        {
            // El nombre no puede estar vacío.
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre es obligatorio.");

            // El correo tampoco puede estar vacío.
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("El correo es obligatorio.");

            // Trim elimina espacios innecesarios.
            Nombre = nombre.Trim();
            Email = email.Trim();
        }

        // Ejercicio Propuesto 01: desactiva al cliente sin eliminar su registro.
        public void Desactivar()
        {
            Activo = false;
        }

        // Extensión del Ejercicio Propuesto 01: permite revertir la desactivación.
        public void Activar()
        {
            Activo = true;
        }

        // Permite reconstruir un cliente conservando su estado Activo,
        // por ejemplo al copiarlo dentro del repositorio.
        public static Cliente Reconstruir(int id, string nombre, string email, bool activo)
        {
            var cliente = new Cliente(id, nombre, email);

            if (!activo)
                cliente.Desactivar();

            return cliente;
        }
    }
}
