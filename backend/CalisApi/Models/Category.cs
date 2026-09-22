namespace CalisApi.Models;

/// <summary>
/// Categoría de ejercicios/rutinas (ej. Pull, Push, Legs).
/// </summary>
public class Category
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }

    public ICollection<Video> Videos { get; set; } = [];
    public ICollection<Rutine> Rutines { get; set; } = [];
}
