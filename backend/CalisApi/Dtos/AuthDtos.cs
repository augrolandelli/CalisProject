namespace CalisApi.Dtos;

/// <summary>Datos públicos de usuario — nunca incluir hash ni datos sensibles.</summary>
public record UserDto(int Id, string FullName, string Phone, string Email, string Role, string State, string? PhotoUrl);

/// <summary>Solicitud de registro. El rol inicial siempre es Guerrero (lo fija el servidor).</summary>
public record RegisterRequest(string FullName, string Phone, string Email, string Password);

/// <summary>Solicitud de login.</summary>
public record LoginRequest(string Email, string Password);

/// <summary>Solicitud de renovación de tokens.</summary>
public record RefreshRequest(string RefreshToken);

/// <summary>Par de tokens + usuario autenticado.</summary>
public record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt, UserDto User);
