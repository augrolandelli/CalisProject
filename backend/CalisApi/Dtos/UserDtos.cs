namespace CalisApi.Dtos;

/// <summary>Usuario para la administración (nunca incluye hash).</summary>
public record AdminUserDto(int Id, string FullName, string Email, string Role, string State);

/// <summary>Cambio de rol de un usuario (Admin).</summary>
public record ChangeRoleRequest(string Role);
