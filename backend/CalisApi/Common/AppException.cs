namespace CalisApi.Common;

/// <summary>
/// Excepción de aplicación con código HTTP y mensaje seguro para el cliente.
/// Los detalles internos nunca se exponen (ver spec §10).
/// </summary>
public class AppException(string message, int statusCode = StatusCodes.Status400BadRequest, string code = "app_error")
    : Exception(message)
{
    public int StatusCode { get; } = statusCode;
    public string Code { get; } = code;

    public static AppException NotFound(string message) =>
        new(message, StatusCodes.Status404NotFound, "not_found");

    public static AppException Conflict(string message) =>
        new(message, StatusCodes.Status409Conflict, "conflict");

    public static AppException Unauthorized(string message) =>
        new(message, StatusCodes.Status401Unauthorized, "unauthorized");

    public static AppException Forbidden(string message) =>
        new(message, StatusCodes.Status403Forbidden, "forbidden");
}
