using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Hotel.Messaging
{
    // Mantiene una única conexión a RabbitMQ compartida por todo el servicio.
    // Abrir una conexión por mensaje sería costoso y poco eficiente.
    public class RabbitMqConexion : IAsyncDisposable
    {
        private readonly RabbitMqOptions _opciones;
        private readonly SemaphoreSlim _candado = new(1, 1);
        private IConnection? _conexion;

        public RabbitMqConexion(IOptions<RabbitMqOptions> opciones)
        {
            _opciones = opciones.Value;
        }

        public async Task<IConnection> ObtenerAsync(CancellationToken ct = default)
        {
            if (_conexion is { IsOpen: true })
                return _conexion;

            await _candado.WaitAsync(ct);
            try
            {
                if (_conexion is { IsOpen: true })
                    return _conexion;

                var factory = new ConnectionFactory
                {
                    HostName = _opciones.Host,
                    Port = _opciones.Puerto,
                    UserName = _opciones.Usuario,
                    Password = _opciones.Clave,

                    // Si el broker se reinicia, el cliente se reconecta solo.
                    AutomaticRecoveryEnabled = true,
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(5)
                };

                _conexion = await factory.CreateConnectionAsync(ct);
                return _conexion;
            }
            finally
            {
                _candado.Release();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_conexion is not null)
                await _conexion.DisposeAsync();

            _candado.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
