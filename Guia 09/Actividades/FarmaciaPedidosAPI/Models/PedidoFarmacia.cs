namespace FarmaciaPedidosAPI.Models
{
    // Modelo que representa un pedido realizado al área de farmacia.
    public class PedidoFarmacia
    {
        // Identificador del pedido.
        public int Id { get; set; }

        // Nombre del medicamento solicitado.
        public string Medicamento { get; set; } = string.Empty;

        // Área que solicita el medicamento.
        public string AreaSolicitante { get; set; } = string.Empty;

        // Cantidad requerida.
        public int Cantidad { get; set; }

        // Fecha del pedido.
        public DateTime Fecha { get; set; }

        // Estado actual del pedido.
        public string Estado { get; set; } = string.Empty;
    }
}
