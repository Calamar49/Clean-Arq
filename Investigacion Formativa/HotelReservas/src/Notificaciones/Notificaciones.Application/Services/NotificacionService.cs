using Hotel.Contracts;
using Microsoft.Extensions.Logging;
using Notificaciones.Application.DTOs;
using Notificaciones.Application.Interfaces;
using Notificaciones.Domain.Entities;

namespace Notificaciones.Application.Services
{
    // Genera los avisos al huésped según el desenlace de la Saga.
    public class NotificacionService : INotificacionService
    {
        private readonly INotificacionRepository _repository;
        private readonly ILogger<NotificacionService> _log;

        public NotificacionService(
            INotificacionRepository repository, ILogger<NotificacionService> log)
        {
            _repository = repository;
            _log = log;
        }

        public async Task RegistrarAsync(
            Guid messageId,
            string routingKey,
            Guid reservaId,
            string huespedNombre,
            string huespedEmail,
            string? detalle,
            CancellationToken ct)
        {
            // Idempotencia: un mensaje reentregado no genera un aviso duplicado.
            if (await _repository.YaProcesadoAsync(messageId))
            {
                _log.LogInformation("Mensaje {MessageId} ya procesado: no se repite el aviso.", messageId);
                return;
            }

            var (tipo, asunto, cuerpo) = Componer(routingKey, huespedNombre, detalle);

            await _repository.AgregarAsync(
                new Notificacion(reservaId, huespedEmail, tipo, asunto, cuerpo));

            await _repository.RegistrarProcesadoAsync(messageId, routingKey);

            // En un sistema real aquí se enviaría el correo; para el prototipo
            // basta con dejar constancia del aviso generado.
            _log.LogInformation(
                "Aviso registrado para {Email}: {Asunto}", huespedEmail, asunto);
        }

        public async Task<List<NotificacionDTO>> ListarAsync(string? destinatario, Guid? reservaId)
        {
            var notificaciones = await _repository.ListarAsync(destinatario, reservaId);

            return notificaciones.Select(n => new NotificacionDTO
            {
                Id = n.Id,
                ReservaId = n.ReservaId,
                Destinatario = n.Destinatario,
                Tipo = n.Tipo.ToString(),
                Asunto = n.Asunto,
                Cuerpo = n.Cuerpo,
                FechaEnvio = n.FechaEnvio
            }).ToList();
        }

        private static (TipoNotificacion Tipo, string Asunto, string Cuerpo) Componer(
            string routingKey, string huesped, string? detalle)
        {
            return routingKey switch
            {
                RoutingKeys.ReservaConfirmada => (
                    TipoNotificacion.ReservaConfirmada,
                    "Su reserva fue confirmada",
                    $"Estimado(a) {huesped}, su reserva quedó confirmada. {detalle}".Trim()),

                RoutingKeys.ReservaRechazada => (
                    TipoNotificacion.ReservaRechazada,
                    "No pudimos completar su reserva",
                    $"Estimado(a) {huesped}, su reserva no pudo completarse. Motivo: {detalle}"),

                _ => (
                    TipoNotificacion.ReservaCancelada,
                    "Su reserva fue cancelada",
                    $"Estimado(a) {huesped}, su reserva fue cancelada. Motivo: {detalle}")
            };
        }
    }
}
