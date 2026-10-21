namespace Pagos.Application.Interfaces
{
    // Resultado devuelto por la pasarela tras intentar el cobro.
    public record ResultadoCobro(bool Aprobado, string? Referencia, string? Motivo)
    {
        public static ResultadoCobro Exito(string referencia) => new(true, referencia, null);

        public static ResultadoCobro Fallo(string motivo) => new(false, null, motivo);
    }

    // Contrato de la pasarela de pago.
    //
    // Gracias a esta abstracción el servicio no sabe si detrás hay una pasarela
    // simulada o Stripe: la implementación se elige por configuración. Eso
    // permite ejecutar la demostración sin conexión y, a la vez, integrar una
    // pasarela real sin tocar la lógica de negocio.
    public interface IPasarelaPago
    {
        // Nombre de la pasarela, que se guarda junto al pago.
        string Nombre { get; }

        // El medio de pago llega como un identificador, no como datos de
        // tarjeta: cada pasarela lo traduce a su propio método de prueba.
        Task<ResultadoCobro> CobrarAsync(
            decimal monto,
            string moneda,
            string descripcion,
            string metodoPago,
            CancellationToken ct = default);
    }
}
