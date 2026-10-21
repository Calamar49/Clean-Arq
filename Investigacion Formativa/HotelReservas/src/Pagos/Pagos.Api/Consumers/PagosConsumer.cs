using System.Text.Json;
using Hotel.Contracts;
using Hotel.Messaging;
using Microsoft.Extensions.Options;
using Pagos.Application.Interfaces;

namespace Pagos.Api.Consumers
{
    // Escucha las reservas que ya tienen la habitación bloqueada y procesa el cobro.
    public class PagosConsumer : ConsumidorBase
    {
        public PagosConsumer(
            RabbitMqConexion conexion,
            IServiceScopeFactory ambitos,
            IOptions<RabbitMqOptions> opciones,
            ILogger<PagosConsumer> log)
            : base(conexion, ambitos, opciones, log)
        {
        }

        protected override string Servicio => "pagos";

        protected override string[] RoutingKeys => [Hotel.Contracts.RoutingKeys.ReservaBloqueada];

        protected override async Task ManejarAsync(
            string routingKey, string json, IServiceProvider servicios, CancellationToken ct)
        {
            var evento = JsonSerializer.Deserialize<ReservaBloqueada>(json)
                ?? throw new EventoIgnorableException("No se pudo interpretar el evento ReservaBloqueada.");

            var pagos = servicios.GetRequiredService<IPagoService>();

            await pagos.ProcesarCobroAsync(
                evento.MessageId, evento.ReservaId, evento.MontoTotal,
                evento.HuespedEmail, evento.MetodoPago, ct);
        }
    }
}
