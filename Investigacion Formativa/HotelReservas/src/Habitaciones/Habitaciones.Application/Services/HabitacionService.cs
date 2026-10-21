using Habitaciones.Application.DTOs;
using Habitaciones.Application.Exceptions;
using Habitaciones.Application.Interfaces;
using Habitaciones.Domain.Entities;

namespace Habitaciones.Application.Services
{
    // Coordina los casos de uso del servicio de habitaciones.
    // Depende de la interfaz del repositorio, no de EF Core ni de MySQL.
    public class HabitacionService : IHabitacionService
    {
        private const int MaximoNoches = 30;

        private readonly IHabitacionRepository _repository;

        public HabitacionService(IHabitacionRepository repository)
        {
            _repository = repository;
        }

        public async Task<List<HabitacionDTO>> ObtenerTodasAsync()
        {
            var habitaciones = await _repository.ListarAsync();
            return habitaciones.Select(Mapear).ToList();
        }

        public async Task<HabitacionDTO> ObtenerPorIdAsync(int id)
        {
            var habitacion = await _repository.ObtenerAsync(id)
                ?? throw new NoEncontradoException("Habitación no encontrada.");

            return Mapear(habitacion);
        }

        public async Task<HabitacionDTO> CrearAsync(CrearHabitacionDTO dto)
        {
            if (await _repository.ExisteNumeroAsync(dto.Numero.Trim()))
                throw new ConflictoException($"Ya existe la habitación {dto.Numero.Trim()}.");

            var habitacion = new Habitacion(
                dto.Numero, dto.Tipo, dto.Capacidad, dto.PrecioPorNoche);

            await _repository.AgregarAsync(habitacion);

            return Mapear(habitacion);
        }

        public async Task<HabitacionDTO> ActualizarAsync(int id, ActualizarHabitacionDTO dto)
        {
            var habitacion = await _repository.ObtenerAsync(id)
                ?? throw new NoEncontradoException("Habitación no encontrada.");

            habitacion.Actualizar(dto.Tipo, dto.Capacidad, dto.PrecioPorNoche, dto.Activa);
            await _repository.GuardarCambiosAsync();

            return Mapear(habitacion);
        }

        public async Task<List<HabitacionDTO>> ObtenerDisponiblesAsync(
            DateOnly entrada, DateOnly salida, int? capacidad)
        {
            ValidarRango(entrada, salida);

            var habitaciones = await _repository.ListarDisponiblesAsync(entrada, salida, capacidad);
            return habitaciones.Select(Mapear).ToList();
        }

        public async Task<ResultadoBloqueo> BloquearNochesAsync(BloquearNochesDTO dto)
        {
            if (dto.ReservaId == Guid.Empty)
                throw new ReglaNegocioException("La reserva es obligatoria.");

            ValidarRango(dto.FechaEntrada, dto.FechaSalida);

            var habitacion = await _repository.ObtenerAsync(dto.HabitacionId)
                ?? throw new NoEncontradoException("Habitación no encontrada.");

            if (!habitacion.Activa)
                throw new ReglaNegocioException("La habitación no está disponible para reservas.");

            // El repositorio garantiza la atomicidad y la ausencia de overbooking.
            return await _repository.BloquearNochesAsync(
                dto.ReservaId, dto.HabitacionId, dto.FechaEntrada, dto.FechaSalida);
        }

        public Task<int> LiberarReservaAsync(Guid reservaId)
        {
            if (reservaId == Guid.Empty)
                throw new ReglaNegocioException("La reserva es obligatoria.");

            return _repository.LiberarReservaAsync(reservaId);
        }

        private static void ValidarRango(DateOnly entrada, DateOnly salida)
        {
            if (salida <= entrada)
                throw new ReglaNegocioException("La fecha de salida debe ser posterior a la de entrada.");

            if (entrada < DateOnly.FromDateTime(DateTime.Today))
                throw new ReglaNegocioException("La fecha de entrada no puede estar en el pasado.");

            if (salida.DayNumber - entrada.DayNumber > MaximoNoches)
                throw new ReglaNegocioException($"La estadía no puede superar las {MaximoNoches} noches.");
        }

        private static HabitacionDTO Mapear(Habitacion habitacion)
        {
            return new HabitacionDTO
            {
                Id = habitacion.Id,
                Numero = habitacion.Numero,
                Tipo = habitacion.Tipo,
                Capacidad = habitacion.Capacidad,
                PrecioPorNoche = habitacion.PrecioPorNoche,
                Activa = habitacion.Activa
            };
        }
    }
}
