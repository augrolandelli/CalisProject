using Microsoft.EntityFrameworkCore;

namespace CalisApi.Data;

public static class MigrationExtensions
{
    /// <summary>
    /// Aplica las migraciones pendientes de EF Core. Útil en el arranque de la API
    /// en entornos donde no se puede correr <c>dotnet ef database update</c>.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CalisDbContext>();
        await db.Database.MigrateAsync();
    }
}
