using Application.Interfaces;

using Domain.Entities;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Infrastructure.Repositories
{
    public class ClienteRepository : IClienteRepository
    {
        // Lista estática: los datos se comparten durante
        // toda la ejecución del microservicio.
        private static readonly List<Cliente> _clientes =
        [
            new Cliente(1, "Ana Torres", "ana@ejemplo.com"),
            new Cliente(2, "Luis Pérez", "luis@ejemplo.com"),
            new Cliente(3, "María López", "maria@ejemplo.com")
        ];

        // Protege la lista frente a operaciones simultáneas.
        private static readonly object _lock = new();

        // Permite generar identificadores consecutivos.
        private static int _ultimoId = 3;

        // Obtiene una copia de todos los clientes.
        public List<Cliente> ObtenerTodos()
        {
            lock (_lock)
            {
                return _clientes
                    .Select(Copiar)
                    .ToList();
            }
        }

        // Busca un cliente mediante su identificador.
        public Cliente? ObtenerPorId(int id)
        {
            lock (_lock)
            {
                var cliente = _clientes
                    .FirstOrDefault(c => c.Id == id);

                // Devolvemos una copia para proteger
                // los datos almacenados.
                return cliente is null
                    ? null
                    : Copiar(cliente);
            }
        }

        // Registra un nuevo cliente en la lista.
        public Cliente Agregar(Cliente cliente)
        {
            lock (_lock)
            {
                // Generamos el siguiente identificador.
                _ultimoId++;

                // Creamos la entidad con el ID definitivo.
                var nuevo = new Cliente(
                    _ultimoId,
                    cliente.Nombre,
                    cliente.Email);

                // Incorporamos el cliente a la lista.
                _clientes.Add(nuevo);

                return Copiar(nuevo);
            }
        }

        // Actualiza un cliente previamente registrado.
        public bool Actualizar(Cliente cliente)
        {
            lock (_lock)
            {
                // Localizamos su posición dentro de la lista.
                int posicion = _clientes.FindIndex(
                    c => c.Id == cliente.Id);

                // Si no existe, no podemos actualizarlo.
                if (posicion == -1)
                    return false;

                // Sustituimos el registro por los nuevos datos.
                _clientes[posicion] = Copiar(cliente);

                return true;
            }
        }

        // Elimina un cliente mediante su identificador.
        public bool Eliminar(int id)
        {
            lock (_lock)
            {
                var cliente = _clientes
                    .FirstOrDefault(c => c.Id == id);

                if (cliente is null)
                    return false;

                return _clientes.Remove(cliente);
            }
        }

        // Ejercicio Propuesto 01: desactiva un cliente sin eliminarlo.
        public bool Desactivar(int id)
        {
            lock (_lock)
            {
                var cliente = _clientes
                    .FirstOrDefault(c => c.Id == id);

                if (cliente is null)
                    return false;

                cliente.Desactivar();

                return true;
            }
        }

        // Extensión del Ejercicio Propuesto 01: revierte la desactivación.
        public bool Activar(int id)
        {
            lock (_lock)
            {
                var cliente = _clientes
                    .FirstOrDefault(c => c.Id == id);

                if (cliente is null)
                    return false;

                cliente.Activar();

                return true;
            }
        }

        // Ejercicio Propuesto 02: busca clientes por coincidencia parcial de nombre,
        // sin distinguir mayúsculas/minúsculas.
        public List<Cliente> BuscarPorNombre(string nombre)
        {
            lock (_lock)
            {
                return _clientes
                    .Where(c => c.Nombre.Contains(nombre, StringComparison.OrdinalIgnoreCase))
                    .Select(Copiar)
                    .ToList();
            }
        }

        // Crea una copia independiente de una entidad.
        // Evita exponer directamente los objetos almacenados.
        // Conserva el estado Activo (Ejercicio Propuesto 01).
        private static Cliente Copiar(Cliente cliente)
        {
            return Cliente.Reconstruir(
                cliente.Id,
                cliente.Nombre,
                cliente.Email,
                cliente.Activo);
        }
    }
}
