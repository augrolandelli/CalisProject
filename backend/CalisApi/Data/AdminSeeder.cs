using CalisApi.Auth;
using CalisApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CalisApi.Data;

/// <summary>
/// Seeding de un usuario admin en producción si no existe ninguno.
/// La contraseña se lee de configuración (variables de entorno) y nunca se loguea.
/// </summary>
public static class AdminSeeder
{
    public static async Task SeedAsync(
        CalisDbContext db,
        IPasswordHasher passwordHasher,
        IConfiguration configuration,
        ILogger logger)
    {
        if (await db.Users.AnyAsync(u => u.Role == Roles.Admin))
        {
            logger.LogInformation("Admin seeding skipped: an admin user already exists.");
            return;
        }

        var email = configuration["AdminSeed:Email"] ?? "admin@calisapp.com";
        var password = configuration["AdminSeed:Password"] ?? "Admin123!";
        var fullName = configuration["AdminSeed:FullName"] ?? "Admin CalisApp";
        var phone = configuration["AdminSeed:Phone"] ?? "0000000000";

        db.Users.Add(new User
        {
            FullName = fullName,
            Phone = phone,
            Email = email,
            PasswordHash = passwordHasher.Hash(password),
            Role = Roles.Admin,
            State = "Activo",
        });

        await db.SaveChangesAsync();
        logger.LogInformation("Admin user seeded successfully with email {Email}.", email);
    }
}
