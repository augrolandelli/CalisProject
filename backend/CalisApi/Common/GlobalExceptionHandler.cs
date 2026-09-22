using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CalisApi.Common;

/// <summary>
/// Manejador global de excepciones: formato uniforme { message, code } (spec §4.9)
/// y sin stack traces en las respuestas.
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is FluentValidation.ValidationException validation)
        {
            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            await httpContext.Response.WriteAsJsonAsync(new
            {
                message = validation.Errors.First().ErrorMessage,
                code = "validation_error",
                errors = validation.Errors.GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()),
            }, cancellationToken);
            return true;
        }

        var statusCode = exception is AppException appException
            ? appException.StatusCode
            : StatusCodes.Status500InternalServerError;

        var message = exception is AppException ? exception.Message : "Error interno del servidor.";
        var code = exception is AppException appEx ? appEx.Code : "internal_error";

        if (statusCode >= 500)
        {
            logger.LogError(exception, "Error no controlado procesando {Path}", httpContext.Request.Path);
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = message,
            Extensions = { ["code"] = code },
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new { message, code }, cancellationToken);
        return true;
    }
}
