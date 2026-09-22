namespace CalisApi.Models;

/// <summary>
/// Video de ejercicio de la videoteca. El archivo se aloja en almacenamiento
/// de objetos (S3) y aquí se guarda su URL.
/// </summary>
public class Video
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Difficulty { get; set; }
    public required string Requisites { get; set; }
    public required string Url { get; set; }

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public ICollection<RutineExercise> RutineExercises { get; set; } = [];
}
