namespace Habitaciones.Domain.Entities
{
    // Representa una habitación del hotel.
    // No depende de EF Core, ASP.NET ni de ninguna otra capa.
    public class Habitacion
    {
        public static readonly string[] TiposPermitidos = { "Simple", "Doble", "Suite" };

        public int Id { get; private set; }
        public string Numero { get; private set; } = string.Empty;
        public string Tipo { get; private set; } = string.Empty;
        public int Capacidad { get; private set; }
        public decimal PrecioPorNoche { get; private set; }
        public bool Activa { get; private set; }

        // Constructor requerido por EF Core.
        private Habitacion()
        {
        }

        public Habitacion(string numero, string tipo, int capacidad, decimal precioPorNoche)
        {
            if (string.IsNullOrWhiteSpace(numero))
                throw new ArgumentException("El número de habitación es obligatorio.");

            Numero = numero.Trim();
            Activa = true;

            Actualizar(tipo, capacidad, precioPorNoche, true);
        }

        // Reglas de negocio de una habitación: se validan siempre en el dominio.
        public void Actualizar(string tipo, int capacidad, decimal precioPorNoche, bool activa)
        {
            if (string.IsNullOrWhiteSpace(tipo) ||
                !TiposPermitidos.Contains(tipo.Trim(), StringComparer.OrdinalIgnoreCase))
                throw new ArgumentException(
                    $"El tipo debe ser uno de: {string.Join(", ", TiposPermitidos)}.");

            if (capacidad < 1 || capacidad > 10)
                throw new ArgumentException("La capacidad debe estar entre 1 y 10 personas.");

            if (precioPorNoche <= 0)
                throw new ArgumentException("El precio por noche debe ser mayor a cero.");

            Tipo = TiposPermitidos.First(t => t.Equals(tipo.Trim(), StringComparison.OrdinalIgnoreCase));
            Capacidad = capacidad;
            PrecioPorNoche = precioPorNoche;
            Activa = activa;
        }
    }
}
