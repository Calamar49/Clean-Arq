using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;

namespace Application.Interfaces
{
    // Contrato de las operaciones que ofrecerá Application.
    // Presentation utilizará esta interfaz.
    public interface IClienteService
    {
        // Devuelve todos los clientes.
        List<ClienteDTO> ObtenerTodos();

        // Consulta un cliente por su identificador.
        ClienteDTO? ObtenerPorId(int id);

        // Registra un nuevo cliente.
        ClienteDTO Crear(CrearClienteDTO dto);

        // Actualiza un cliente existente.
        bool Actualizar(int id, CrearClienteDTO dto);

        // Elimina un cliente.
        bool Eliminar(int id);

        // Ejercicio Propuesto 01: desactiva un cliente sin eliminarlo.
        bool Desactivar(int id);

        // Extensión del Ejercicio Propuesto 01: revierte la desactivación.
        bool Activar(int id);

        // Ejercicio Propuesto 02: busca clientes por coincidencia parcial de nombre.
        List<ClienteDTO> BuscarPorNombre(string nombre);
    }
}
