using System.Text.Json;
using Hotel.Contracts;
using Hotel.Messaging;
using Microsoft.Extensions.Options;
using Notificaciones.Application.Interfaces;

namespace Notificaciones.Api.Consumers
{
    // Escucha los desenlaces de la Saga para avisar al huésped.
    public class NotificacionesConsumer : ConsumidorBase
    {
        public NotificacionesConsumer(
            RabbitMqConexion conexion,
            IServiceScopeFactory ambitos,
            IOptions<RabbitMqOptions> opciones,
            ILogger<NotificacionesConsumer> log)
            : base(conexion, ambitos, opciones, log)
        {
        }

        protected override string Servicio => "notificaciones";

        protected override string[] RoutingKeys =>
        [
            Hotel.Contracts.RoutingKeys.ReservaConfirmada,
            Hotel.Contracts.RoutingKeys.ReservaRechazada,
            Hotel.Contracts.RoutingKeys.ReservaCancelada
        ];

        protected override async Task ManejarAsync(
            string routingKey, string json, IServiceProvider servicios, CancellationToken ct)
        {
            var notificaciones = servicios.GetRequiredService<INotificacionService>();

            switch (routingKey)
            {
                case Hotel.Contracts.RoutingKeys.ReservaConfirmada:
                {
                    var e = Deserializar<ReservaConfirmada>(json);
                    var detalle =
                        $"Habitación {e.HabitacionNumero}, del {e.FechaEntrada:dd/MM/yyyy} " +
                        $"al {e.FechaSalida:dd/MM/yyyy}. Total: {e.MontoTotal:N2}.";

                    await notificaciones.RegistrarAsync(
                        e.MessageId, routingKey, e.ReservaId, e.HuespedNombre, e.HuespedEmail, detalle, ct);
                    break;
                }

                case Hotel.Contracts.RoutingKeys.ReservaRechazada:
                {
                    var e = Deserializar<ReservaRechazada>(json);
                    await notificaciones.RegistrarAsync(
                        e.MessageId, routingKey, e.ReservaId, e.HuespedNombre, e.HuespedEmail, e.Motivo, ct);
                    break;
                }

                case Hotel.Contracts.RoutingKeys.ReservaCancelada:
                {
                    var e = Deserializar<ReservaCancelada>(json);
                    await notificaciones.RegistrarAsync(
                        e.MessageId, routingKey, e.ReservaId, e.HuespedNombre, e.HuespedEmail, e.Motivo, ct);
                    break;
                }
            }
        }

        private static T Deserializar<T>(string json) =>
            JsonSerializer.Deserialize<T>(json)
            ?? throw new EventoIgnorableException($"No se pudo interpretar el evento {typeof(T).Name}.");
    }
}
