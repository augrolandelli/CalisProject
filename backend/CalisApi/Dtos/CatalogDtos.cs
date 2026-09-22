namespace CalisApi.Dtos;

/// <summary>Categoría de ejercicios/rutinas.</summary>
public record CategoryDto(int Id, string Name, string Description);

/// <summary>Video de la videoteca con su categoría anidada.</summary>
public record VideoDto(
    int Id,
    string Title,
    string Description,
    string Difficulty,
    string Requisites,
    string Url,
    int CategoryId,
    CategoryDto Category);

/// <summary>Rutina en formato resumido (listados).</summary>
public record RutineSummaryDto(
    int Id,
    string Title,
    string Description,
    string Duration,
    string Difficulty,
    string CategoryName);

/// <summary>Ejercicio dentro del detalle de una rutina.</summary>
public record RutineExerciseDto(
    string Exercise,
    string Tipo,
    int Reps,
    int Series,
    string Descanso,
    string Obs,
    int? VideoId);

/// <summary>Detalle completo de una rutina con ejercicios ordenados (calentamiento primero).</summary>
public record RutineDetailDto(
    int Id,
    string Title,
    string Description,
    string Duration,
    string Difficulty,
    int CategoryId,
    IReadOnlyList<RutineExerciseDto> Exercises);

// ---- Escritura (Admin, Fase 5) ----

/// <summary>Alta/edición de categoría.</summary>
public record WriteCategoryRequest(string Name, string Description);

/// <summary>Alta de video con archivo ya subido y validado (purpose "exercise").</summary>
public record WriteVideoRequest(
    string Title,
    string Description,
    string Difficulty,
    string Requisites,
    int CategoryId,
    Guid? MediaAssetId);

/// <summary>Ejercicio del constructor de rutinas.</summary>
public record WriteRutineExercise(
    string Exercise,
    string Tipo,
    int Reps,
    int Series,
    string Descanso,
    string Obs,
    int? VideoId);

/// <summary>Alta/edición de rutina. El servidor normaliza el orden (calentamiento primero).</summary>
public record WriteRutineRequest(
    string Title,
    string Description,
    string Duration,
    string Difficulty,
    int CategoryId,
    WriteRutineExercise[] Exercises);
