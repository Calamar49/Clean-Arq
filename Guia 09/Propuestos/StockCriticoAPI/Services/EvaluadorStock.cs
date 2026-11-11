using StockCriticoAPI.Models;

namespace StockCriticoAPI.Services
{
    // Determina el estado de un producto comparando sus existencias con el
    // mínimo establecido. La regla vive aquí y no en el controlador para
    // poder reutilizarla en el alta, en la consulta y en el filtro de
    // críticos sin repetirla tres veces.
    public static class EvaluadorStock
    {
        public const string Critico = "Crítico";
        public const string Alerta = "Alerta";
        public const string Normal = "Normal";

        public static string Evaluar(ProductoCritico producto)
        {
            if (producto.Stock < producto.StockMinimo)
            {
                return Critico;
            }

            if (producto.Stock == producto.StockMinimo)
            {
                return Alerta;
            }

            return Normal;
        }
    }
}
