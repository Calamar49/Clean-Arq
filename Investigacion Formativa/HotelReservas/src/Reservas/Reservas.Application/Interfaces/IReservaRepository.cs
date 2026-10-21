using Reservas.Domain.Entities;

namespace Reservas.Application.Interfaces
{
    // Contrato de persistencia del modelo de escritura.
    public interface IReservaRepository
    {
        Task<Reserva?> ObtenerAsync(Guid id);

        Task<List<Reserva>> ListarAsync(string? email, string? estado);

        // Identificadores de las reservas que siguen bloqueadas desde antes
        // de la fecha indicada. Devuelve solo los Id porque quien los usa
        // vuelve a cargar cada reserva para aplicarle su transición.
        Task<List<Guid>> ListarBloqueadasAntesDeAsync(DateTime limite, int maximo = 100);

        Task AgregarAsync(Reserva reserva);

        // Persiste los cambios aplicando concurrencia optimista.
        // Devuelve false si otro proceso modificó la reserva primero.
        Task<bool> GuardarCambiosAsync();
    }
}
