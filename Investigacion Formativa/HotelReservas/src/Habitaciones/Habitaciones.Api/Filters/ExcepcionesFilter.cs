using Habitaciones.Application.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Habitaciones.Api.Filters
{
    // Traduce las excepciones de negocio a respuestas HTTP coherentes.
    public class ExcepcionesFilter : IExceptionFilter
    {
        public void OnException(ExceptionContext context)
        {
            var respuesta = context.Exception switch
            {
                ReglaNegocioException ex => new ObjectResult(new { mensaje = ex.Message }) { StatusCode = 400 },
                ArgumentException ex => new ObjectResult(new { mensaje = ex.Message }) { StatusCode = 400 },
                NoEncontradoException ex => new ObjectResult(new { mensaje = ex.Message }) { StatusCode = 404 },
                ConflictoException ex => new ObjectResult(new { mensaje = ex.Message }) { StatusCode = 409 },
                _ => null
            };

            if (respuesta != null)
            {
                context.Result = respuesta;
                context.ExceptionHandled = true;
            }
        }
    }
}
