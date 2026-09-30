using System;
using System.Collections.Generic;
using System.Text;

using Domain.Entities;

namespace Application.Interfaces
{
    // Contrato que define las operaciones de almacenamiento.
    // Application no necesita saber si los datos se guardan
    // en una lista, una base de datos o un servicio externo.
    public interface IClienteRepository
    {
        // Obtiene todos los clientes registrados.
        List<Cliente> ObtenerTodos();

        // Busca un cliente mediante su identificador.
        // Devuelve null cuando el cliente no existe.
        Cliente? ObtenerPorId(int id);

        // Registra un cliente y devuelve la entidad creada.
        Cliente Agregar(Cliente cliente);

        // Actualiza los datos de un cliente existente.
        // Devuelve false si el cliente no existe.
        bool Actualizar(Cliente cliente);

        // Elimina un cliente por su identificador.
        bool Eliminar(int id);

        // Ejercicio Propuesto 01: desactiva un cliente sin eliminarlo.
        // Devuelve false si el cliente no existe.
        bool Desactivar(int id);

        // Extensión del Ejercicio Propuesto 01: revierte la desactivación.
        bool Activar(int id);

        // Ejercicio Propuesto 02: busca clientes cuyo nombre contenga
        // el texto indicado (sin distinguir mayúsculas/minúsculas).
        List<Cliente> BuscarPorNombre(string nombre);
    }
}
