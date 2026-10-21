using Hotel.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Hotel.Messaging
{
    public static class MessagingExtensions
    {
        // Registra la mensajería en cualquier servicio con una sola línea.
        public static IServiceCollection AgregarMensajeria(
            this IServiceCollection servicios, IConfiguration configuracion)
        {
            servicios.Configure<RabbitMqOptions>(
                configuracion.GetSection(RabbitMqOptions.Seccion));

            servicios.AddSingleton<RabbitMqConexion>();
            servicios.AddSingleton<IEventPublisher, RabbitMqPublisher>();

            return servicios;
        }
    }
}
