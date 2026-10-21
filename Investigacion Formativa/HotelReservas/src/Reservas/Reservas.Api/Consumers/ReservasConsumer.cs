using System.Text.Json;
using Hotel.Contracts;
using Hotel.Messaging;
using Microsoft.Extensions.Options;
using Reservas.Application.Exceptions;
using Reservas.Application.Interfaces;
using Reservas.Domain.Exceptions;

namespace Reservas.Api.Consumers
{
    // Escucha los eventos que hacen avanzar la Saga desde el punto de vista
    // de la reserva: el resultado del bloqueo de la habitación y el del pago.
    public class ReservasConsumer : ConsumidorBase
    {
        private readonly ILogger<ReservasConsumer> _log;

        public ReservasConsumer(
            RabbitMqConexion conexion,
            IServiceScopeFactory ambitos,
            IOptions<RabbitMqOptions> opciones,
            ILogger<ReservasConsumer> log)
            : base(conexion, ambitos, opciones, log)
        {
            _log = log;
        }

        protected override string Servicio => "reservas";

        protected override string[] RoutingKeys =>
        [
            Hotel.Contracts.RoutingKeys.HabitacionBloqueada,
            Hotel.Contracts.RoutingKeys.HabitacionNoDisponible,
            Hotel.Contracts.RoutingKeys.PagoAprobado,
            Hotel.Contracts.RoutingKeys.PagoRechazado
        ];

        protected override async Task ManejarAsync(
            string routingKey, string json, IServiceProvider servicios, CancellationToken ct)
        {
            var reservas = servicios.GetRequiredService<IReservaService>();

            try
            {
                switch (routingKey)
                {
                    // Las noches quedaron bloqueadas: la reserva avanza y
                    // se solicitará el pago.
                    case Hotel.Contracts.RoutingKeys.HabitacionBloqueada:
                    {
                        var evento = Deserializar<HabitacionBloqueada>(json);
                        await reservas.MarcarBloqueadaAsync(evento.ReservaId);
                        break;
                    }

                    // No había disponibilidad: la Saga termina en rechazo.
                    case Hotel.Contracts.RoutingKeys.HabitacionNoDisponible:
                    {
                        var evento = Deserializar<HabitacionNoDisponible>(json);
                        await reservas.RechazarAsync(evento.ReservaId, evento.Motivo);
                        break;
                    }

                    // Pago aprobado: fin feliz.
                    case Hotel.Contracts.RoutingKeys.PagoAprobado:
                    {
                        var evento = Deserializar<PagoAprobado>(json);

                        try
                        {
                            await reservas.ConfirmarAsync(evento.ReservaId);
                        }
                        catch (TransicionInvalidaException ex)
                        {
                            // El cobro se aprobó cuando la reserva ya no esperaba
                            // el pago: venció su bloqueo o el huésped la canceló.
                            // El dinero quedó cobrado sin reserva que lo respalde,
                            // así que esto exige una devolución.
                            _log.LogWarning(
                                "Cobro aprobado fuera de plazo para la reserva {ReservaId} " +
                                "({Monto}): {Motivo}. Requiere devolución.",
                                evento.ReservaId, evento.Monto, ex.Message);

                            throw new EventoIgnorableException(ex.Message);
                        }

                        break;
                    }

                    // Pago rechazado: se rechaza la reserva, lo que dispara
                    // la compensación en el servicio de Habitaciones.
                    case Hotel.Contracts.RoutingKeys.PagoRechazado:
                    {
                        var evento = Deserializar<PagoRechazado>(json);
                        await reservas.RechazarAsync(evento.ReservaId, evento.Motivo);
                        break;
                    }
                }
            }
            catch (TransicionInvalidaException ex)
            {
                // El mensaje llegó repetido o fuera de orden y la reserva ya
                // cambió de estado: reintentarlo no cambiaría el resultado.
                throw new EventoIgnorableException(ex.Message);
            }
            catch (NoEncontradoException ex)
            {
                throw new EventoIgnorableException(ex.Message);
            }
        }

        private static T Deserializar<T>(string json) =>
            JsonSerializer.Deserialize<T>(json)
            ?? throw new EventoIgnorableException($"No se pudo interpretar el evento {typeof(T).Name}.");
    }
}
