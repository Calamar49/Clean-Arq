using System.Text.Json;
using Habitaciones.Application.DTOs;
using Habitaciones.Application.Exceptions;
using Habitaciones.Application.Interfaces;
using Hotel.Contracts;
using Hotel.Messaging;
using Microsoft.Extensions.Options;

namespace Habitaciones.Api.Consumers
{
    // Participa en la Saga bloqueando las noches solicitadas y liberándolas
    // cuando la reserva se rechaza o se cancela (compensación).
    public class HabitacionesConsumer : ConsumidorBase
    {
        private readonly IEventPublisher _eventos;

        public HabitacionesConsumer(
            RabbitMqConexion conexion,
            IServiceScopeFactory ambitos,
            IOptions<RabbitMqOptions> opciones,
            IEventPublisher eventos,
            ILogger<HabitacionesConsumer> log)
            : base(conexion, ambitos, opciones, log)
        {
            _eventos = eventos;
        }

        protected override string Servicio => "habitaciones";

        protected override string[] RoutingKeys =>
        [
            Hotel.Contracts.RoutingKeys.ReservaSolicitada,
            Hotel.Contracts.RoutingKeys.ReservaRechazada,
            Hotel.Contracts.RoutingKeys.ReservaCancelada
        ];

        protected override async Task ManejarAsync(
            string routingKey, string json, IServiceProvider servicios, CancellationToken ct)
        {
            var habitaciones = servicios.GetRequiredService<IHabitacionService>();

            switch (routingKey)
            {
                case Hotel.Contracts.RoutingKeys.ReservaSolicitada:
                    await IntentarBloquearAsync(habitaciones, Deserializar<ReservaSolicitada>(json), ct);
                    break;

                // Compensación: la reserva no prosperó, se devuelven las noches.
                case Hotel.Contracts.RoutingKeys.ReservaRechazada:
                    await habitaciones.LiberarReservaAsync(Deserializar<ReservaRechazada>(json).ReservaId);
                    break;

                // El huésped canceló: también se liberan las noches.
                case Hotel.Contracts.RoutingKeys.ReservaCancelada:
                    await habitaciones.LiberarReservaAsync(Deserializar<ReservaCancelada>(json).ReservaId);
                    break;
            }
        }

        private async Task IntentarBloquearAsync(
            IHabitacionService habitaciones, ReservaSolicitada evento, CancellationToken ct)
        {
            ResultadoBloqueo resultado;

            try
            {
                resultado = await habitaciones.BloquearNochesAsync(new BloquearNochesDTO
                {
                    ReservaId = evento.ReservaId,
                    HabitacionId = evento.HabitacionId,
                    FechaEntrada = evento.FechaEntrada,
                    FechaSalida = evento.FechaSalida
                });
            }
            catch (Exception ex) when (ex is ReglaNegocioException or NoEncontradoException)
            {
                // Datos inválidos o habitación inexistente: reintentar no ayudaría,
                // así que se informa el rechazo para que la Saga termine.
                await _eventos.PublicarAsync(
                    new HabitacionNoDisponible(evento.ReservaId, evento.HabitacionId, ex.Message), ct);
                return;
            }

            if (resultado == ResultadoBloqueo.Bloqueado)
            {
                await _eventos.PublicarAsync(
                    new HabitacionBloqueada(evento.ReservaId, evento.HabitacionId), ct);
            }
            else
            {
                await _eventos.PublicarAsync(
                    new HabitacionNoDisponible(
                        evento.ReservaId,
                        evento.HabitacionId,
                        "La habitación ya está reservada en alguna de las fechas solicitadas."), ct);
            }
        }

        private static T Deserializar<T>(string json) =>
            JsonSerializer.Deserialize<T>(json)
            ?? throw new EventoIgnorableException($"No se pudo interpretar el evento {typeof(T).Name}.");
    }
}
