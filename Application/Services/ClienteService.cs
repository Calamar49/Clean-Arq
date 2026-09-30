using Application.DTOs;
using Application.Interfaces;

using Domain.Entities;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Application.Services
{
    // Coordina los casos de uso del microservicio.
    // No conoce cómo se almacenan físicamente los clientes.
    public class ClienteService : IClienteService
    {
        // Dependemos de una interfaz, no de ClienteRepository.
        private readonly IClienteRepository _repository;

        // El repositorio se recibe por inyección de dependencias.
        public ClienteService(IClienteRepository repository)
        {
            _repository = repository;
        }

        // Caso de uso: obtener todos los clientes.
        public List<ClienteDTO> ObtenerTodos()
        {
            // Solicitamos los datos al repositorio.
            var clientes = _repository.ObtenerTodos();

            // Convertimos las entidades en DTOs de respuesta.
            return clientes.Select(Mapear).ToList();
        }

        // Caso de uso: consultar un cliente.
        public ClienteDTO? ObtenerPorId(int id)
        {
            var cliente = _repository.ObtenerPorId(id);

            // Si no existe, devolvemos null.
            if (cliente is null)
                return null;

            return Mapear(cliente);
        }

        // Caso de uso: registrar un cliente.
        public ClienteDTO Crear(CrearClienteDTO dto)
        {
            // Creamos la entidad del dominio.
            // El ID definitivo será asignado por el repositorio.
            var cliente = new Cliente(
                0,
                dto.Nombre,
                dto.Email);

            // Solicitamos el registro mediante la interfaz.
            var registrado = _repository.Agregar(cliente);

            // Devolvemos únicamente los datos del contrato.
            return Mapear(registrado);
        }

        // Caso de uso: actualizar un cliente.
        public bool Actualizar(int id, CrearClienteDTO dto)
        {
            // Primero comprobamos que exista.
            var cliente = _repository.ObtenerPorId(id);

            if (cliente is null)
                return false;

            // Aplicamos las reglas definidas en Domain.
            cliente.ActualizarDatos(dto.Nombre, dto.Email);

            // Solicitamos que se guarden los cambios.
            return _repository.Actualizar(cliente);
        }

        // Caso de uso: eliminar un cliente.
        public bool Eliminar(int id)
        {
            return _repository.Eliminar(id);
        }

        // Ejercicio Propuesto 01: desactivar un cliente sin eliminarlo.
        public bool Desactivar(int id)
        {
            return _repository.Desactivar(id);
        }

        // Extensión del Ejercicio Propuesto 01: revertir la desactivación.
        public bool Activar(int id)
        {
            return _repository.Activar(id);
        }

        // Ejercicio Propuesto 02: buscar clientes por coincidencia parcial de nombre.
        public List<ClienteDTO> BuscarPorNombre(string nombre)
        {
            var clientes = _repository.BuscarPorNombre(nombre);

            return clientes.Select(Mapear).ToList();
        }

        // Método privado para convertir entidades a DTOs.
        // Centraliza el mapeo y evita duplicar código.
        private static ClienteDTO Mapear(Cliente cliente)
        {
            return new ClienteDTO
            {
                Id = cliente.Id,
                Nombre = cliente.Nombre,
                Email = cliente.Email,
                Activo = cliente.Activo
            };
        }
    }
}
