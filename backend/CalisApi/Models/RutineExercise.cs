namespace CalisApi.Models;

/// <summary>
/// Ejercicio dentro de una rutina, con su dosificación y referencia
/// opcional a un video de la videoteca.
/// </summary>
public class RutineExercise
{
    public int Id { get; set; }
    public required string Exercise { get; set; }
    public required string Tipo { get; set; }
    public int Reps { get; set; }
    public int Series { get; set; }
    public required string Descanso { get; set; }
    public required string Obs { get; set; }
    public int Order { get; set; }

    public int? VideoId { get; set; }
    public Video? Video { get; set; }

    public int RutineId { get; set; }
    public Rutine Rutine { get; set; } = null!;
}

/// <summary>Tipos de ejercicio dentro de una rutina.</summary>
public static class ExerciseTypes
{
    public const string Calentamiento = "Calentamiento";
    public const string Principal = "Principal";
}
