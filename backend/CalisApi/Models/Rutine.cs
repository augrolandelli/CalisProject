namespace CalisApi.Models;

/// <summary>
/// Rutina de entrenamiento compuesta por ejercicios ordenados
/// (calentamiento primero, luego principales).
/// </summary>
public class Rutine
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Duration { get; set; }
    public required string Difficulty { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public ICollection<RutineExercise> Exercises { get; set; } = [];
}
