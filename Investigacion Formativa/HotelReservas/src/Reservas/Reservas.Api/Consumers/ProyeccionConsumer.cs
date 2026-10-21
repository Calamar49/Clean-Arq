using System.Text.Json;
using Hotel.Contracts;
using Hotel.Messaging;
using Microsoft.Extensions.Options;
using Reservas.Application.Services;

namespace Reservas.Api.Consumers
{
    // Mantiene el modelo de lectura al día (lado "query" de CQRS).
    //
    // Usa su propia cola, separada de la del consumidor de la Saga: así un
    // problema en la proyección no detiene el avance de las reservas, y
    // viceversa.
    public class ProyeccionConsumer : ConsumidorBase
    {
        public ProyeccionConsumer(
            RabbitMqConexion conexion,
            IServiceScopeFactory ambitos,
            IOptions<RabbitMqOptions> opciones,
            ILogger<ProyeccionConsumer> log)
            : base(conexion, ambitos, opciones, log)
        {
        }

        protected override string Servicio => "reservas.proyeccion";

        protected override string[] RoutingKeys =>
        [
            Hotel.Contracts.RoutingKeys.ReservaSolicitada,
            Hotel.Contracts.RoutingKeys.ReservaBloqueada,
            Hotel.Contracts.RoutingKeys.ReservaConfirmada,
            Hotel.Contracts.RoutingKeys.ReservaRechazada,
            Hotel.Contracts.RoutingKeys.ReservaCancelada
        ];

        protected override async Task ManejarAsync(
            string routingKey, string json, IServiceProvider servicios, CancellationToken ct)
        {
            var proyeccion = servicios.GetRequiredService<IProyeccionService>();

            switch (routingKey)
            {
                // Alta de la fila en la vista.
                case Hotel.Contracts.RoutingKeys.ReservaSolicitada:
                    await proyeccion.ProyectarSolicitudAsync(Deserializar<ReservaSolicitada>(json));
                    break;

                case Hotel.Contracts.RoutingKeys.ReservaBloqueada:
                {
                    var e = Deserializar<ReservaBloqueada>(json);
                    await proyeccion.ProyectarEstadoAsync(e.ReservaId, "Bloqueada", null, e.FechaEvento);
                    break;
                }

                case Hotel.Contracts.RoutingKeys.ReservaConfirmada:
                {
                    var e = Deserializar<ReservaConfirmada>(json);
                    await proyeccion.ProyectarEstadoAsync(e.ReservaId, "Confirmada", null, e.FechaEvento);
                    break;
                }

                case Hotel.Contracts.RoutingKeys.ReservaRechazada:
                {
                    var e = Deserializar<ReservaRechazada>(json);
                    await proyeccion.ProyectarEstadoAsync(e.ReservaId, "Rechazada", e.Motivo, e.FechaEvento);
                    break;
                }

                case Hotel.Contracts.RoutingKeys.ReservaCancelada:
                {
                    var e = Deserializar<ReservaCancelada>(json);
                    await proyeccion.ProyectarEstadoAsync(e.ReservaId, "Cancelada", e.Motivo, e.FechaEvento);
                    break;
                }
            }
        }

        private static T Deserializar<T>(string json) =>
            JsonSerializer.Deserialize<T>(json)
            ?? throw new EventoIgnorableException($"No se pudo interpretar el evento {typeof(T).Name}.");
    }
}
