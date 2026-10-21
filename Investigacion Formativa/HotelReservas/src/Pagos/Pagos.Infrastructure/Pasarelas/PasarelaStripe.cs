using Hotel.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Pagos.Application.Configuracion;
using Pagos.Application.Interfaces;
using Stripe;

namespace Pagos.Infrastructure.Pasarelas
{
    // Integración con el entorno de pruebas de Stripe.
    //
    // Se usa una clave de prueba (sk_test_...) y un método de pago de prueba,
    // de modo que no interviene dinero real. El cobro se crea y confirma en una
    // sola llamada (PaymentIntent con confirm = true), sin formulario de tarjeta.
    public class PasarelaStripe : IPasarelaPago
    {
        private readonly StripeOptions _opciones;
        private readonly ILogger<PasarelaStripe> _log;

        public PasarelaStripe(IOptions<PasarelaOptions> opciones, ILogger<PasarelaStripe> log)
        {
            _opciones = opciones.Value.Stripe;
            _log = log;

            if (string.IsNullOrWhiteSpace(_opciones.ClaveSecreta))
            {
                throw new InvalidOperationException(
                    "Falta la clave secreta de Stripe. Configúrela con: " +
                    "dotnet user-secrets set \"Pasarela:Stripe:ClaveSecreta\" \"sk_test_...\"");
            }
        }

        public string Nombre => "Stripe";

        public async Task<ResultadoCobro> CobrarAsync(
            decimal monto,
            string moneda,
            string descripcion,
            string metodoPago,
            CancellationToken ct = default)
        {
            var servicio = new PaymentIntentService(new StripeClient(_opciones.ClaveSecreta));

            // El medio elegido por el huésped se traduce al método de prueba
            // equivalente de Stripe.
            var metodoStripe = metodoPago == MetodosPago.TarjetaRechazada
                ? "pm_card_visa_chargeDeclined"
                : _opciones.MetodoPagoPrueba;

            var opciones = new PaymentIntentCreateOptions
            {
                // Stripe trabaja con la unidad mínima de la moneda (centavos).
                Amount = (long)Math.Round(monto * 100m),
                Currency = moneda,
                Description = descripcion,
                PaymentMethod = metodoStripe,

                // Se confirma en el acto y se descartan los métodos que
                // requieren redirección, ya que no hay navegador involucrado.
                Confirm = true,
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                    AllowRedirects = "never"
                }
            };

            try
            {
                var intento = await servicio.CreateAsync(opciones, cancellationToken: ct);

                if (intento.Status == "succeeded")
                {
                    _log.LogInformation(
                        "Cobro aprobado en Stripe: {Monto} {Moneda} ({Id})",
                        monto, moneda.ToUpperInvariant(), intento.Id);

                    return ResultadoCobro.Exito(intento.Id);
                }

                _log.LogWarning(
                    "Stripe devolvió el estado {Estado} para el cobro {Id}.",
                    intento.Status, intento.Id);

                return ResultadoCobro.Fallo($"El pago quedó en estado {intento.Status}.");
            }
            catch (StripeException ex)
            {
                // Tarjeta rechazada, fondos insuficientes, datos inválidos...
                _log.LogWarning("Stripe rechazó el cobro: {Mensaje}", ex.StripeError?.Message);

                return ResultadoCobro.Fallo(
                    ex.StripeError?.Message ?? "La pasarela rechazó el cobro.");
            }
        }
    }
}
